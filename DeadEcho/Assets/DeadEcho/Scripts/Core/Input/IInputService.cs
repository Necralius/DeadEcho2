namespace Game.Core.Input
{
    public interface IInputService
    {
        InputSnapshot Current { get; }
        void Enable();
        void Disable();
        void Tick(float dt);
    }
}
