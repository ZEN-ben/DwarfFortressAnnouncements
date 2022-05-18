using Dwarf_Fortress_Log.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarf_Fortress_Log
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService(typeof(MainViewModel));

            ((INotifyCollectionChanged)this.listBox.Items).CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs e) =>
            {
                int i = this.listBox.Items.Count - 1;
                if (i > 0)
                {
                    this.listBox.ScrollIntoView(this.listBox.Items[i]);
                }
            };
        }
    }
}
