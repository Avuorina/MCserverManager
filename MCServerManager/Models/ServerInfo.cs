using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MCServerManager.Models
{
    public class ServerInfo : INotifyPropertyChanged
    {
        private bool _isRunning = false;
        private string _playerCount = "- / -";

        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string BatFilePath { get; set; } = string.Empty;
        public int Port { get; set; } = 25565;
        
        [JsonIgnore]
        public string Status => IsRunning ? "起動中" : "停止中";

        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        [JsonIgnore]
        public string PlayerCount
        {
            get => _playerCount;
            set
            {
                if (_playerCount != value)
                {
                    _playerCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
