using Microsoft.Win32;
using RemoveWirelessSwitchDriver.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace RemoveWirelessSwitchDriver.Models
{
    internal class Win32
    {
        public static bool IsFlightMode()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            MainWindow.viewModel.AddLogMsg("ネットワークの状態を確認します。");
            foreach (var networkInterface in networkInterfaces)
            {
                MainWindow.viewModel.AddLogMsg(networkInterface.Name + ": " + networkInterface.Description);

                if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;

                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    MainWindow.viewModel.AddLogMsg("機内モードが解除され、正常にWi-Fi接続できる状態になりました。");
                    MainWindow.viewModel.RebootCnt = 0;
                    return false;
                }
            }
            return true;

        }
    }
}
