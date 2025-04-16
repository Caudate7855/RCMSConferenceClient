using System;
using System.Collections.Generic;

namespace Common
{
    public abstract class FSM
    {
        protected FsmStateBase StateBaseCurrent;
        private Dictionary<Type, FsmStateBase> _states = new Dictionary<Type, FsmStateBase>();

        protected void AddState(FsmStateBase stateBase) => _states.Add(stateBase.GetType(), stateBase);

        public void SetState<T>() where T : FsmStateBase
        {
            var type = typeof(T);

            if (StateBaseCurrent?.GetType() == type)
                return;

            if (_states.TryGetValue(type, out var newState))
            {
                StateBaseCurrent?.Exit();
                StateBaseCurrent = newState;
                StateBaseCurrent.Enter();
            }
        }
    }
}