using System;
using JetBrains.Annotations;

namespace Services.PostExchange
{
    [Serializable]
    public class DeviceDataExchange
    {
        [CanBeNull] public Settings settings;
        [CanBeNull] public Player player;
        [CanBeNull] public LoadingInfo loading_info;
    }

    [Serializable]
    public class Settings
    {
        public long free_memory;
        public int charge;
        public int? volume;
    }
    
    [Serializable]
    public class Player
    {
        public int current_duration;
        public int current_content_id;
        public string playback_state;
    }
    
    [Serializable]
    public class LoadingInfo
    {
        public int loading_content_id;
        public long loaded_bytes_of_total;
        public bool last_content;
    }
}