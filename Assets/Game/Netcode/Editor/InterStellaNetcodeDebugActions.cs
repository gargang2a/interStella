using InterStella.Game.Features.Scavenge;
using InterStella.Game.Features.Stations;
using UnityEditor;
using UnityEngine;

namespace InterStella.EditorTools
{
    public static class InterStellaNetcodeDebugActions
    {
        [MenuItem("Tools/InterStella/Debug/Force Start Match")]
        private static void ForceStartMatch()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][Debug] Play mode is required for this action.");
                return;
            }

            StationMatchController matchController = Object.FindObjectOfType<StationMatchController>();
            if (matchController == null)
            {
                Debug.LogWarning("[InterStella][Debug] StationMatchController was not found in the active scene.");
                return;
            }

            matchController.StartMatch();
            Debug.Log("[InterStella][Debug] Forced match start.");
        }

        [MenuItem("Tools/InterStella/Debug/Force PlayerB Carry Scrap_01")]
        private static void ForcePlayerBCarryScrap01()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][Debug] Play mode is required for this action.");
                return;
            }

            GameObject playerObject = GameObject.Find("PlayerB");
            GameObject scrapObject = GameObject.Find("Scrap_01");
            if (playerObject == null || scrapObject == null)
            {
                Debug.LogWarning("[InterStella][Debug] Missing PlayerB or Scrap_01 in the active scene.");
                return;
            }

            if (!playerObject.TryGetComponent(out PlayerCarrySocket carrySocket))
            {
                Debug.LogWarning("[InterStella][Debug] PlayerB is missing PlayerCarrySocket.");
                return;
            }

            if (!scrapObject.TryGetComponent(out ScrapItem scrapItem))
            {
                Debug.LogWarning("[InterStella][Debug] Scrap_01 is missing ScrapItem.");
                return;
            }

            if (!carrySocket.TryPickup(scrapItem))
            {
                Debug.LogWarning("[InterStella][Debug] Failed to force pickup. PlayerB may already carry an item or Scrap_01 is unavailable.");
                return;
            }

            Debug.Log("[InterStella][Debug] Forced PlayerB to carry Scrap_01.");
        }

        [MenuItem("Tools/InterStella/Debug/Place Scrap_03 In Front Of PlayerB")]
        private static void PlaceScrap03InFrontOfPlayerB()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][Debug] Play mode is required for this action.");
                return;
            }

            GameObject playerObject = GameObject.Find("PlayerB");
            GameObject scrapObject = GameObject.Find("Scrap_03");
            if (playerObject == null || scrapObject == null)
            {
                Debug.LogWarning("[InterStella][Debug] Missing PlayerB or Scrap_03 in the active scene.");
                return;
            }

            if (!scrapObject.TryGetComponent(out ScrapItem scrapItem))
            {
                Debug.LogWarning("[InterStella][Debug] Scrap_03 is missing ScrapItem.");
                return;
            }

            if (scrapItem.Carrier != null)
            {
                scrapItem.Carrier.TryForceDropWithoutImpulse();
            }

            Transform playerTransform = playerObject.transform;
            Vector3 targetPosition = playerTransform.position + (playerTransform.forward * 1.2f);
            targetPosition.y = playerTransform.position.y;

            scrapItem.SetWorldStateAuthoritative(targetPosition, simulatePhysics: false);
            Debug.Log("[InterStella][Debug] Placed Scrap_03 in front of PlayerB.");
        }

        [MenuItem("Tools/InterStella/Debug/Place RepairStation In Front Of PlayerB")]
        private static void PlaceRepairStationInFrontOfPlayerB()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][Debug] Play mode is required for this action.");
                return;
            }

            GameObject playerObject = GameObject.Find("PlayerB");
            GameObject stationObject = GameObject.Find("RepairStation");
            if (playerObject == null || stationObject == null)
            {
                Debug.LogWarning("[InterStella][Debug] Missing PlayerB or RepairStation in the active scene.");
                return;
            }

            Transform playerTransform = playerObject.transform;
            Transform stationTransform = stationObject.transform;

            Vector3 stationPosition = playerTransform.position + (playerTransform.forward * 1.8f);
            stationPosition.y = playerTransform.position.y;

            stationTransform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            stationTransform.position = stationPosition;
            stationTransform.rotation = Quaternion.LookRotation(-playerTransform.forward, Vector3.up);
            Debug.Log("[InterStella][Debug] Placed RepairStation in front of PlayerB (test scale applied).");
        }
    }
}
