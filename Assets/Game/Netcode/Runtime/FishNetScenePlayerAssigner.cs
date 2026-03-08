using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using InterStella.Game.Features.Player;
using InterStella.Game.Features.Repair;
using InterStella.Game.Features.Scavenge;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class FishNetScenePlayerAssigner : MonoBehaviour
    {
        [SerializeField]
        private NetworkManager _networkManager;

        [SerializeField]
        private PlayerNetworkBridge[] _playerSlots;

        [SerializeField]
        private NetworkObject[] _scenePlayerObjects;

        [SerializeField]
        private bool _despawnOnDisconnect;

        [SerializeField]
        private bool _logSlotEvents = true;

        [Header("Regression Assist")]
        [SerializeField]
        private bool _seedRemoteSlotForInteractionRegression = true;

        [SerializeField, Min(0)]
        private int _regressionSeedSlotIndex = 1;

        [SerializeField]
        private string _regressionSeedScrapName = "Scrap_03";

        [SerializeField]
        private string _regressionSeedRepairStationName = "RepairStation";

        [SerializeField, Min(0.5f)]
        private float _regressionSeedStationDistance = 1.8f;

        [SerializeField]
        private bool _logRegressionSeedEvents = true;

        [Header("Runtime Override")]
        [SerializeField]
        private bool _allowRuntimeOverride = true;

        [SerializeField]
        private bool _enableRegressionSeedInPlayerBuilds;

        [SerializeField]
        private string _enableRegressionSeedArgument = "interstella-enable-regression-seed";

        private readonly Dictionary<int, int> _clientToSlot = new Dictionary<int, int>(4);
        private readonly Queue<int> _pendingClientQueue = new Queue<int>(4);
        private readonly HashSet<int> _pendingClientSet = new HashSet<int>();
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveDependenciesIfMissing();
            ApplyRuntimeOverrides();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            TryUnsubscribe();
        }

        private void OnDestroy()
        {
            TryUnsubscribe();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void TrySubscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            ResolveDependenciesIfMissing();
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            _networkManager.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
            _isSubscribed = true;
        }

        private void TryUnsubscribe()
        {
            if (!_isSubscribed || _networkManager == null)
            {
                return;
            }

            _networkManager.SceneManager.OnClientLoadedStartScenes -= HandleClientLoadedStartScenes;
            _networkManager.ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
            _isSubscribed = false;
        }

        private void HandleClientLoadedStartScenes(NetworkConnection connection, bool asServer)
        {
            if (!asServer)
            {
                return;
            }

            AssignSlot(connection);
        }

        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                RemovePendingClient(connection.ClientId);
                ReleaseSlot(connection.ClientId);
            }
        }

        private void AssignSlot(NetworkConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            int clientId = connection.ClientId;
            RemovePendingClient(clientId);
            if (_clientToSlot.ContainsKey(clientId))
            {
                if (_logSlotEvents)
                {
                    Debug.Log($"[FishNetScenePlayerAssigner] Client {clientId} is already assigned.");
                }
                return;
            }

            int slotIndex = GetFirstAvailableSlot();
            if (slotIndex < 0)
            {
                EnqueuePendingClient(clientId);
                Debug.LogWarning($"[FishNetScenePlayerAssigner] No available slot for client {clientId}. Queued for reassignment.");
                return;
            }

            PlayerNetworkBridge bridge = GetPlayerBridge(slotIndex);
            NetworkObject scenePlayerObject = GetPlayerObject(slotIndex);
            if (bridge == null || scenePlayerObject == null)
            {
                Debug.LogWarning($"[FishNetScenePlayerAssigner] Slot {slotIndex} is missing required references.");
                return;
            }

            bridge.SetOwnerId(clientId);

            if (!scenePlayerObject.IsSpawned)
            {
                _networkManager.ServerManager.Spawn(scenePlayerObject, connection);
            }
            else
            {
                scenePlayerObject.GiveOwnership(connection);
            }

            _clientToSlot[clientId] = slotIndex;

            if (_logSlotEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Assigned client {clientId} to slot {slotIndex} ({scenePlayerObject.name}).");
            }

            TrySeedInteractionRegression(slotIndex, bridge);
        }

        private void ReleaseSlot(int clientId)
        {
            if (!_clientToSlot.TryGetValue(clientId, out int slotIndex))
            {
                return;
            }

            _clientToSlot.Remove(clientId);

            TryForceDropCarriedItem(slotIndex, clientId);

            PlayerNetworkBridge bridge = GetPlayerBridge(slotIndex);
            if (bridge != null)
            {
                bridge.SetOwnerId(-1);
            }

            NetworkObject scenePlayerObject = GetPlayerObject(slotIndex);
            if (scenePlayerObject == null || !scenePlayerObject.IsSpawned)
            {
                return;
            }

            if (_despawnOnDisconnect)
            {
                _networkManager.ServerManager.Despawn(scenePlayerObject);
                if (_logSlotEvents)
                {
                    Debug.Log($"[FishNetScenePlayerAssigner] Released slot {slotIndex} from client {clientId} and despawned {scenePlayerObject.name}.");
                }
                TryAssignPendingClients();
                return;
            }

            scenePlayerObject.RemoveOwnership();

            if (_logSlotEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Released slot {slotIndex} from client {clientId}; ownership removed from {scenePlayerObject.name}.");
            }

            TryAssignPendingClients();
        }

        private void TryForceDropCarriedItem(int slotIndex, int clientId)
        {
            PlayerNetworkBridge bridge = GetPlayerBridge(slotIndex);
            if (bridge == null || !bridge.TryGetComponent(out PlayerCarrySocket carrySocket))
            {
                return;
            }

            if (!carrySocket.TryForceDropWithoutImpulse())
            {
                return;
            }

            if (_logSlotEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Forced scrap drop on disconnect for client {clientId} at slot {slotIndex}.");
            }
        }

        private int GetFirstAvailableSlot()
        {
            int slotCount = Mathf.Min(
                _playerSlots == null ? 0 : _playerSlots.Length,
                _scenePlayerObjects == null ? 0 : _scenePlayerObjects.Length);

            if (slotCount <= 0)
            {
                return -1;
            }

            bool[] usedSlots = new bool[slotCount];
            foreach (int slotIndex in _clientToSlot.Values)
            {
                if (slotIndex >= 0 && slotIndex < usedSlots.Length)
                {
                    usedSlots[slotIndex] = true;
                }
            }

            for (int i = 0; i < usedSlots.Length; i++)
            {
                if (!usedSlots[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnqueuePendingClient(int clientId)
        {
            if (clientId < 0 || _clientToSlot.ContainsKey(clientId))
            {
                return;
            }

            if (!_pendingClientSet.Add(clientId))
            {
                return;
            }

            _pendingClientQueue.Enqueue(clientId);
            if (_logSlotEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Queued pending client {clientId} for next available slot.");
            }
        }

        private void RemovePendingClient(int clientId)
        {
            if (clientId < 0)
            {
                return;
            }

            _pendingClientSet.Remove(clientId);
        }

        private void TryAssignPendingClients()
        {
            if (_networkManager == null || _networkManager.ServerManager == null || _pendingClientQueue.Count == 0)
            {
                return;
            }

            int remaining = _pendingClientQueue.Count;
            while (remaining-- > 0 && _pendingClientQueue.Count > 0)
            {
                int pendingClientId = _pendingClientQueue.Dequeue();
                if (!_pendingClientSet.Contains(pendingClientId))
                {
                    continue;
                }

                if (_clientToSlot.ContainsKey(pendingClientId))
                {
                    _pendingClientSet.Remove(pendingClientId);
                    continue;
                }

                if (!_networkManager.ServerManager.Clients.TryGetValue(pendingClientId, out NetworkConnection pendingConnection))
                {
                    _pendingClientSet.Remove(pendingClientId);
                    continue;
                }

                int slotIndex = GetFirstAvailableSlot();
                if (slotIndex < 0)
                {
                    _pendingClientQueue.Enqueue(pendingClientId);
                    return;
                }

                _pendingClientSet.Remove(pendingClientId);
                AssignSlot(pendingConnection);
            }
        }

        private PlayerNetworkBridge GetPlayerBridge(int slotIndex)
        {
            if (_playerSlots == null || slotIndex < 0 || slotIndex >= _playerSlots.Length)
            {
                return null;
            }

            return _playerSlots[slotIndex];
        }

        private NetworkObject GetPlayerObject(int slotIndex)
        {
            if (_scenePlayerObjects == null || slotIndex < 0 || slotIndex >= _scenePlayerObjects.Length)
            {
                return null;
            }

            return _scenePlayerObjects[slotIndex];
        }

        private void ResolveDependenciesIfMissing()
        {
            if (_networkManager == null)
            {
                _networkManager = FindObjectOfType<NetworkManager>();
            }

            if (_playerSlots == null || _playerSlots.Length == 0)
            {
                _playerSlots = new PlayerNetworkBridge[2];
                _playerSlots[0] = FindPlayerBridge("PlayerA");
                _playerSlots[1] = FindPlayerBridge("PlayerB");
            }

            if (_scenePlayerObjects == null || _scenePlayerObjects.Length == 0)
            {
                _scenePlayerObjects = new NetworkObject[2];
                _scenePlayerObjects[0] = FindPlayerObject("PlayerA");
                _scenePlayerObjects[1] = FindPlayerObject("PlayerB");
            }
        }

        private static PlayerNetworkBridge FindPlayerBridge(string playerName)
        {
            GameObject player = GameObject.Find(playerName);
            return player == null ? null : player.GetComponent<PlayerNetworkBridge>();
        }

        private static NetworkObject FindPlayerObject(string playerName)
        {
            GameObject player = GameObject.Find(playerName);
            return player == null ? null : player.GetComponent<NetworkObject>();
        }

        private void TrySeedInteractionRegression(int slotIndex, PlayerNetworkBridge bridge)
        {
            if (!_seedRemoteSlotForInteractionRegression || slotIndex != _regressionSeedSlotIndex || bridge == null)
            {
                return;
            }

            if (!ShouldEnableRegressionSeedForCurrentRuntime())
            {
                return;
            }

            if (!bridge.TryGetComponent(out PlayerCarrySocket carrySocket))
            {
                if (_logRegressionSeedEvents)
                {
                    Debug.LogWarning($"[FishNetScenePlayerAssigner] Regression seed skipped. {bridge.name} is missing PlayerCarrySocket.");
                }

                return;
            }

            GameObject scrapObject = GameObject.Find(_regressionSeedScrapName);
            GameObject stationObject = GameObject.Find(_regressionSeedRepairStationName);
            if (scrapObject == null || stationObject == null)
            {
                if (_logRegressionSeedEvents)
                {
                    Debug.LogWarning($"[FishNetScenePlayerAssigner] Regression seed skipped. Missing scrap='{_regressionSeedScrapName}' or station='{_regressionSeedRepairStationName}'.");
                }

                return;
            }

            if (!scrapObject.TryGetComponent(out ScrapItem scrapItem))
            {
                if (_logRegressionSeedEvents)
                {
                    Debug.LogWarning($"[FishNetScenePlayerAssigner] Regression seed skipped. '{_regressionSeedScrapName}' has no ScrapItem.");
                }

                return;
            }

            if (scrapItem.Carrier != null)
            {
                scrapItem.Carrier.TryForceDropWithoutImpulse();
            }

            if (carrySocket.HasItem)
            {
                carrySocket.TryForceDropWithoutImpulse();
            }

            Transform playerTransform = bridge.transform;
            Vector3 playerForward = playerTransform.forward.sqrMagnitude <= 0.0001f ? Vector3.forward : playerTransform.forward.normalized;
            Vector3 scrapPosition = playerTransform.position + (playerForward * 1.0f);
            scrapPosition.y = playerTransform.position.y;
            scrapItem.SetWorldStateAuthoritative(scrapPosition, simulatePhysics: false);

            if (!carrySocket.TryPickup(scrapItem))
            {
                if (_logRegressionSeedEvents)
                {
                    Debug.LogWarning($"[FishNetScenePlayerAssigner] Regression seed failed. Could not assign '{_regressionSeedScrapName}' to {bridge.name}.");
                }

                return;
            }

            Transform stationTransform = stationObject.transform;
            Vector3 stationPosition = playerTransform.position + (playerForward * _regressionSeedStationDistance);
            stationPosition.y = playerTransform.position.y;
            stationTransform.position = stationPosition;
            stationTransform.rotation = Quaternion.LookRotation(-playerForward, Vector3.up);

            if (stationObject.TryGetComponent(out RepairStationObjective repairObjective))
            {
                repairObjective.ResetObjective();
            }

            TetherNetworkStateReplicator tetherReplicator = FindObjectOfType<TetherNetworkStateReplicator>();
            if (tetherReplicator != null)
            {
                tetherReplicator.LogRegressionSnapshot();
            }

            if (_logRegressionSeedEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Regression seed ready for slot {slotIndex}. player={bridge.name}, scrap={scrapObject.name}, station={stationObject.name}.");
            }
        }

        private void ApplyRuntimeOverrides()
        {
            if (!_allowRuntimeOverride)
            {
                return;
            }

            string runtimeValue = ReadRuntimeOverride(_enableRegressionSeedArgument, "INTERSTELLA_ENABLE_REGRESSION_SEED");
            if (string.IsNullOrWhiteSpace(runtimeValue))
            {
                return;
            }

            string normalized = runtimeValue.Trim().ToLowerInvariant();
            _enableRegressionSeedInPlayerBuilds = normalized == "1"
                || normalized == "true"
                || normalized == "yes"
                || normalized == "on";
        }

        private bool ShouldEnableRegressionSeedForCurrentRuntime()
        {
            if (Application.isEditor)
            {
                return true;
            }

            return _enableRegressionSeedInPlayerBuilds;
        }

        private static string ReadRuntimeOverride(string argumentName, string environmentVariableName)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            if (string.IsNullOrWhiteSpace(argumentName))
            {
                return string.Empty;
            }

            string argumentToken = "-" + argumentName;
            string equalsToken = argumentToken + "=";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith(equalsToken, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(equalsToken.Length);
                }

                if (string.Equals(arg, argumentToken, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }
    }
}
