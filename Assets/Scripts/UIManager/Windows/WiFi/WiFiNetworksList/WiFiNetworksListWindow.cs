using System;
using System.Collections.Generic;
using DG.Tweening;
using UIManager.UISystem.Abstracts;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager.Windows
{
    public class WiFiNetworksListWindow : UIViewBase
    {
        [SerializeField] private ButtonBase _backButton;
        [SerializeField] private Image _loaderImage;
        
        [SerializeField] private Transform _networksScroll;
        
        [SerializeField] private NetworkButton _networkButton;
        
        public ButtonBase BackButton => _backButton;
        public Transform NetworkScroll => _networksScroll;
        public NetworkButton NetworkButton => _networkButton;

        public event Action OnUpdateWifiListCommand;

        private void OnEnable()
        {
            _loaderImage.transform.DORotate(new Vector3(0, 0, -360), 1, RotateMode.FastBeyond360).SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);

            InvokeRepeating(nameof(TriggerUpdateListEvent), 0, 30);
        }

        private void OnDisable()
        {
            _loaderImage.DOKill();
            _loaderImage.transform.rotation = Quaternion.identity;

            CancelInvoke(nameof(TriggerUpdateListEvent));
        }

        private void TriggerUpdateListEvent() =>
            OnUpdateWifiListCommand?.Invoke();
    }
}