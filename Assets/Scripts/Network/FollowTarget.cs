using Mirror;
using UnityEngine;

public class FollowTarget : NetworkBehaviour
{
    public GameObject target;
    public GameObject View;

    public void Start()
    {
        if (isLocalPlayer)
        {
            View.SetActive(false);
            target = FindObjectOfType<Pvr_UnitySDKHeadTrack>().gameObject;
        }
        else
        {
            View.SetActive(true);
        }
    }

    void Update()
    {
        if (target == null)
            return;
        
        transform.position = target.transform.position;
        transform.rotation = target.transform.rotation;
    }
}