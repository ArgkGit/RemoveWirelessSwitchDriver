using RemoveWirelessSwitchDriver.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemoveWirelessSwitchDriver.Models
{
    internal class SetupAPI
    {
        // P/InvokeのためのWin32 API関数の定義
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property, out uint propertyRegDataType, IntPtr propertyBuffer, uint propertyBufferSize, out uint requiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiRemoveDevice(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, IntPtr classInstallParams, uint classInstallParamsSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiCallClassInstaller(uint installFunction, IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData);

        // SetupAPI関連の定数
        // https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetclassdevs
        // https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetdeviceregistryproperty
        const uint DIGCF_ALLCLASSES = 4;
        const uint SPDRP_HARDWAREID = 1;
        const int DIF_PROPERTYCHANGE = 18; // プロパティ変更操作
        const uint DICS_DISABLE = 3; // デバイスを無効化
        const uint DICS_FLAG_GLOBAL = 1;

        const int INVALID_HANDLE_VALUE = -1;

        // https://docs.microsoft.com/en-us/windows/win32/api/setupapi/ns-setupapi-sp_devinfo_data
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint StateChange; // 無効化または有効化
            public uint Scope;
            public uint HwProfile;
        }

        public static bool UninstallDevice(string[] deviceIds)
        {
            bool? result = null;

            // デバイスクラスのGUID (ここではすべてのデバイスクラスを指定)
            Guid classGuid = Guid.Empty;

            // デバイス情報のセットを取得
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, DIGCF_ALLCLASSES);
            if (deviceInfoSet.ToInt64() == INVALID_HANDLE_VALUE)
            {
                throw new Exception("SetupDiGetClassDevs failed.");
            }

            // デバイス情報のデータ構造体を初期化
            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

            // デバイス情報の列挙を開始
            bool success = SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            if (!success)
            {
                throw new Exception("SetupDiEnumDeviceInfo failed.");
            }

            MainWindow.viewModel.AddLogMsg("デバイスの検索を開始します。");
            while (success)
            {
                uint requiredSize = 0;
                // ハードウェアIDを取得するためにバッファーサイズを取得
                success = SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, out _, IntPtr.Zero, 0, out requiredSize);

                if (!success && Marshal.GetLastWin32Error() == 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    // 必要なバッファーサイズでバッファーを確保
                    IntPtr propertyBuffer = Marshal.AllocHGlobal((int)requiredSize);

                    // ハードウェアIDを再度取得
                    success = SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, out _, propertyBuffer, requiredSize, out requiredSize);

                    if (success)
                    {
                        string infFilePath = "";
                        bool IsFind;

                        // ハードウェアIDを取得
                        string? hardwareId = Marshal.PtrToStringAuto(propertyBuffer);
                        if (hardwareId == null) continue;
                        MainWindow.viewModel.AddLogMsg(hardwareId);

                        IsFind = false;
                        foreach (var deviceId in deviceIds)
                        {
                            if (!hardwareId.Contains(deviceId, StringComparison.OrdinalIgnoreCase)) continue;
                            IsFind = true;
                        }
                        if (!IsFind) goto Next;

                        MainWindow.viewModel.AddLogMsg("デバイスの検索に成功しました。");
                        MainWindow.viewModel.AddLogMsg("デバイスのアンインストールを開始します。");
                        success = SetupDiRemoveDevice(deviceInfoSet, ref deviceInfoData);
                        if (success)
                        {
                            MainWindow.viewModel.AddLogMsg("デバイスのアンインストールに成功しました。");

                            IsFind = false;
                            foreach (var deviceId in deviceIds)
                            {
                                if (!Common.SearchDriverFile(deviceId, out infFilePath)) continue;
                                IsFind = true;
                            }
                            if (!IsFind)
                            {
                                result = true;
                                break;
                            }
                            try
                            {
                                // ファイルを削除する
                                File.Delete(infFilePath);
                                MainWindow.viewModel.AddLogMsg($"ファイル {infFilePath} は削除されました。");

                                string pnfFilePath = Path.ChangeExtension(infFilePath, "PNF");
                                MainWindow.viewModel.AddLogMsg($"ファイル {pnfFilePath} は削除されました。");
                            }
                            catch (Exception ex)
                            {
                                MainWindow.viewModel.AddLogMsg($"ファイルの削除中にエラーが発生しました: {ex.Message}");
                            }

                            result = true;
                            break;
                        }
                        else
                        {
                            MainWindow.viewModel.AddLogMsg("デバイスのアンインストールに失敗しました。");
                            result = false;
                            break;
                        }

                    }
                Next:
                    Marshal.FreeHGlobal(propertyBuffer);
                }

                // 次のデバイス情報を列挙
                success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceInfoData.DevInst, ref deviceInfoData);

                if (MainWindow.viewModel.CancelFlg)
                {
                    result = false;
                    break;
                }
            }

            // デバイス情報セットを解放
            SetupDiDestroyDeviceInfoList(deviceInfoSet);

            if(result == null)
            {
                MainWindow.viewModel.AddLogMsg("デバイスが見つかりませんでした。");
                result = false;
            }
            return (bool)result;
        }


        public static void DisableDevice(string deviceId)
        {
            // デバイスクラスのGUID (ここではすべてのデバイスクラスを指定)
            Guid classGuid = Guid.Empty;

            // デバイス情報のセットを取得
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, DIGCF_ALLCLASSES);
            if (deviceInfoSet.ToInt64() == INVALID_HANDLE_VALUE)
            {
                throw new Exception("SetupDiGetClassDevs failed.");
            }

            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

            bool success = SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            if (!success)
            {
                throw new Exception("SetupDiEnumDeviceInfo failed.");
            }

            while (success)
            {
                // デバイスIDを取得
                uint requiredSize = 0;
                success = SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, out _, IntPtr.Zero, 0, out requiredSize);

                if (!success && Marshal.GetLastWin32Error() == 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    IntPtr propertyBuffer = Marshal.AllocHGlobal((int)requiredSize);
                    success = SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, out _, propertyBuffer, requiredSize, out requiredSize);
                    if (success)
                    {
                        string? hardwareId = Marshal.PtrToStringAuto(propertyBuffer);
                        if (hardwareId == null) continue;
                        if (hardwareId.Contains(deviceId, StringComparison.OrdinalIgnoreCase))
                        {
                            // デバイスを無効化するためのクラスインストールパラメータを設定
                            SP_CLASSINSTALL_HEADER classInstallHeader = new SP_CLASSINSTALL_HEADER
                            {
                                cbSize = (uint)Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER)),
                                InstallFunction = DIF_PROPERTYCHANGE
                            };

                            SP_PROPCHANGE_PARAMS classInstallParams = new SP_PROPCHANGE_PARAMS
                            {
                                ClassInstallHeader = classInstallHeader,
                                StateChange = DICS_DISABLE, // デバイスを無効化
                                Scope = DICS_FLAG_GLOBAL,
                                HwProfile = 0
                            };

                            IntPtr classInstallParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(classInstallParams));
                            Marshal.StructureToPtr(classInstallParams, classInstallParamsPtr, false);

                            // デバイスを無効化
                            success = SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, classInstallParamsPtr, (uint)Marshal.SizeOf(classInstallParams));
                            if (success)
                            {
                                success = SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, deviceInfoSet, deviceInfoData);
                                if (!success)
                                {
                                    throw new Exception("SetupDiCallClassInstaller failed.");
                                }
                            }

                            Marshal.FreeHGlobal(classInstallParamsPtr);
                        }
                    }

                    Marshal.FreeHGlobal(propertyBuffer);
                }

                success = SetupDiEnumDeviceInfo(deviceInfoSet, deviceInfoData.DevInst, ref deviceInfoData);
            }

            // デバイス情報セットを解放
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }
}
