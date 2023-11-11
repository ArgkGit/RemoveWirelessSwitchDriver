using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RemoveWirelessSwitchDriver.Models
{
    internal class Common
    {

        public static async Task<bool> CountDown(int cnt, string msg)
        {
            const int DIVISION = 10;

            MainWindow.viewModel.AddLogMsg(cnt.ToString() + "秒後に" + msg);

            for(int i = cnt * DIVISION; i > 0; i--)
            {
                if (MainWindow.viewModel.CancelFlg) return false;
                if (MainWindow.viewModel.SkipFlg) return true;

                await Task.Delay(100);

                if(i % DIVISION == 0)
                {
                    MainWindow.viewModel.AddLogMsg((i/ DIVISION).ToString());
                }
            }
            return true;
        }

        public static void SerializeToXmlFile<T>(T obj, string filePath)
        {
            // XmlSerializer のインスタンスを作成
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            // ファイルストリームを作成してシリアライズ
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fileStream, obj);
            }
        }

        public static T DeserializeFromXmlFile<T>(string filePath)
        {
            // XmlSerializer のインスタンスを作成
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            // ファイルストリームを作成してデシリアライズ
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                return (T)serializer.Deserialize(fileStream);
            }
        }


        public static bool SearchDriverFile(string deviceId, out string infFilePath)
        {
            infFilePath = "";
            string searchDirectory = @"C:\Windows\INF";
            string searchString = deviceId;

            // INFファイルを検索
            string[] infFiles = Directory.GetFiles(searchDirectory, "*oem*.inf", SearchOption.TopDirectoryOnly);

            foreach (string infFile in infFiles)
            {
                if (SearchInFile(infFile, searchString))
                {
                    infFilePath = infFile;
                    return true;
                }
            }
            return false;
        }

        public static bool SearchInFile(string filePath, string searchString)
        {
            try
            {
                // ファイルを読み込み、指定された文字列を検索
                string fileContent = File.ReadAllText(filePath);
                return fileContent.Contains(searchString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
                return false;
            }
        }
    }
}
