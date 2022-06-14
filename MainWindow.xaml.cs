using Dwarf_Fortress_Log.ViewModel;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using static Dwarf_Fortress_Log.Util.Hook;
using static Dwarf_Fortress_Log.Util.WindowsServices;

namespace Dwarf_Fortress_Log
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr dfhWnd;
        private IntPtr hWinEventHook;
        private Process dfProcess;
        private DpiScale dpiScale;
        private MainViewModel mvm;
        private bool attached = false;

        protected WinEventDelegate WinEventDelegate;
        static GCHandle GCSafetyHandle;

        public MainWindow()
        {
            InitializeComponent();

            WinEventDelegate = new WinEventDelegate(WinEventCallback);
            GCSafetyHandle = GCHandle.Alloc(WinEventDelegate);

            DataContext = App.Current.Services.GetService(typeof(MainViewModel));

            ((INotifyCollectionChanged)listBox.Items).CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs e) =>
            {
                int i = listBox.Items.Count - 1;
                if (i > 0)
                {
                    listBox.ScrollIntoView(listBox.Items[i]);
                }
            };

            mvm = (MainViewModel)DataContext;
            mvm.PropertyChanged += Mvm_PropertyChanged;
            

            Width = mvm.Configuration.Width;
            Height = mvm.Configuration.Height;
        }

        private void Mvm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (attached == false && e.PropertyName == "Process" && mvm.Process != null)
            {
                AttachWindow();
            }
        }

        private void AttachWindow()
        {
            attached = true;
            dfProcess = mvm.Process;
            dfhWnd = mvm.Process.MainWindowHandle;
            new WindowInteropHelper(this).Owner = dfhWnd;

            uint targetThreadId = GetWindowThread(dfhWnd);
            hWinEventHook = WinEventHookOne(
                SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE,
                WinEventDelegate, (uint)dfProcess.Id, targetThreadId
            );

            RECT rect = GetWindowRectangle(dfhWnd);
            MoveWindow(rect);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainViewModel? mvm = (MainViewModel)DataContext;
            dpiScale = VisualTreeHelper.GetDpi(this);
            if (mvm.Process != null)
            {
                AttachWindow();
            }
        }

        protected void WinEventCallback(
            IntPtr hWinEventHook,
            SWEH_Events eventType,
            IntPtr hWnd,
            SWEH_ObjectId idObject,
            long idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd == dfhWnd &&
                eventType == SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE &&
                idObject == (SWEH_ObjectId)SWEH_CHILDID_SELF)
            {
                RECT rect = GetWindowRectangle(hWnd);
                MoveWindow(rect);
            }
        }

        private void MoveWindow(RECT rect)
        {
            Top = (rect.Bottom - (Height+mvm.Configuration.OffsetY) * dpiScale.DpiScaleY) / dpiScale.DpiScaleY;
            Left = (rect.Right - (Width + mvm.Configuration.OffsetX) * dpiScale.DpiScaleX) / dpiScale.DpiScaleX;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.R:
                    mvm.LoadConfigCommand.Execute(null);
                    mvm.LoadGamelogCommand.Execute(null);
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GCSafetyHandle.Free();
        }
    }
}
