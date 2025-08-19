namespace Base.Network.Provider
{
    public interface ITickSource
    {
        uint ServerTick { get; }
        uint ClientTick { get; }
    }
}