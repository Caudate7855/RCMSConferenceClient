using Mirror;
using UnityEngine;

public class FollowTarget : NetworkBehaviour
{
    public GameObject target;

    public void Awake()
    {
        if (isLocalPlayer)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
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