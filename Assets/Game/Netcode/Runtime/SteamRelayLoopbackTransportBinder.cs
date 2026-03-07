using System;
using FishNet.Managing;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class SteamRelayLoopbackTransportBinder : MonoBehaviour, ISteamRelayTransportBinder
    {
        [SerializeField]
        private bool _allowHostIdEndpointOverride = true;

        [SerializeField]
        private string _fallbackAddress = "127.0.0.1";

        [SerializeField]
        private ushort _fallbackPort = 7770;

        public bool TryApplyBootstrap(
            NetworkManager networkManager,
            bool isHostMode,
            string lobbyId,
            string hostId,
            ref string clientAddress,
            ref ushort port,
            out string details)
        {
            if (networkManager == null)
            {
                details = "NetworkManager is missing.";
                return false;
            }

            if (isHostMode)
            {
                details = $"Host mode relay bootstrap accepted. lobbyId={lobbyId}.";
                return true;
            }

            if (_allowHostIdEndpointOverride && TryParseEndpoint(hostId, out string parsedAddress, out ushort parsedPort))
            {
                clientAddress = parsedAddress;
                port = parsedPort;
                details = $"Client endpoint resolved from hostId={hostId} -> {clientAddress}:{port}.";
                return true;
            }

            clientAddress = string.IsNullOrWhiteSpace(_fallbackAddress) ? "127.0.0.1" : _fallbackAddress.Trim();
            port = _fallbackPort;
            details = $"Client endpoint used fallback={clientAddress}:{port}.";
            return true;
        }

        public static bool TryParseEndpoint(string rawHostId, out string address, out ushort port)
        {
            address = string.Empty;
            port = 0;
            if (string.IsNullOrWhiteSpace(rawHostId))
            {
                return false;
            }

            string candidate = rawHostId.Trim();
            int separator = candidate.LastIndexOf(':');
            if (separator <= 0 || separator >= candidate.Length - 1)
            {
                return false;
            }

            string endpointAddress = candidate.Substring(0, separator);
            string endpointPort = candidate.Substring(separator + 1);
            if (string.IsNullOrWhiteSpace(endpointAddress) || !ushort.TryParse(endpointPort, out ushort parsedPort))
            {
                return false;
            }

            address = endpointAddress;
            port = parsedPort;
            return true;
        }
    }
}
