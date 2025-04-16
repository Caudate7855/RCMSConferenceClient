using System;
using JetBrains.Annotations;

namespace Services.GetExchange
{
    [Serializable]
    public class ExchangeInfo
    {
        [CanBeNull] public string session_title;
        [CanBeNull] public string volume;
        [CanBeNull] public Player player;
        [CanBeNull] public MediaManagement management;
    }
    
    [Serializable]
    public class Player
    {
        [CanBeNull] public string id;
        [CanBeNull] public string title;
        [CanBeNull] public string current_duration;
        [CanBeNull] public string format;
        [CanBeNull] public string playback_state;
        
    }

    [Serializable]
    public class MediaManagement
    {
        [CanBeNull] public bool need_centering;
        [CanBeNull] public string action;
    }
}