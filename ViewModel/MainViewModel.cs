using Dwarf_Fortress_Log.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dwarf_Fortress_Log.ViewModel
{
    public class MainViewModel : ObservableRecipient
    {
        public IAsyncRelayCommand LoadGamelogCommand { get; }
        public IAsyncRelayCommand LoadConfigCommand { get; }
        public ObservableCollection<LogItem> LogItems { get; } = new ObservableCollection<LogItem>();
        public ObservableCollection<MissingItem> MissingItems { get; } = new ObservableCollection<MissingItem>();
        public ObservableCollection<BattleItem> BattleItems { get; } = new ObservableCollection<BattleItem>();

        private Process? _process;
        public Process? Process
        {
            get => _process;
            set => SetProperty<Process>(ref _process, value);
        }

        private object logItemsLock = new object();
        private object missingItemsLock = new object();
        private object battleItemsLock = new object();
        private long lastPosition = 0;

        private Configuration _configuration;
        public Configuration Configuration
        {
            get => _configuration;
            set => SetProperty(ref _configuration, value);
        }

        private Dictionary<string, SolidColorBrush> customBrushes = new Dictionary<string, SolidColorBrush>();

        public MainViewModel()
        {
            try
            {
                LoadGamelogCommand = new AsyncRelayCommand(LoadGamelogAsync);
                LoadConfigCommand = new AsyncRelayCommand(LoadConfigAsync);
                BindingOperations.EnableCollectionSynchronization(LogItems, logItemsLock);
                BindingOperations.EnableCollectionSynchronization(MissingItems, missingItemsLock);
                BindingOperations.EnableCollectionSynchronization(BattleItems, battleItemsLock);

                LoadConfigCommand.Execute(null);
                LoadCustomBrushes(Configuration);

                LoadGamelogCommand.Execute(null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private Task LoadConfigAsync()
        {
            IDeserializer builder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Configuration = builder.Deserialize<Configuration>(File.ReadAllText("config.yaml"));


            return Task.CompletedTask;
        }

        private void LoadCustomBrushes(Configuration configuration)
        {
            foreach (CustomColor c in configuration.CustomColors)
            {
                try
                {
                    customBrushes.Add(c.Name, (SolidColorBrush)(new BrushConverter().ConvertFromString(c.Hex) ?? Brushes.Violet));
                }
                catch (Exception exception)
                {
                    lock (logItemsLock) LogItems.Add(new LogItem { Content = $"Could not parse custom color '{c.Name} ({c.Hex})'" });
                }
            }
        }

        private async Task LoadGamelogAsync()
        {
            Process = Process.GetProcessesByName("Dwarf Fortress").FirstOrDefault(p => p.MainWindowTitle == "Dwarf Fortress");
            if (Process == null)
            {
                lock (logItemsLock) LogItems.Add(new LogItem { Content = "'Dwarf Fortress.exe' was not found running." });
                while (true)
                {
                    lock (logItemsLock) LogItems.Add(new LogItem { Content = "Looking for 'Dwarf Fortress.exe'" });
                    await Task.Delay(1000);
                    Process = Process.GetProcessesByName("Dwarf Fortress").FirstOrDefault(p => p.MainWindowTitle == "Dwarf Fortress");
                    if (Process != null)
                    {
                        break;
                    }
                }
            }

            string? gameFolder = Path.GetDirectoryName(Process.MainModule.FileName);

            using (Stream stream = File.Open($"{gameFolder}/gamelog.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                lastPosition = stream.Length - Configuration.Readback;
                lastPosition = lastPosition > 0 ? lastPosition : 0;
            }

            while (true)
            {
                try
                {
                    string? line = null;

                    Stream stream = File.Open($"{gameFolder}/gamelog.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    stream.Seek(lastPosition, SeekOrigin.Begin);
                    StreamReader streamReader = new StreamReader(stream);

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // Debug.WriteLine(line);
                        if (Regex.IsMatch(line, " cancels .+: Needs "))
                        {
                            AddMissingItem(line);

                            continue;
                        }

                        if (Regex.IsMatch(line, "The .+ .+ the .+!"))
                        {
                            AddBattleItem(line);
                        }

                        lock (logItemsLock)
                        {
                            LogItem item = CreateLogItemFromLine(line);
                            if (!item.Skipped)
                            {
                                LogItems.Add(item);
                            }
                        }
                    }
                    lastPosition = stream.Position;
                    streamReader.Close();
                    stream.Close();

                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    lock (logItemsLock) LogItems.Add(new LogItem { Content = ex.ToString() });
                    Debug.WriteLine(ex.ToString());
                    break;
                };
            }
        }

        private void AddMissingItem(string? line)
        {
            Match match = Regex.Match(line, " cancels .+: Needs (.+)\\.");
            string itemName = match.Groups[1].Value;
            itemName = Regex.Replace(itemName, "unrotten ", "");
            itemName = Regex.Replace(itemName, "unused ", "");
            itemName = Regex.Replace(itemName, "collected ", "");
            itemName = Regex.Replace(itemName, "empty ", "");
            itemName = Regex.Replace(itemName, "MAT-producing ", " ");
            itemName = Regex.Replace(itemName, " item", "");
            itemName = Regex.Replace(itemName, "-containing", "");

            MissingItem? foundItem = MissingItems.FirstOrDefault((i) => i.Item == itemName);
            if (foundItem == null)
            {
                lock (missingItemsLock)
                {
                    SolidColorBrush brush = GetRandomColor();

                    MissingItem item = new MissingItem()
                    {
                        Item = itemName,
                        ColorForeground = brush
                    };
                    MissingItems.Add(item);
                    if (MissingItems.Count > 6)
                    {
                        MissingItems.RemoveAt(0);
                    }
                }
            } else
            {
                MissingItems.Remove(foundItem);
                MissingItems.Add(foundItem);
            }
        }

        private static SolidColorBrush GetRandomColor()
        {
            Random random = new Random();
            float brightness = 0f;
            int r = 0;
            int g = 0;
            int b = 0;

            while (brightness < 100f)
            {
                r = random.Next(255);
                g = random.Next(255);
                b = random.Next(255);

                brightness = (r * 299 + g * 587 + b * 114) / 1000;

                //Debug.WriteLine(brightness);
            }

            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }

        private void AddBattleItem(string? line)
        {
            Match match = Regex.Match(line, "The (?:giant|cave|flying|stray|spinning)?(?: war|cave)? ?(.+?)[ '](?:.+?) the (?:giant|cave|flying|stray|spinning)?(?: war|cave)? ?(.+?)[ '].+?!");
            if (match.Success && match.Groups[1].Value != "force")
            {
                lock (battleItemsLock)
                {
                    SolidColorBrush brush = GetRandomColor();
                    
                    string unitA = Regex.Replace(match.Groups[1].Value, "dwarf", "drf");
                    string unitB = Regex.Replace(match.Groups[2].Value, "dwarf", "drf");

                    BattleItem battleItem = new BattleItem()
                    {
                        UnitA = unitA,
                        UnitB = unitB,
                        ColorForeground = brush
                    };

                    List<BattleItem> remove = new List<BattleItem>();
                    foreach (BattleItem bi in BattleItems)
                    {
                        if (bi.UnitA == match.Groups[1].Value && bi.UnitB == match.Groups[2].Value)
                        {
                            remove.Add(bi);
                            battleItem.ColorForeground = bi.ColorForeground;
                        }
                        if (bi.UnitA == match.Groups[2].Value && bi.UnitB == match.Groups[1].Value)
                        {
                            remove.Add(bi);
                            battleItem.ColorForeground = bi.ColorForeground;
                        }
                    }
                    foreach (BattleItem bi in remove)
                    {
                        BattleItems.Remove(bi);
                    }

                    BattleItems.Add(battleItem);
                    if (BattleItems.Count > 6)
                    {
                        BattleItems.RemoveAt(0);
                    }
                }
            }
        }

        private LogItem CreateLogItemFromLine(string line)
        {
            SolidColorBrush foreground = Brushes.WhiteSmoke;
            SolidColorBrush background = Brushes.Transparent;

            bool found = false;
            foreach (Rule r in Configuration.Rules)
            {
                if (r.Regex != null && Regex.IsMatch(line, r.Regex))
                {
                    if (r.Skip)
                    {
                        return new LogItem { Skipped = true };
                    }
                    if (r.Foreground != null)
                    {
                        foreground = StringToBrush(r.Foreground);
                    }
                    if (r.Background != null)
                    {
                        background = StringToBrush(r.Background);
                    }
                    if (found == true)
                    {
                        Debug.WriteLine($"Duplicate rule match found for {line}: {r.Regex}");
                    }
                    found = true;
                }
            }

            return new LogItem
            {
                ColorBackground = background,
                ColorForeground = foreground,
                Content = line
            };
        }

        private SolidColorBrush StringToBrush(string term)
        {
            if (customBrushes.TryGetValue(term, out var brush))
            {
                return brush;
            }

            try
            {
                return (SolidColorBrush)(new BrushConverter().ConvertFromString(term) ?? Brushes.Violet);
            }
            catch (Exception exception)
            {
                lock (logItemsLock) LogItems.Add(new LogItem { Content = $"Could not parse custom color '{term}'" });
            }
            return Brushes.Violet;
        }
    }
}
