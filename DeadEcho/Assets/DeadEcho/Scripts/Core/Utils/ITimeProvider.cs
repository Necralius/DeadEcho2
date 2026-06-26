namespace Game.Core.Utils
{
    public interface ITimeProvider
    {
        float DeltaTime { get; }
        float Time { get; }
    }
}