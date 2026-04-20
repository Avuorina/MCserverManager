using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using MCServerManager.Mvvm;

namespace MCServerManager.ViewModels
{
    public class AddServerViewModel : INotifyPropertyChanged
    {
        private string _serverName = string.Empty;
        private string _folderPath = string.Empty;
        private string _batFilePath = string.Empty;

        public event Action? RequestClose;

        public string ServerName
        {
            get => _serverName;
            set
            {
                _serverName = value;
                OnPropertyChanged();
                ((RelayCommand)SaveCommand).CanExecuteChanged += null; 
            }
        }

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged();
                
                if (string.IsNullOrWhiteSpace(ServerName) && !string.IsNullOrWhiteSpace(_folderPath))
                {
                    ServerName = new DirectoryInfo(_folderPath).Name;
                }
                AutoDetectBatFile();
            }
        }

        public string BatFilePath
        {
            get => _batFilePath;
            set
            {
                _batFilePath = value;
                OnPropertyChanged();
            }
        }

        public ICommand BrowseFolderCommand { get; }
        public ICommand BrowseBatCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsSaved { get; private set; }

        public AddServerViewModel()
        {
            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
            BrowseBatCommand = new RelayCommand(_ => BrowseBat());
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "サーバーのフォルダを選択してちょうだい"
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPath = dialog.FolderName;
            }
        }

        private void BrowseBat()
        {
            var dialog = new OpenFileDialog
            {
                Title = "起動用のbatファイルを選んでね",
                Filter = "バッチファイル (*.bat)|*.bat|すべてのファイル (*.*)|*.*",
                InitialDirectory = string.IsNullOrWhiteSpace(FolderPath) ? Environment.GetFolderPath(Environment.SpecialFolder.MyComputer) : FolderPath
            };

            if (dialog.ShowDialog() == true)
            {
                BatFilePath = dialog.FileName;
            }
        }

        private void AutoDetectBatFile()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath)) return;

            var batFiles = Directory.GetFiles(FolderPath, "*.bat");
            if (batFiles.Length > 0)
            {
                // start.bat や run.bat を優先して探すわ
                var preferred = batFiles.FirstOrDefault(f => 
                    f.EndsWith("start.bat", StringComparison.OrdinalIgnoreCase) || 
                    f.EndsWith("run.bat", StringComparison.OrdinalIgnoreCase));
                
                BatFilePath = preferred ?? batFiles[0];
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ServerName) &&
                   !string.IsNullOrWhiteSpace(FolderPath) &&
                   !string.IsNullOrWhiteSpace(BatFilePath) &&
                   Directory.Exists(FolderPath) &&
                   File.Exists(BatFilePath);
        }

        private void Save()
        {
            IsSaved = true;
            RequestClose?.Invoke();
        }

        private void Cancel()
        {
            IsSaved = false;
            RequestClose?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
