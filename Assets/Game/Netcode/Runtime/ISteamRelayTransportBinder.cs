using FishNet.Managing;

namespace InterStella.Game.Netcode.Runtime
{
    public interface ISteamRelayTransportBinder
    {
        bool TryApplyBootstrap(
            NetworkManager networkManager,
            bool isHostMode,
            string lobbyId,
            string hostId,
            ref string clientAddress,
            ref ushort port,
            out string details);
    }
}
