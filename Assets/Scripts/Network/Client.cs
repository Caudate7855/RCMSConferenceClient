using Mirror;
using UnityEngine;

public class Client : NetworkBehaviour
{
    public GameObject visualPrefab;

    public override void OnStartClient()
    {
        var visual = Instantiate(visualPrefab);
        visual.GetComponent<FollowTarget>().target = gameObject;
    }
}