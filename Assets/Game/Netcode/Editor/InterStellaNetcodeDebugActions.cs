using InterStella.Game.Features.Scavenge;
using InterStella.Game.Features.Stations;
using InterStella.Game.Features.Player;
using InterStella.Game.Netcode.Runtime;
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

        [MenuItem("Tools/InterStella/Debug/Camera/Set First Person")]
        private static void SetCameraFirstPerson()
        {
            if (!TryGetCameraController(out PlayerCameraModeController cameraController))
            {
                return;
            }

            cameraController.SetFirstPersonMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Failed to snap camera after FirstPerson mode set.");
                return;
            }

            LogCameraSnapshot("FirstPerson", cameraController);
        }

        [MenuItem("Tools/InterStella/Debug/Camera/Set Overview")]
        private static void SetCameraOverview()
        {
            if (!TryGetCameraController(out PlayerCameraModeController cameraController))
            {
                return;
            }

            cameraController.SetOverviewMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Failed to snap camera after Overview mode set.");
                return;
            }

            LogCameraSnapshot("Overview", cameraController);
        }

        [MenuItem("Tools/InterStella/Debug/Camera/Set Third Person")]
        private static void SetCameraThirdPerson()
        {
            if (!TryGetCameraController(out PlayerCameraModeController cameraController))
            {
                return;
            }

            cameraController.SetThirdPersonMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Failed to snap camera after ThirdPerson mode set.");
                return;
            }

            LogCameraSnapshot("ThirdPerson", cameraController);
        }

        [MenuItem("Tools/InterStella/Debug/Camera/Run Mode Smoke (1-2-3)")]
        private static void RunCameraModeSmoke()
        {
            if (!TryGetCameraController(out PlayerCameraModeController cameraController))
            {
                return;
            }

            GameObject playerA = GameObject.Find("PlayerA");
            if (playerA == null)
            {
                Debug.LogWarning("[InterStella][Debug] Camera smoke failed: PlayerA was not found.");
                return;
            }

            Transform playerATransform = playerA.transform;
            Transform playerBTransform = GameObject.Find("PlayerB")?.transform;

            cameraController.SetFirstPersonMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Camera smoke failed: FirstPerson snap failed.");
                return;
            }

            float firstPersonDistance = Vector3.Distance(cameraController.transform.position, playerATransform.position);

            cameraController.SetThirdPersonMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Camera smoke failed: ThirdPerson snap failed.");
                return;
            }

            float thirdPersonDistance = Vector3.Distance(cameraController.transform.position, playerATransform.position);

            cameraController.SetOverviewMode();
            if (!cameraController.ForceSnapToCurrentMode())
            {
                Debug.LogWarning("[InterStella][Debug] Camera smoke failed: Overview snap failed.");
                return;
            }

            Vector3 focusPoint = playerATransform.position;
            if (playerBTransform != null)
            {
                focusPoint = (focusPoint + playerBTransform.position) * 0.5f;
            }

            float overviewHeight = cameraController.transform.position.y - focusPoint.y;
            bool pass = firstPersonDistance <= 1.5f &&
                        thirdPersonDistance > firstPersonDistance + 0.8f &&
                        overviewHeight >= 8f;

            string status = pass ? "PASS" : "FAIL";
            Debug.Log($"[InterStella][CameraSmoke] {status} firstDistance={firstPersonDistance:F2} thirdDistance={thirdPersonDistance:F2} overviewHeight={overviewHeight:F2} mode={cameraController.CurrentModeName}");
        }

        [MenuItem("Tools/InterStella/Debug/Steam/Log Session Snapshot")]
        private static void LogSteamSessionSnapshot()
        {
            if (!TryGetSteamSession(out SteamSessionService steamSession))
            {
                return;
            }

            SteamworksBootstrap bootstrap = Object.FindObjectOfType<SteamworksBootstrap>();
            string bootstrapState = bootstrap == null
                ? "missing"
                : $"initialized={bootstrap.IsInitialized}, localSteamId={bootstrap.LocalSteamIdString}, lastError={bootstrap.LastInitError}";

            Debug.Log(
                "[InterStella][SteamDebug] " +
                $"state={steamSession.StateName}, " +
                $"sessionActive={steamSession.IsSessionActive}, " +
                $"isHost={steamSession.IsHost}, " +
                $"lobbyId={steamSession.ActiveLobbyId}, " +
                $"hostSteamId={steamSession.ActiveHostSteamId}, " +
                $"autoInviteFriend={steamSession.AutoInviteFriendSteamId}, " +
                $"bootstrap=({bootstrapState})");
        }

        [MenuItem("Tools/InterStella/Debug/Steam/Copy Join Launch Args")]
        private static void CopySteamJoinLaunchArguments()
        {
            if (!TryGetSteamSession(out SteamSessionService steamSession))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(steamSession.ActiveLobbyId))
            {
                Debug.LogWarning("[InterStella][SteamDebug] Cannot copy join args because there is no active Steam lobby.");
                return;
            }

            string launchArgs = $"-interstella-provider steam +connect_lobby {steamSession.ActiveLobbyId}";
            EditorGUIUtility.systemCopyBuffer = launchArgs;
            Debug.Log("[InterStella][SteamDebug] Copied join launch args to clipboard: " + launchArgs);
        }

        [MenuItem("Tools/InterStella/Debug/Steam/Invite Configured Friend")]
        private static void InviteConfiguredSteamFriend()
        {
            if (!TryGetSteamSession(out SteamSessionService steamSession))
            {
                return;
            }

            string targetSteamId = steamSession.AutoInviteFriendSteamId;
            if (string.IsNullOrWhiteSpace(targetSteamId))
            {
                Debug.LogWarning("[InterStella][SteamDebug] SteamSessionService._autoInviteFriendSteamId is empty.");
                return;
            }

            bool invited = steamSession.TryInviteUserToActiveLobby(targetSteamId, out string details);
            if (invited)
            {
                Debug.Log("[InterStella][SteamDebug] Invite configured friend succeeded. " + details);
                return;
            }

            Debug.LogWarning("[InterStella][SteamDebug] Invite configured friend failed. " + details);
        }

        private static bool TryGetCameraController(out PlayerCameraModeController cameraController)
        {
            cameraController = null;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][Debug] Play mode is required for camera debug actions.");
                return false;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[InterStella][Debug] Main Camera was not found.");
                return false;
            }

            if (!mainCamera.TryGetComponent(out cameraController))
            {
                Debug.LogWarning("[InterStella][Debug] Main Camera is missing PlayerCameraModeController.");
                return false;
            }

            return true;
        }

        private static bool TryGetSteamSession(out SteamSessionService steamSession)
        {
            steamSession = null;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InterStella][SteamDebug] Play mode is required for Steam debug actions.");
                return false;
            }

            steamSession = Object.FindObjectOfType<SteamSessionService>();
            if (steamSession == null)
            {
                Debug.LogWarning("[InterStella][SteamDebug] SteamSessionService was not found in the active scene.");
                return false;
            }

            return true;
        }

        private static void LogCameraSnapshot(string expectedMode, PlayerCameraModeController cameraController)
        {
            Vector3 position = cameraController.transform.position;
            Vector3 euler = cameraController.transform.eulerAngles;
            Debug.Log($"[InterStella][CameraMode] forced={cameraController.CurrentModeName} expected={expectedMode} position=({position.x:F2},{position.y:F2},{position.z:F2}) rotation=({euler.x:F1},{euler.y:F1},{euler.z:F1})");
        }
    }
}
