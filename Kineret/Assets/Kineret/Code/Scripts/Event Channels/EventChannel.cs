using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class EventChannel<T> : ScriptableObject
{
    public event UnityAction<T> OnEventRaised;

    public void RaiseEvent(T value)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(value);
    }
}
