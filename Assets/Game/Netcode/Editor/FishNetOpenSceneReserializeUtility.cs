#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InterStella.Game.Netcode.Editor
{
    public static class FishNetOpenSceneReserializeUtility
    {
        [MenuItem("Tools/InterStella/Netcode/Reserialize Open Scene NetworkObjects", false, 5000)]
        public static void ReserializeOpenSceneNetworkObjects()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[InterStella][Netcode] Cannot reserialize NetworkObjects while in play mode.");
                return;
            }

            Type editorType = Type.GetType("FishNet.Editing.ReserializeNetworkObjectsEditor, FishNet.Runtime");
            if (editorType == null)
            {
                Debug.LogError("[InterStella][Netcode] FishNet reserialize editor type was not found.");
                return;
            }

            const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo openWindowMethod = editorType.GetMethod("ReserializeNetworkObjects", staticFlags);
            FieldInfo windowField = editorType.GetField("_window", staticFlags);
            FieldInfo iteratePrefabsField = editorType.GetField("_iteratePrefabs", instanceFlags);
            FieldInfo iterateScenesField = editorType.GetField("_iterateScenes", instanceFlags);
            FieldInfo sceneReserializeTypeField = editorType.GetField("_sceneReserializeType", instanceFlags);
            FieldInfo isRunningField = editorType.GetField("IsRunning", staticFlags);
            MethodInfo saveLastValuesMethod = editorType.GetMethod("SaveLastValues", instanceFlags);
            MethodInfo reserializePrefabsMethod = editorType.GetMethod("ReserializeProjectPrefabs", instanceFlags);
            MethodInfo reserializeScenesMethod = editorType.GetMethod("ReserializeScenes", instanceFlags, null, Type.EmptyTypes, null);

            if (openWindowMethod == null
                || windowField == null
                || iteratePrefabsField == null
                || iterateScenesField == null
                || sceneReserializeTypeField == null
                || isRunningField == null
                || saveLastValuesMethod == null
                || reserializePrefabsMethod == null
                || reserializeScenesMethod == null)
            {
                Debug.LogError("[InterStella][Netcode] FishNet reserialize members could not be resolved.");
                return;
            }

            try
            {
                openWindowMethod.Invoke(null, null);
                object window = windowField.GetValue(null);
                if (window == null)
                {
                    Debug.LogError("[InterStella][Netcode] FishNet reserialize window was not created.");
                    return;
                }

                iteratePrefabsField.SetValue(window, true);
                iterateScenesField.SetValue(window, true);

                object openScenesEnumValue = Enum.ToObject(sceneReserializeTypeField.FieldType, 1);
                sceneReserializeTypeField.SetValue(window, openScenesEnumValue);

                isRunningField.SetValue(null, true);
                saveLastValuesMethod.Invoke(window, null);
                reserializePrefabsMethod.Invoke(window, null);
                reserializeScenesMethod.Invoke(window, null);
                isRunningField.SetValue(null, false);

                if (window is EditorWindow editorWindow)
                {
                    editorWindow.Close();
                }

                Debug.Log("[InterStella][Netcode] FishNet reserialize task completed for open scenes and prefabs.");
            }
            catch (Exception ex)
            {
                isRunningField.SetValue(null, false);
                Debug.LogError($"[InterStella][Netcode] FishNet reserialize task failed: {ex}");
            }
        }
    }
}
#endif
