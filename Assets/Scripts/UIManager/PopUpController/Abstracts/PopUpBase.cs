using System;
using UnityEngine;

namespace UIManager
{
    public abstract class PopUpBase : MonoBehaviour
    {
        public event Action OnClosePerformed;
        
        public int PriorityIndex;
        public AppFSM AppFsm;

        public void Open()
        {
            OnOpen();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            OnClose();
            OnClosePerformed?.Invoke();
            OnClosePerformed = null;
            gameObject.SetActive(false);
        }
        
        protected virtual void OnOpen() { }

        protected virtual void OnClose() { }
    }
}