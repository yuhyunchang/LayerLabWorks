using UnityEngine;

namespace LayerLabAsset
{
    // 공통 FSM 상태 베이스 클래스
    public abstract class CharacterStateBase<T> where T : MonoBehaviour
    {
        protected T Owner { get; }
        public T StateType { get; }

        protected CharacterStateBase(T owner, T stateType)
        {
            Owner = owner;
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