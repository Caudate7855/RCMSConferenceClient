using System.Collections.Generic;
using UIManager;
using UnityEngine;

[CreateAssetMenu(fileName = "PopUpsContainer", menuName = "SO/Containers/PopUpsContainer")]
public class PopUpsContainer : ScriptableObject
{
    public List<PopUpBase> PopUps;
}