using System;
using System.Collections.Generic;

namespace Services
{
    [Serializable]
    public class VideoInfos
    {
        public List<VideoInfo> VideoToDownloadInfos;
    }
    
    
    [Serializable]
    public class VideoInfo
    {
        public string id;
        public string title;
        public string format;
        public string url;
        public string processed_url;
        public string preview_url;
    }
}