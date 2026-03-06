namespace InterStella.Game.Netcode.Runtime
{
    public interface INetworkAuthorityGateway
    {
        bool IsHostAuthority { get; }
        bool IsAuthoritativeOwner(int ownerId);
        bool TryCommitAuthoritativeAction(string actionName, int requesterId);
    }
}
