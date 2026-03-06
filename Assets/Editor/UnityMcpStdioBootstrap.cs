#if UNITY_EDITOR
using System;
using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;
using UnityEngine;

namespace InterStella.EditorTools
{
    [InitializeOnLoad]
    public static class UnityMcpStdioBootstrap
    {
        private const string USE_HTTP_TRANSPORT_KEY = "MCPForUnity.UseHttpTransport";
        private const string BOOTSTRAP_DONE_KEY = "InterStella.UnityMcpStdioBootstrap.Done";

        static UnityMcpStdioBootstrap()
        {
            EditorApplication.delayCall += TryBootstrap;
        }

        [MenuItem("Tools/interStella/Unity MCP/Force Start Stdio Bridge")]
        private static void ForceStartStdioBridgeMenu()
        {
            ForceStartBridgeCore();
        }

        private static void TryBootstrap()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryBootstrap;
                return;
            }

            if (EditorPrefs.GetBool(BOOTSTRAP_DONE_KEY, false))
            {
                return;
            }

            ForceStartBridgeCore();

            EditorPrefs.SetBool(BOOTSTRAP_DONE_KEY, true);
        }

        public static void RunBatchBootstrap()
        {
            ForceStartBridgeCore();
            EditorPrefs.SetBool(BOOTSTRAP_DONE_KEY, true);
        }

        private static void ForceStartBridgeCore()
        {
            // Force stdio transport so Codex can connect via command-based MCP.
            EditorPrefs.SetBool(USE_HTTP_TRANSPORT_KEY, false);

            try
            {
                StdioBridgeHost.StartAutoConnect();
                Debug.Log("[interStella] Unity MCP stdio bridge started.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[interStella] Unity MCP stdio bridge start failed: {exception.Message}");
            }
        }
    }
}
#endif
