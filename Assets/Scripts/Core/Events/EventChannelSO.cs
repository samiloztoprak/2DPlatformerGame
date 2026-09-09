using System;
using UnityEngine;

namespace Game.Core.Events
{
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnEventRaised;

        public void Raise(T value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
