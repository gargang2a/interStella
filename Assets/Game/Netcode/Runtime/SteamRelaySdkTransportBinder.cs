using FishNet.Managing;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    /// <summary>
    /// Scaffold binder for future Steam SDK relay transport wiring.
    /// Current behavior can optionally delegate to loopback binder until real SDK integration is implemented.
    /// </summary>
    public sealed class SteamRelaySdkTransportBinder : MonoBehaviour, ISteamRelayTransportBinder
    {
        [SerializeField]
        private bool _allowLoopbackFallbackUntilSdkReady = true;

        [SerializeField]
        private SteamRelayLoopbackTransportBinder _loopbackFallback;

        [SerializeField]
        private bool _logScaffoldWarnings = true;

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

            const string scaffoldReason = "Steam SDK binder scaffold is active, but real Steam relay transport wiring is not implemented yet.";
            if (_allowLoopbackFallbackUntilSdkReady)
            {
                ResolveLoopbackFallbackIfMissing();
                if (_loopbackFallback != null && _loopbackFallback.TryApplyBootstrap(
                    networkManager,
                    isHostMode,
                    lobbyId,
                    hostId,
                    ref clientAddress,
                    ref port,
                    out string loopbackDetails))
                {
                    details = "Steam SDK scaffold delegated to loopback fallback. " + loopbackDetails;
                    if (_logScaffoldWarnings)
                    {
                        Debug.LogWarning($"[SteamRelaySdkTransportBinder] {scaffoldReason}");
                    }

                    return true;
                }
            }

            details = $"{scaffoldReason} Loopback fallback is disabled or unavailable.";
            if (_logScaffoldWarnings)
            {
                Debug.LogWarning($"[SteamRelaySdkTransportBinder] {details}");
            }

            return false;
        }

        private void Awake()
        {
            ResolveLoopbackFallbackIfMissing();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveLoopbackFallbackIfMissing();
        }
#endif

        private void ResolveLoopbackFallbackIfMissing()
        {
            if (_loopbackFallback == null)
            {
                _loopbackFallback = GetComponent<SteamRelayLoopbackTransportBinder>();
            }
        }
    }
}
