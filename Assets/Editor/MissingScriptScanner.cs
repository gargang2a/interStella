using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptScanner
{
    [MenuItem("Tools/interStella/Diagnostics/Scan Missing Scripts (Active Scene)")]
    private static void ScanActiveSceneMissingScripts()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogWarning("[MissingScriptScanner] Active scene is not loaded.");
            return;
        }

        var roots = activeScene.GetRootGameObjects();
        var stack = new Stack<Transform>(256);
        int totalMissing = 0;

        foreach (GameObject root in roots)
        {
            stack.Push(root.transform);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(current.gameObject);
                if (missing > 0)
                {
                    totalMissing += missing;
                    Debug.Log($"[MissingScriptScanner] Missing({missing}) at {GetPath(current)}", current.gameObject);
                }

                for (int i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }
        }

        string scenePath = activeScene.path;
        if (totalMissing == 0)
        {
            Debug.Log($"[MissingScriptScanner] No missing scripts found in scene '{scenePath}'.");
        }
        else
        {
            Debug.LogWarning($"[MissingScriptScanner] Total missing scripts: {totalMissing} in scene '{scenePath}'.");
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }
}
