using Services;
using TMPro;
using UIManager.Windows.VideoWindow;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = CustomDebug.Debug;

namespace UIManager.Windows
{
    public class DownloadInfoWindow : MonoBehaviour
    {
        [SerializeField] private Slider _downloadingSlider;
        [SerializeField] private TMP_Text _downloadContentName;
        [SerializeField] private TMP_Text _downloadContentCounter;
        [SerializeField] private TMP_Text _downloadedSize;
        [SerializeField] private LoadingCircle _loadingCircle;
        [SerializeField] private GameObject _contentCountObject;
        [SerializeField] private GameObject _downloadedContentObject;
        
        private long _downloadedBytes;
        private long _totalBytes;


        private void OnEnable()
        {
            SetLoadingCircleState();
        }

        public void SetDownloadingState()
        {
            _loadingCircle.gameObject.SetActive(false);
            _contentCountObject.gameObject.SetActive(true);
            _downloadedContentObject.gameObject.SetActive(true);
        }

        private void SetLoadingCircleState()
        {
            _loadingCircle.gameObject.SetActive(true);
            _contentCountObject.gameObject.SetActive(false);
            _downloadedContentObject.gameObject.SetActive(false);
        }
        
        public void UpdateSliderValue(float currentValue, float maxValue)
        {
            SetDownloadingState();
            _downloadingSlider.maxValue = maxValue;
            _downloadingSlider.value = currentValue;
        }

        public void UpdateDownloadableContentInfo(string currentContentName, int downloadedContentSizeValue, int totalContentSizeValue)
        {
            if (currentContentName.Length > 25) 
                currentContentName = currentContentName.Substring(0, 25) + "...";

            _downloadContentName.text = currentContentName;

            _downloadContentCounter.text = $"{downloadedContentSizeValue.ToString()}/{totalContentSizeValue.ToString()}";
        }
        
        public void UpdateDownloadedSize(long downloadedBytes, long totalBytes)
        {
            _downloadedBytes = downloadedBytes;
            _totalBytes = downloadedBytes;

            var downloadedFormatted = FormatBytes(downloadedBytes);
            var totalFormatted = FormatBytes(totalBytes);

            _downloadedSize.text = $"{downloadedFormatted} / {totalFormatted}";
        }
        
        public void UpdateDownloadedSize() => UpdateDownloadedSize(_downloadedBytes, _totalBytes);
        
        private string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
                
            if (bytes >= GB)
                return ($"{(bytes / (double)GB):F2} {LocalizationManager.GetLocalizedTextAsync("Gb")}");
            else if (bytes >= MB)
                return ($"{(bytes / (double)MB):F2} {LocalizationManager.GetLocalizedTextAsync("Mb")}");
            else if (bytes >= KB)
                return ($"{(bytes / (double)KB):F2} {LocalizationManager.GetLocalizedTextAsync("Kb")}");
            else
                return ($"{bytes} {LocalizationManager.GetLocalizedTextAsync("B")}");
        }
    }
}