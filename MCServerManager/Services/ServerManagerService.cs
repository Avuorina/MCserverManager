using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SharpOpenNat;

namespace MCServerManager.Services
{
    public class ServerManagerService
    {
        public static int DetectPort(string folderPath)
        {
            string propertiesFilePath = Path.Combine(folderPath, "server.properties");
            if (File.Exists(propertiesFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(propertiesFilePath);
                    foreach (var line in lines)
                    {
                        var match = Regex.Match(line, @"^server-port=(\d+)");
                        if (match.Success)
                        {
                            if (int.TryParse(match.Groups[1].Value, out int port))
                            {
                                return port;
                            }
                        }
                    }
                }
                catch
                {
                    // 読み込みエラー時は無視してデフォルト値を返すわよ
                }
            }
            return 25565;
        }

        public static void OpenWindowsFirewall(string serverName, int port)
        {
            try
            {
                // TCPとUDPの両方を開放するわ
                RunNetsh($"advfirewall firewall add rule name=\"Minecraft {serverName} TCP\" dir=in action=allow protocol=TCP localport={port}");
                RunNetsh($"advfirewall firewall add rule name=\"Minecraft {serverName} UDP\" dir=in action=allow protocol=UDP localport={port}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイアウォールの設定に失敗しました。管理者権限が必要です。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void RunNetsh(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                Verb = "runas", // これで管理者権限に昇格するわ
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            using (Process? process = Process.Start(psi))
            {
                process?.WaitForExit();
            }
        }

        public static async Task TryOpenUPnPAsync(string serverName, int port)
        {
            try
            {
                // 10秒でタイムアウトするわ
                var device = await OpenNat.Discoverer.DiscoverDeviceAsync();

                // TCPとUDPのマッピングを追加するわ
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, $"Minecraft Server - {serverName} (TCP)"));
                await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, $"Minecraft Server - {serverName} (UDP)"));
            }
            catch (Exception)
            {
                // UPnP非対応ルーター等のエラーは無視して続行よ
                // ログに出力する場合はここを拡張してね
            }
        }

        public static void StartServer(string folderPath, string batFilePath)
        {
            if (!File.Exists(batFilePath))
            {
                throw new FileNotFoundException($"指定された.batファイルが見つかりません。パスを確認してください: {batFilePath}");
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = batFilePath,
                WorkingDirectory = folderPath,
                UseShellExecute = true // 通常のコマンドプロンプトウィンドウを表示するわ
            };

            Process.Start(psi);
        }

        public static void OpenExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                MessageBox.Show("指定されたフォルダが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
