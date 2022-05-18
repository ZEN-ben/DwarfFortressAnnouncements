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
using System.Windows.Data;
using System.Windows.Media;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dwarf_Fortress_Log.ViewModel
{
    public class MainViewModel : ObservableRecipient
    {
        public ObservableCollection<LogItem> LogItems { get; } = new ObservableCollection<LogItem>();

        private object logItemsLock = new object();
        private long lastPosition = 0;

        private Configuration configuration;
        private Dictionary<string, SolidColorBrush> customBrushes = new Dictionary<string, SolidColorBrush>();

        public MainViewModel()
        {
            LoadGamelogCommand = new AsyncRelayCommand(LoadGamelogAsync);
            BindingOperations.EnableCollectionSynchronization(LogItems, logItemsLock);

            IDeserializer builder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            configuration = builder.Deserialize<Configuration>(File.ReadAllText("config.yaml"));

            LoadCustomBrushes(configuration);

            LoadGamelogCommand.Execute(null);
        }

        private void LoadCustomBrushes(Configuration configuration)
        {
            foreach (CustomColor c in configuration.CustomColors)
            {
                try
                {
                    customBrushes.Add(c.Name, (SolidColorBrush)(new BrushConverter().ConvertFromString(c.Hex) ?? Brushes.Violet));
                } catch (Exception exception)
                {
                    lock (logItemsLock) LogItems.Add(new LogItem { Content = $"Could not parse custom color '{c.Name} ({c.Hex})'" });
                }
            }
        }

        public IAsyncRelayCommand LoadGamelogCommand { get; }

        private async Task LoadGamelogAsync()
        {
            Process? process = Process.GetProcessesByName("Dwarf Fortress").FirstOrDefault();
            if (process == null)
            {
                lock (logItemsLock) LogItems.Add(new LogItem {  Content = "'Dwarf Fortress.exe' was not found running." });
                while (true)
                {
                    lock (logItemsLock) LogItems.Add(new LogItem { Content = "Looking for 'Dwarf Fortress.exe'" });
                    await Task.Delay(1000);
                    process = Process.GetProcessesByName("Dwarf Fortress").FirstOrDefault();
                    if (process != null)
                    {
                        break;
                    }
                }
            }

            string? gameFolder = Path.GetDirectoryName(process.MainModule.FileName);

            using (Stream stream = File.Open($"{gameFolder}/gamelog.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                lastPosition = stream.Length - configuration.Readback;
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
                        lock (logItemsLock) LogItems.Add(CreateLogItemFromLine(line));
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

        private LogItem CreateLogItemFromLine(string line)
        {
            SolidColorBrush foreground = Brushes.WhiteSmoke;
            SolidColorBrush background = Brushes.Transparent;

            bool found = false;
            foreach (Rule r in configuration.Rules)
            {
                if (r.Regex != null && Regex.IsMatch(line, r.Regex))
                {
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
