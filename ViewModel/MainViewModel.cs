﻿using Dwarf_Fortress_Log.Model;
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

        private Process? _process;
        public Process? Process
        {
            get => _process;
            set => SetProperty<Process>(ref _process, value);
        }

        private object logItemsLock = new object();
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
            LoadGamelogCommand = new AsyncRelayCommand(LoadGamelogAsync);
            BindingOperations.EnableCollectionSynchronization(LogItems, logItemsLock);

            IDeserializer builder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Configuration = builder.Deserialize<Configuration>(File.ReadAllText("config.yaml"));

            LoadCustomBrushes(Configuration);

            LoadGamelogCommand.Execute(null);
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

        public IAsyncRelayCommand LoadGamelogCommand { get; }

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
