using MahApps.Metro.Controls;
using RemoveWirelessSwitchDriver.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoveWirelessSwitchDriver.Models;
using System.Windows.Controls.Primitives;
using System.IO;

namespace RemoveWirelessSwitchDriver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MainViewModel viewModel;
        const string LOGFILE = "log.xml";

        public MainWindow()
        {

            InitializeComponent();
            DataContext = viewModel = new MainViewModel();

            if (File.Exists(LOGFILE))
            {
                MainViewModel tmpViewModel = Common.DeserializeFromXmlFile<MainViewModel>(LOGFILE);
                viewModel.RebootCnt = tmpViewModel.RebootCnt + 1;
            }

            viewModel.ListBox = listBox;

            // アプリ起動と同時に自動的に実行する
            Start_Click(null, null);
        }

        private async void Start_Click(object? sender, RoutedEventArgs? e)
        {
            viewModel.VisibilityRunning = Visibility.Visible;

            bool result = false;
            string[] deviceId = new string[] 
            {
                @"ACPI\VEN_NCT&DEV_0021",
            };

            try
            {
                if(viewModel.RebootCnt >= 3)
                {
                    viewModel.RebootCnt = 0;
                    viewModel.VisibilityExit = Visibility.Visible;
                    viewModel.AddLogMsg("3連続で機内モードの解除に失敗しました。");
                    result = await Common.CountDown(5, "アプリを終了します。");
                    Application.Current.Shutdown();
                }


                await Task.Run(() => { result = Win32.IsFlightMode(); });

                if (!result)
                {
                    viewModel.VisibilityExit = Visibility.Visible;

                    result = await Common.CountDown(5, "アプリを終了します。");
                    if (!result) return;
                    Application.Current.Shutdown();
                }

                await Task.Run(() => { result = SetupAPI.UninstallDevice(deviceId); });

                if (!result) return;

                await Task.Run(() => { result = Win32.IsFlightMode(); });

                if (result)
                {
                    viewModel.VisibilityReboot = Visibility.Visible;

                    viewModel.AddLogMsg("機内モードを検知しました。");
                    result = await Common.CountDown(5, "Wi-Fi接続できるようにするためにPCを再起動します。");
                    if (!result) return;

                    await Task.Run(() => { result = User32.RestartWindows(); });

                    if (result)
                    {
                        Common.SerializeToXmlFile(viewModel, "BeforeRebootLog.xml");
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    viewModel.VisibilityExit = Visibility.Visible;

                    result = await Common.CountDown(5, "アプリを終了します。");
                    if (!result) return;
                    Application.Current.Shutdown();
                }
            }
            catch(Exception ex)
            {
                viewModel.AddLogMsg(ex.ToString());
            }
            finally
            {
                viewModel.VisibilityStart = Visibility.Visible;
            }
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SkipFlg = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CancelFlg = true;
        }

        private void MetroWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs? e)
        {
            Common.SerializeToXmlFile(viewModel, LOGFILE);
        }
    }
}
