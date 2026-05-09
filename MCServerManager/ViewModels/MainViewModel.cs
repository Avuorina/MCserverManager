using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using MCServerManager.Models;
using MCServerManager.Mvvm;
using MCServerManager.Services;
using MineStatLib;

namespace MCServerManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ServerInfo> _servers = new();
        private ServerInfo? _selectedServer;
        private static readonly HttpClient _httpClient = new HttpClient();
        private DispatcherTimer _refreshTimer;
        private string _globalIp = "取得中...";
        private bool _isAutoRefreshEnabled = false;

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (_isAutoRefreshEnabled != value)
                {
                    _isAutoRefreshEnabled = value;
                    OnPropertyChanged();
                    if (_isAutoRefreshEnabled)
                    {
                        _refreshTimer.Start();
                    }
                    else
                    {
                        _refreshTimer.Stop();
                    }
                }
            }
        }

        public ObservableCollection<ServerInfo> Servers
        {
            get => _servers;
            set
            {
                _servers = value;
                OnPropertyChanged();
            }
        }

        public ServerInfo? SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddServerCommand { get; }
        public ICommand DeleteServerCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand StartServerCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand CopyIpCommand { get; }

        public MainViewModel()
        {
            AddServerCommand = new RelayCommand(_ => AddServer());
            DeleteServerCommand = new RelayCommand(_ => DeleteServer(), _ => SelectedServer != null);
            RefreshCommand = new RelayCommand(_ => LoadServers());
            StartServerCommand = new RelayCommand(param => StartServer(param as ServerInfo));
            OpenFolderCommand = new RelayCommand(param => OpenFolder(param as ServerInfo));
            CopyIpCommand = new RelayCommand(param => CopyIp(param as ServerInfo));

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _refreshTimer.Tick += (s, e) => { _ = UpdatePlayerCountsAsync(); };

            LoadServers();
            _ = FetchGlobalIpAsync();
        }

        private async Task FetchGlobalIpAsync()
        {
            try
            {
                _globalIp = await _httpClient.GetStringAsync("https://api.ipify.org");
            }
            catch
            {
                _globalIp = "127.0.0.1"; // 取得失敗時のフォールバック
            }
        }

        private void CopyIp(ServerInfo? server)
        {
            if (server == null) return;
            string text = _globalIp;
            Clipboard.SetText(text);
            MessageBox.Show($"「{text}」をクリップボードにコピーしました！", "コピー完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetSaveFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "MCServerManager");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "servers.json");
        }

        private void LoadServers()
        {
            try
            {
                string path = GetSaveFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<ObservableCollection<ServerInfo>>(json);
                    if (list != null)
                    {
                        foreach (var server in list)
                        {
                            var existing = Servers.FirstOrDefault(s => s.FolderPath == server.FolderPath);
                            if (existing != null)
                            {
                                server.IsRunning = existing.IsRunning;
                            }
                        }
                        Servers = list;
                    }
                }
                
                // ロード後に非同期でプレイヤー数を取得するわ
                _ = UpdatePlayerCountsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存データの読み込みに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveServers()
        {
            try
            {
                string path = GetSaveFilePath();
                string json = JsonSerializer.Serialize(Servers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存データの書き込みに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddServer()
        {
            // AddServerWindowはView層の責務だけど、シンプルなMVVMということで直接インスタンス化しましょ
            var vm = new AddServerViewModel();
            var window = new AddServerWindow { DataContext = vm };
            vm.RequestClose += () => window.Close();
            
            window.ShowDialog();

            if (vm.IsSaved)
            {
                var server = new ServerInfo
                {
                    Name = vm.ServerName,
                    FolderPath = vm.FolderPath,
                    BatFilePath = vm.BatFilePath,
                    Port = ServerManagerService.DetectPort(vm.FolderPath)
                };
                Servers.Add(server);
                SaveServers();
            }
        }

        private void DeleteServer()
        {
            if (SelectedServer != null)
            {
                var result = MessageBox.Show($"本当に {SelectedServer.Name} を削除してもよろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Servers.Remove(SelectedServer);
                    SaveServers();
                }
            }
        }

        private async void StartServer(ServerInfo? server)
        {
            if (server == null) return;

            if (!Directory.Exists(server.FolderPath) || !File.Exists(server.BatFilePath))
            {
                MessageBox.Show("フォルダまたは.batファイルが存在しません。パスを確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. ポート検出
            server.Port = ServerManagerService.DetectPort(server.FolderPath);
            
            // 2. Windowsファイアウォール開放 (netsh)
            ServerManagerService.OpenWindowsFirewall(server.Name, server.Port);

            // 3. UPnPポート開放
            await ServerManagerService.TryOpenUPnPAsync(server.Name, server.Port);

            // 4. サーバー起動
            try
            {
                ServerManagerService.StartServer(server.FolderPath, server.BatFilePath);
                server.IsRunning = true;
                
                MessageBox.Show("サーバーの起動とポート開放を実行しました。\nコンソールウィンドウを確認してください。", "起動完了", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 再描画のため
                var index = Servers.IndexOf(server);
                if(index >= 0) {
                    Servers[index] = new ServerInfo { Name = server.Name, FolderPath = server.FolderPath, BatFilePath = server.BatFilePath, Port = server.Port, IsRunning = server.IsRunning };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"起動中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(ServerInfo? server)
        {
            if (server != null)
            {
                ServerManagerService.OpenExplorer(server.FolderPath);
            }
        }

        private async Task UpdatePlayerCountsAsync()
        {
            var tasks = Servers.Select(async server =>
            {
                await Task.Run(() =>
                {
                    try
                    {
                        // ローカルホストに対して、タイムアウト5秒でPingを送るわ
                        var ms = new MineStat("127.0.0.1", (ushort)server.Port, 5, SlpProtocol.Automatic);
                        if (ms.ServerUp)
                        {
                            server.PlayerCount = $"{ms.CurrentPlayers} / {ms.MaximumPlayers}";
                            server.IsRunning = true; // Ping応答があれば確実に起動中ね
                        }
                        else
                        {
                            server.PlayerCount = "- / -";
                            server.IsRunning = false;
                        }
                    }
                    catch
                    {
                        server.PlayerCount = "- / -";
                        server.IsRunning = false;
                    }
                });
            });

            await Task.WhenAll(tasks);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
