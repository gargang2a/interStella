namespace InterStella.Game.Netcode.Runtime
{
    public interface ISessionService
    {
        bool IsSessionActive { get; }
        bool IsHost { get; }
        bool StartSession();
        void StopSession();
    }
}
