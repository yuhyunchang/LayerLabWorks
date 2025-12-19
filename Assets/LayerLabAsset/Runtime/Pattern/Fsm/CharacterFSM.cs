using System.Collections.Generic;
using UnityEngine;

namespace LayerLabAsset
{
    public class CharacterFSM<T> where T : MonoBehaviour
    {
        private readonly Dictionary<T, CharacterStateBase<T>> _states = new();
        private CharacterStateBase<T> _currentState;
        private bool _isChanging;

        public CharacterStateBase<T> CurrentState => _currentState;

        public void AddState(CharacterStateBase<T> state)
        {
            if (state == null)
            {
                Debug.LogError("CharacterFSM::AddState() - state is null");
                return;
            }

            if (!_states.TryAdd(state.StateType, state))
            {
                Debug.LogError($"CharacterFSM::AddState() - state already exists: {state.StateType}");
            }
        }

        public void ChangeState(T nextState)
        {
            if (!_states.ContainsKey(nextState))
            {
                Debug.LogError($"CharacterFSM::ChangeStateAsync() - state not found: {nextState}");
                return;
            }

            if (_isChanging)
            {
                Debug.LogWarning($"CharacterFSM::ChangeStateAsync() - already changing to: {nextState}");
                return;
            }

            var newState = _states[nextState];
            if (newState == _currentState)
            {
                return;
            }

            _isChanging = true;

            _currentState?.Exit();
            _currentState = newState;

            _isChanging = false;
            _currentState.Enter();
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public bool IsCurrentState<TState>() where TState : CharacterStateBase<T>
        {
            return _currentState is TState;
        }
    }
}