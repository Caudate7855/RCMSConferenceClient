using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ActivatorBase : MonoBehaviour
{
    public event Action<bool> OnActivationChanged;

    [SerializeField] private bool _invertStateForLinked;
    
    public List<ActivatorBase> _linkedActivators = new List<ActivatorBase>();

    public bool ActiveState { get; private set; }

    public virtual UniTask Activate(bool state, bool withAnimation = false)
    {
        ActivateLinked(state, withAnimation);

        ActiveState = state;
        
        OnActivationChanged?.Invoke(state);
        
        return UniTask.CompletedTask;
    }

    public virtual void ActivateLinked(bool state, bool withAnimation = false)
    {
        foreach (var activator in _linkedActivators.Where(activator => activator != this && activator != null))
            activator.Activate(_invertStateForLinked ? !state : state, withAnimation);
    }

    public UniTask Deactivate(bool withAnimation)
    {
        Activate(false, withAnimation);
        
        return UniTask.CompletedTask;
    }
}