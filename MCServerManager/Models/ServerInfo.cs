using System;
using System.Text.Json.Serialization;

namespace MCServerManager.Models
{
    public class ServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string BatFilePath { get; set; } = string.Empty;
        public int Port { get; set; } = 25565;
        
        [JsonIgnore]
        public string Status => IsRunning ? "起動中" : "停止中";

        [JsonIgnore]
        public bool IsRunning { get; set; } = false;
    }
}
