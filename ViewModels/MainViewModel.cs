using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Serialization;

namespace RemoveWirelessSwitchDriver.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _RebootCnt = 0;
        private string _applicationTitle = "";
        private ObservableCollection<string> _items;
        private ListBox _listBox; // ListBoxを保持するプロパティ
        private Visibility _VisibilityStart = Visibility.Visible;
        private Visibility _VisibilityRunning = Visibility.Hidden;
        private Visibility _VisibilityExit = Visibility.Hidden;
        private Visibility _VisibilityReboot = Visibility.Hidden;
        private Visibility _VisibilityCancel = Visibility.Hidden;
        private bool _CancelFlg = false;
        private bool _SkipFlg = false;


        public int RebootCnt
        {
            get { return _RebootCnt; }
            set
            {
                _RebootCnt = value;
                NotifyPropertyChanged(nameof(RebootCnt));
            }
        }


        public string ApplicationTitle
        {
            get { return _applicationTitle; }
            set
            {
                if (_applicationTitle != value)
                {
                    _applicationTitle = value;
                    NotifyPropertyChanged(nameof(ApplicationTitle));
                }
            }
        }

        public ObservableCollection<string> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                NotifyPropertyChanged(nameof(Items));
            }
        }

        [XmlIgnore]
        public ListBox ListBox
        {
            get { return _listBox; }
            set
            {
                _listBox = value;
                NotifyPropertyChanged(nameof(ListBox));
            }
        }

        public Visibility VisibilityStart
        {
            get { return _VisibilityStart; }
            set
            {
                if (_VisibilityStart == value) return;

                if (value == Visibility.Visible)
                {
                    _VisibilityStart = Visibility.Visible;
                    _VisibilityRunning = Visibility.Hidden;
                    _VisibilityExit = Visibility.Hidden;
                    _VisibilityReboot = Visibility.Hidden;
                    _VisibilityCancel = Visibility.Hidden;
                }

                NotifyPropertyChanged(nameof(VisibilityStart));
                NotifyPropertyChanged(nameof(VisibilityRunning));
                NotifyPropertyChanged(nameof(VisibilityExit));
                NotifyPropertyChanged(nameof(VisibilityReboot));
                NotifyPropertyChanged(nameof(VisibilityCancel));
            }
        }

        public Visibility VisibilityRunning
        {
            get { return _VisibilityRunning; }
            set
            {
                if (_VisibilityRunning == value) return;

                if (value == Visibility.Visible)
                {
                    _CancelFlg = false;
                    _VisibilityStart = Visibility.Hidden;
                    _VisibilityRunning = Visibility.Visible;
                    _VisibilityExit = Visibility.Hidden;
                    _VisibilityReboot = Visibility.Hidden;
                    _VisibilityCancel = Visibility.Visible;
                }

                NotifyPropertyChanged(nameof(CancelFlg));
                NotifyPropertyChanged(nameof(VisibilityStart));
                NotifyPropertyChanged(nameof(VisibilityRunning));
                NotifyPropertyChanged(nameof(VisibilityExit));
                NotifyPropertyChanged(nameof(VisibilityReboot));
                NotifyPropertyChanged(nameof(VisibilityCancel));
            }
        }

        public Visibility VisibilityExit
        {
            get { return _VisibilityExit; }
            set
            {
                if (_VisibilityExit == value) return;

                if (value == Visibility.Visible)
                {
                    _CancelFlg = false;
                    _VisibilityStart = Visibility.Hidden;
                    _VisibilityRunning = Visibility.Hidden;
                    _VisibilityExit = Visibility.Visible;
                    _VisibilityReboot = Visibility.Hidden;
                    _VisibilityCancel = Visibility.Visible;
                }

                NotifyPropertyChanged(nameof(CancelFlg));
                NotifyPropertyChanged(nameof(VisibilityStart));
                NotifyPropertyChanged(nameof(VisibilityRunning));
                NotifyPropertyChanged(nameof(VisibilityExit));
                NotifyPropertyChanged(nameof(VisibilityReboot));
                NotifyPropertyChanged(nameof(VisibilityCancel));
            }
        }

        public Visibility VisibilityReboot
        {
            get { return _VisibilityReboot; }
            set
            {
                if (_VisibilityReboot == value) return;

                if (value == Visibility.Visible)
                {
                    _CancelFlg = false;
                    _VisibilityStart = Visibility.Hidden;
                    _VisibilityRunning = Visibility.Hidden;
                    _VisibilityExit = Visibility.Hidden;
                    _VisibilityReboot = Visibility.Visible;
                    _VisibilityCancel = Visibility.Visible;
                }

                NotifyPropertyChanged(nameof(CancelFlg));
                NotifyPropertyChanged(nameof(VisibilityStart));
                NotifyPropertyChanged(nameof(VisibilityRunning));
                NotifyPropertyChanged(nameof(VisibilityExit));
                NotifyPropertyChanged(nameof(VisibilityReboot));
                NotifyPropertyChanged(nameof(VisibilityCancel));
            }
        }

        public Visibility VisibilityCancel
        {
            get { return _VisibilityCancel; }
            set
            {
                if (_VisibilityCancel == value) return;
                _VisibilityCancel = value;
                _CancelFlg = false;
                NotifyPropertyChanged(nameof(CancelFlg));
                NotifyPropertyChanged(nameof(VisibilityCancel));
            }
        }

        public bool CancelFlg
        {
            get { return _CancelFlg; }
            set
            {
                _CancelFlg = value;
                NotifyPropertyChanged(nameof(CancelFlg));
            }
        }

        public bool SkipFlg
        {
            get { return _SkipFlg; }
            set
            {
                _SkipFlg = value;
                NotifyPropertyChanged(nameof(SkipFlg));
            }
        }

        public MainViewModel()
        {
            // アセンブリ情報からタイトルを取得
            string? assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (assemblyVersion != null && assemblyName != null)
            {
                ApplicationTitle = $"{assemblyName} {assemblyVersion.ToString()}";
            }

            _items = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(_items, new object());
        }
        public void AddLogMsg(string msg)
        {
            try
            {
                if (Application.Current == null) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Add(GetTime() + " " + msg);
                });
                ScrollToBottom();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


        }

        private string GetTime()
        {
            return "[" + DateTime.Now.ToString() + "]";
        }

        private void ScrollToBottom()
        {
            if (Items.Count > 0 && ListBox != null)
            {
                int lastIndex = Items.Count - 1;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (lastIndex >= 0 && lastIndex < Items.Count)
                    {
                        ListBox.ScrollIntoView(ListBox.Items[lastIndex]);
                    }
                });
            }
        }
    }
}
