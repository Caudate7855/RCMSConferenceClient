using Photon.Pun;
using UnityEngine;

namespace Network
{
    public class ClientController : MonoBehaviourPun
    {
        private GameObject _followObject;
        
        private void Awake()
        {
            _followObject = GameObject.FindObjectOfType<Pvr_UnitySDKManager>(true).gameObject;
        }

        private void Update()
        {
            transform.position = _followObject.transform.position;
            transform.rotation = _followObject.transform.rotation;
        }
    }
}