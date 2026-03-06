using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using InterStella.Game.Features.Player;
using InterStella.Game.Features.Scavenge;
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

        private readonly Dictionary<int, int> _clientToSlot = new Dictionary<int, int>(4);
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveDependenciesIfMissing();
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
                ReleaseSlot(connection.ClientId);
            }
        }

        private void AssignSlot(NetworkConnection connection)
        {
            int clientId = connection.ClientId;
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
                Debug.LogWarning($"[FishNetScenePlayerAssigner] No available slot for client {clientId}.");
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
                return;
            }

            scenePlayerObject.RemoveOwnership();

            if (_logSlotEvents)
            {
                Debug.Log($"[FishNetScenePlayerAssigner] Released slot {slotIndex} from client {clientId}; ownership removed from {scenePlayerObject.name}.");
            }
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
    }
}
