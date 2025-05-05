using Photon.Pun;
using UnityEngine;

namespace Network
{
    public class ClientController : MonoBehaviourPun
    {
        private GameObject _followObject;
        
        private void Awake()
        {
            if (!photonView.IsMine)
            {
                return;
            }
            
            _followObject = FindObjectOfType<Pvr_UnitySDKManager>(true).gameObject;
        }

        private void Update()
        {
            if (_followObject == null)
            {
                return;
            }
            
            transform.position = _followObject.transform.position;
            transform.rotation = _followObject.transform.rotation;
        }
    }
}