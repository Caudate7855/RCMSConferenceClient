using JetBrains.Annotations;

namespace Services
{
    [UsedImplicitly]
    public class RemoteContentContainer
    {
        public bool HasLoadedContentInMediaPlayer;
        [CanBeNull] public string VideoTitle;
        public VideoPlaybackStates? VideoPlaybackState;
        public VideoManagementAction? VideoAction;
        public int? CurrentDuration;
    }
}