namespace InterStella.Game.Netcode.Runtime
{
    public interface ISteamLobbyService
    {
        bool TryCreateLobby(out string lobbyId, out string hostSteamId, out string details);
        bool TryJoinLobby(string lobbyId, out string hostSteamId, out string details);
        bool TryInviteUser(string lobbyId, string targetSteamId, out string details);
        bool TryConsumePendingInvite(out string lobbyId);
        void LeaveLobby(string lobbyId);
    }
}
