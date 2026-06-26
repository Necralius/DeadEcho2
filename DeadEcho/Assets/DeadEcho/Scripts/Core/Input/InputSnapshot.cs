using UnityEngine;

namespace Game.Core.Input
{
    public readonly struct InputSnapshot
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;

        public readonly bool JumpPressed;
        public readonly bool CrouchHeld;
        public readonly bool SprintHeld;

        public readonly bool FirePressed;
        public readonly bool FireHeld;

        public readonly bool ADSHeld;
        public readonly bool ReloadPressed;

        public readonly bool ToggleFlashlightPressed;

        public InputSnapshot(
            Vector2 move, Vector2 look,
            bool jumpPressed, bool crouchHeld, bool sprintHeld,
            bool firePressed, bool fireHeld,
            bool adsHeld, bool reloadPressed,
            bool toggleFlashlightPressed)
        {
            Move = move;
            Look = look;
            JumpPressed = jumpPressed;
            CrouchHeld = crouchHeld;
            SprintHeld = sprintHeld;
            FirePressed = firePressed;
            FireHeld = fireHeld;
            ADSHeld = adsHeld;
            ReloadPressed = reloadPressed;
            ToggleFlashlightPressed = toggleFlashlightPressed;
        }
    }
}
