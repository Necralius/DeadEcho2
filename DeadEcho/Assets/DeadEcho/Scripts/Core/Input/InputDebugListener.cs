using Game.Core.Bootstrap;
using Game.Core.Events;
using Game.Core.Input;
using System;
using UnityEngine;

public sealed class InputDebugListener : MonoBehaviour
{
    private IDisposable _subFire;
    private IDisposable _subJump;

    private void Start()
    {
        var core = FindFirstObjectByType<CoreBootstrapper>();
        var bus = core.EventBus;

        _subFire = bus.Subscribe<FirePressedEvent>(_ => Debug.Log("Fire pressed"));
        _subJump = bus.Subscribe<JumpPressedEvent>(_ => Debug.Log("Jump pressed"));
    }

    private void OnDestroy()
    {
        _subFire?.Dispose();
        _subJump?.Dispose();
    }
}