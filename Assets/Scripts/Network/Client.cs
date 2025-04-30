using Mirror;
using UnityEngine;

public class Client : NetworkBehaviour
{
    public GameObject visualPrefab;
    public bool HasVisualisation;
    [SyncVar] public uint targetNetId;

    public override void OnStartClient()
    {
        Debug.Log("netnet client");
        
        var visual = Instantiate(visualPrefab);
        visual.GetComponent<FollowTarget>().target = gameObject;

        foreach (var net in NetworkClient.spawned)
        {
            Debug.Log($"netnet = {net.Key} {net.Value}");
        }
        
        if (NetworkClient.spawned.TryGetValue(targetNetId, out var target))
        {
            Debug.Log($"-------{target.name}");
        }
    }
}