using System;
using System.Collections.Generic;
using UnityEngine;

namespace LayerLabAsset
{
    public class FsmClass<T> where T : Enum
    {
        private readonly Dictionary<T, FsmState<T>> _stateList = new();
        private bool _isStateChanging;

        public FsmState<T> GetState { get; private set; }

        public T GetStateType
        {
            get
            {
                if (null == GetState)
                    return default;

                return GetState.StateType;
            }
        }

        public virtual void Init()
        {
        }

        public virtual void Clear()
        {
            _stateList.Clear();
            GetState = null;
        }

        public virtual void AddState(FsmState<T> state)
        {
            if (null == state)
            {
                Debug.LogError("FsmClass::AddFsm()[ null == FsmState<T>");
                return;
            }

            if (!_stateList.TryAdd(state.StateType, state))
            {
                Debug.LogError("FsmClass::AddFsm()[ have state : " + state.StateType);
            }
        }

        public virtual void SetState(T stateType)
        {
            if (false == _stateList.ContainsKey(stateType))
            {
                Debug.LogError("FsmClass::SetState()[ no have state : " + stateType);
                return;
            }

            if (_isStateChanging)
            {
                Debug.LogError("FsmClass::SetState()[ change state : " + stateType);
                return;
            }

            var nextState = _stateList[stateType];
            if (nextState == GetState)
            {
                Debug.LogWarning("FsmClass::SetState()[ same state : " + stateType);
                return;
            }

            _isStateChanging = true;

            if (null != GetState)
            {
                GetState.Exit();
            }

            //게임컨트롤 게임상태 변경
            GetState = nextState;
응 
            //Chaning 완료 처리후 Enter 진입 (처리가 되지않아 Enter에서 상태바꿀시 에러가 남)
            _isStateChanging = false;
            GetState.Enter();
        }

        public virtual void Update()
        {
            if (null == GetState)
                return;

            GetState.Update();
        }
    }
}