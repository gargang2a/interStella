#if UNITY_EDITOR
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using InterStella.Game.Netcode.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InterStella.Game.Netcode.Editor
{
    public static class SteamTransportSetupMenu
    {
        private const string MATCH_SYSTEMS_NAME = "MatchSystems";
        private const uint DEFAULT_STEAM_APP_ID = 480;

        [MenuItem("Tools/InterStella/Netcode/Setup Steam Transport Components")]
        public static void SetupSteamTransportComponents()
        {
            GameObject target = ResolveTarget();
            if (target == null)
            {
                Debug.LogError("[InterStella][SteamTransportSetup] Could not find MatchSystems or a NetworkManager root in the active scene.");
                return;
            }

            NetworkManager networkManager = GetOrAddComponent<NetworkManager>(target);
            FishNetSessionService fishNetSession = GetOrAddComponent<FishNetSessionService>(target);
            SteamSessionService steamSession = GetOrAddComponent<SteamSessionService>(target);
            SteamworksLobbyService steamLobbyService = GetOrAddComponent<SteamworksLobbyService>(target);
            SteamRelayLoopbackTransportBinder loopbackBinder = GetOrAddComponent<SteamRelayLoopbackTransportBinder>(target);
            SteamRelaySdkTransportBinder sdkBinder = GetOrAddComponent<SteamRelaySdkTransportBinder>(target);
            SteamworksBootstrap steamworksBootstrap = GetOrAddComponent<SteamworksBootstrap>(target);
            TransportManager transportManager = GetOrAddComponent<TransportManager>(target);
            Tugboat tugboatTransport = GetOrAddComponent<Tugboat>(target);
            Multipass multipassTransport = GetOrAddComponent<Multipass>(target);
            FishySteamworks.FishySteamworks fishySteamworksTransport = GetOrAddComponent<FishySteamworks.FishySteamworks>(target);

            transportManager.Transport = tugboatTransport;
            multipassTransport.GlobalServerActions = true;

            ConfigureMultipass(multipassTransport, tugboatTransport, fishySteamworksTransport);
            ConfigureFishySteamworks(fishySteamworksTransport);
            ConfigureSteamworksBootstrap(steamworksBootstrap);

            SetObjectReference(fishNetSession, "_networkManager", networkManager);
            SetObjectReference(fishNetSession, "_steamRelayTransportBinderBehaviour", sdkBinder);

            SetObjectReference(steamSession, "_networkSessionBehaviour", fishNetSession);
            SetObjectReference(steamSession, "_steamLobbyServiceBehaviour", steamLobbyService);
            SetObjectReference(steamSession, "_steamworksBootstrap", steamworksBootstrap);

            SetObjectReference(steamLobbyService, "_steamworksBootstrap", steamworksBootstrap);

            SetObjectReference(sdkBinder, "_loopbackFallback", loopbackBinder);
            SetObjectReference(sdkBinder, "_steamworksBootstrap", steamworksBootstrap);
            SetObjectReference(sdkBinder, "_fishNetSessionService", fishNetSession);
            SetObjectReference(sdkBinder, "_transportManager", transportManager);
            SetObjectReference(sdkBinder, "_tugboatTransport", tugboatTransport);
            SetObjectReference(sdkBinder, "_multipassTransport", multipassTransport);
            SetObjectReference(sdkBinder, "_fishySteamworksTransport", fishySteamworksTransport);

            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(networkManager);
            EditorUtility.SetDirty(fishNetSession);
            EditorUtility.SetDirty(steamSession);
            EditorUtility.SetDirty(steamLobbyService);
            EditorUtility.SetDirty(loopbackBinder);
            EditorUtility.SetDirty(sdkBinder);
            EditorUtility.SetDirty(steamworksBootstrap);
            EditorUtility.SetDirty(transportManager);
            EditorUtility.SetDirty(tugboatTransport);
            EditorUtility.SetDirty(multipassTransport);
            EditorUtility.SetDirty(fishySteamworksTransport);
            EditorSceneManager.MarkSceneDirty(target.scene);

            Debug.Log($"[InterStella][SteamTransportSetup] Configured Steam transport components on '{target.name}'.");
        }

        private static GameObject ResolveTarget()
        {
            GameObject byName = GameObject.Find(MATCH_SYSTEMS_NAME);
            if (byName != null)
            {
                return byName;
            }

            NetworkManager networkManager = Object.FindObjectOfType<NetworkManager>();
            return networkManager != null ? networkManager.gameObject : null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return Undo.AddComponent<T>(target);
        }

        private static void ConfigureMultipass(Multipass multipassTransport, Tugboat tugboatTransport, FishySteamworks.FishySteamworks fishySteamworksTransport)
        {
            SerializedObject serializedObject = new SerializedObject(multipassTransport);
            SerializedProperty transportsProperty = serializedObject.FindProperty("_transports");
            if (transportsProperty != null)
            {
                transportsProperty.arraySize = 2;
                transportsProperty.GetArrayElementAtIndex(0).objectReferenceValue = tugboatTransport;
                transportsProperty.GetArrayElementAtIndex(1).objectReferenceValue = fishySteamworksTransport;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureFishySteamworks(FishySteamworks.FishySteamworks fishySteamworksTransport)
        {
            SerializedObject serializedObject = new SerializedObject(fishySteamworksTransport);
            SerializedProperty peerToPeerProperty = serializedObject.FindProperty("_peerToPeer");
            SerializedProperty portProperty = serializedObject.FindProperty("_port");
            if (peerToPeerProperty != null)
            {
                peerToPeerProperty.boolValue = true;
            }

            if (portProperty != null)
            {
                portProperty.intValue = 7770;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSteamworksBootstrap(SteamworksBootstrap steamworksBootstrap)
        {
            SerializedObject serializedObject = new SerializedObject(steamworksBootstrap);
            SerializedProperty appIdProperty = serializedObject.FindProperty("_appId");
            SerializedProperty initializeOnAwakeProperty = serializedObject.FindProperty("_initializeOnAwake");
            if (appIdProperty != null)
            {
                appIdProperty.longValue = DEFAULT_STEAM_APP_ID;
            }

            if (initializeOnAwakeProperty != null)
            {
                initializeOnAwakeProperty.boolValue = true;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[InterStella][SteamTransportSetup] Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
