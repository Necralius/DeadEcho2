using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Events;

namespace Game.Core.Input
{
    public sealed class InputService : IInputService
    {
        private readonly IEventBus _eventBus;
        private readonly InputActionAsset _asset;

        private InputActionMap _player;

        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _crouch;
        private InputAction _sprint;
        private InputAction _fire;
        private InputAction _ads;
        private InputAction _reload;
        private InputAction _flashlight;

        // latched "pressed this frame"
        private bool _jumpPressed;
        private bool _firePressed;
        private bool _reloadPressed;
        private bool _flashlightPressed;

        public InputSnapshot Current { get; private set; }

        public InputService(IEventBus eventBus, InputActionAsset inputAsset)
        {
            _eventBus = eventBus;
            _asset = inputAsset;

            if (_asset == null)
            {
                Debug.LogError("InputService: InputActionAsset é null. Atribua no CoreBootstrapper.");
                return;
            }

            _player = _asset.FindActionMap("Player", throwIfNotFound: true);

            _move = _player.FindAction("Move", throwIfNotFound: true);
            _look = _player.FindAction("Look", throwIfNotFound: true);
            _jump = _player.FindAction("Jump", throwIfNotFound: true);
            _crouch = _player.FindAction("Crouch", throwIfNotFound: true);
            _sprint = _player.FindAction("Sprint", throwIfNotFound: true);
            _fire = _player.FindAction("Fire", throwIfNotFound: true);
            _ads = _player.FindAction("ADS", throwIfNotFound: true);
            _reload = _player.FindAction("Reload", throwIfNotFound: true);
            _flashlight = _player.FindAction("Flashlight", throwIfNotFound: true);

            // Bind callbacks (pressed this frame)
            _jump.performed += _ => _jumpPressed = true;
            _fire.performed += _ => _firePressed = true;
            _reload.performed += _ => _reloadPressed = true;
            _flashlight.performed += _ => _flashlightPressed = true;

            Current = default;
        }

        public void Enable()
        {
            _player?.Enable();
        }

        public void Disable()
        {
            _player?.Disable();
        }

        public void Tick(float dt)
        {
            if (_asset == null || _player == null) return;

            // Read continuous values
            var move = _move.ReadValue<Vector2>();
            var look = _look.ReadValue<Vector2>();

            // Read held buttons
            bool crouchHeld = _crouch.IsPressed();
            bool sprintHeld = _sprint.IsPressed();
            bool fireHeld = _fire.IsPressed();
            bool adsHeld = _ads.IsPressed();

            // Create snapshot
            Current = new InputSnapshot(
                move, look,
                jumpPressed: _jumpPressed,
                crouchHeld: crouchHeld,
                sprintHeld: sprintHeld,
                firePressed: _firePressed,
                fireHeld: fireHeld,
                adsHeld: adsHeld,
                reloadPressed: _reloadPressed,
                toggleFlashlightPressed: _flashlightPressed
            );

            // Publish one-frame events
            if (_jumpPressed) _eventBus.Publish(new JumpPressedEvent());
            if (_firePressed) _eventBus.Publish(new FirePressedEvent());
            if (_reloadPressed) _eventBus.Publish(new ReloadPressedEvent());
            if (_flashlightPressed) _eventBus.Publish(new FlashlightToggledEvent());

            // Reset pressed latches
            _jumpPressed = false;
            _firePressed = false;
            _reloadPressed = false;
            _flashlightPressed = false;
        }
    }
}