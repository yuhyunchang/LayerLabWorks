using System;

namespace LayerLabAsset
{
    public class FsmState<T> where T : Enum
    {
        public T StateType { get; }

        public FsmState(T stateType)
        {
            StateType = stateType;
        }

        public virtual void Enter()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
        }
    }
}