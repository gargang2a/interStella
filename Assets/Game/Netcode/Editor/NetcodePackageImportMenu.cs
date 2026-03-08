#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InterStella.Game.Netcode.Editor
{
    public static class NetcodePackageImportMenu
    {
        private const string FISHY_STEAMWORKS_PACKAGE_NAME = "FishySteamworks.4.1.1.unitypackage";

        [MenuItem("Tools/InterStella/Netcode/Import FishySteamworks Package")]
        public static void ImportFishySteamworksPackage()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string packagePath = Path.Combine(projectRoot, ".tmp", FISHY_STEAMWORKS_PACKAGE_NAME);
            if (!File.Exists(packagePath))
            {
                Debug.LogError($"[InterStella][NetcodePackageImport] Package not found at '{packagePath}'.");
                return;
            }

            AssetDatabase.ImportPackage(packagePath, false);
            Debug.Log($"[InterStella][NetcodePackageImport] Imported '{packagePath}'.");
        }
    }
}
#endif
