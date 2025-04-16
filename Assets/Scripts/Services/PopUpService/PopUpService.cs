using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UIManager;
using UnityEngine;
using Zenject;

namespace Services
{
    [UsedImplicitly]
    public class PopUpService
    {
        [Inject] private PopUpsContainer _popUpsContainer;
        [Inject] private AppFSM _appFsm;

        private PopUpCanvas _popUpCanvas;
        private PopUpBase _activePopUp;
        private List<PopUpBase> _popUps = new List<PopUpBase>();

        public PopUpService()
        {
            _popUpCanvas = Object.FindObjectOfType<PopUpCanvas>();
            InstantiateAllPopUps();
        }

        public void OpenPopup<T>() where T : PopUpBase
        {
            var popup = _popUps.OfType<T>().FirstOrDefault();
            _activePopUp = _popUps.FirstOrDefault(t => t.isActiveAndEnabled);

            if (popup == null)
            {   
                Debug.LogError($"Cannot open popup of type {popup}");
                return;
            }

            if (_activePopUp == popup)
            {
                Debug.Log("Pop up is already open");
                return;
            }

            if (_activePopUp != null && _activePopUp.PriorityIndex > popup.PriorityIndex)
            {
                Debug.Log("Current popUp has higher priority");
                return;
            }

            if (_activePopUp != null)
                _activePopUp.Close();

            popup.Open();
        }

        public T GetPopUp<T>() where T : PopUpBase => 
            _popUps.FirstOrDefault(t => t is T) as T;

        private async void InstantiateAllPopUps()
        {
            await UniTask.WaitWhile(() => _popUpsContainer == null);
            
            _popUpCanvas.gameObject.SetActive(true);

            foreach (PopUpBase popup in _popUpsContainer.PopUps)
            {
                PopUpBase instantiatedPopUp = Object.Instantiate(popup, _popUpCanvas.transform);
                instantiatedPopUp.AppFsm = _appFsm;
                _popUps.Add(instantiatedPopUp);
                instantiatedPopUp.gameObject.SetActive(false);
            }
        }
    }
}