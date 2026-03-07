using InterStella.Game.Features.Player;
using InterStella.Game.Features.Repair;
using InterStella.Game.Netcode.Runtime;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Stations
{
    public sealed class StationMatchController : MonoBehaviour
    {
        [SerializeField]
        private MonoBehaviour _sessionServiceBehaviour;

        [SerializeField]
        private RepairStationObjective _repairObjective;

        [SerializeField]
        private PlayerFuel[] _trackedFuels;

        [SerializeField]
        private float _matchDurationSeconds = 300f;

        [SerializeField]
        private bool _autoStartOnPlay = true;

        [SerializeField]
        private bool _failWhenAllFuelDepleted = true;

        private ISessionService _sessionService;
        private float _remainingSeconds;
        private MatchPhase _phase = MatchPhase.Lobby;

        public MatchPhase CurrentPhase => _phase;
        public float RemainingSeconds => _remainingSeconds;

        private void Awake()
        {
            _sessionService = ResolveSessionService();
            _remainingSeconds = Mathf.Max(0f, _matchDurationSeconds);
        }

        private void Start()
        {
            if (_autoStartOnPlay)
            {
                StartMatch();
            }
        }

        private void Update()
        {
            if (_phase != MatchPhase.InProgress)
            {
                return;
            }

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f)
            {
                FailMatch();
                return;
            }

            if (_repairObjective != null && _repairObjective.CurrentState.IsCompleted)
            {
                CompleteMatch();
                return;
            }

            if (_failWhenAllFuelDepleted && AreAllPlayersOutOfFuel())
            {
                FailMatch();
            }
        }

        public void StartMatch()
        {
            _repairObjective?.ResetObjective();
            ResetTrackedFuelToStartup();
            _sessionService?.StartSession();
            _remainingSeconds = Mathf.Max(0f, _matchDurationSeconds);
            _phase = MatchPhase.InProgress;
        }

        public void ResetMatch()
        {
            _repairObjective?.ResetObjective();
            ResetTrackedFuelToStartup();
            _remainingSeconds = Mathf.Max(0f, _matchDurationSeconds);
            _phase = MatchPhase.Lobby;
        }

        private void CompleteMatch()
        {
            _phase = MatchPhase.Success;
            _sessionService?.StopSession();
        }

        private void FailMatch()
        {
            _phase = MatchPhase.Failed;
            _sessionService?.StopSession();
        }

        private bool AreAllPlayersOutOfFuel()
        {
            if (_trackedFuels == null || _trackedFuels.Length == 0)
            {
                return false;
            }

            int validCount = 0;
            for (int i = 0; i < _trackedFuels.Length; i++)
            {
                PlayerFuel fuel = _trackedFuels[i];
                if (fuel == null)
                {
                    continue;
                }

                validCount++;
                if (!fuel.IsDepleted)
                {
                    return false;
                }
            }

            return validCount > 0;
        }

        private void ResetTrackedFuelToStartup()
        {
            if (_trackedFuels == null || _trackedFuels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _trackedFuels.Length; i++)
            {
                PlayerFuel fuel = _trackedFuels[i];
                if (fuel != null)
                {
                    fuel.ResetToStartupFuel();
                }
            }
        }

        private ISessionService ResolveSessionService()
        {
            ISessionService configured = _sessionServiceBehaviour as ISessionService;
            if (configured is SteamSessionService)
            {
                return configured;
            }

            SteamSessionService discoveredSteam = FindObjectOfType<SteamSessionService>();
            if (discoveredSteam != null)
            {
                _sessionServiceBehaviour = discoveredSteam;
                Debug.Log("[StationMatchController] Session service switched to discovered SteamSessionService.");
                return discoveredSteam;
            }

            if (configured != null)
            {
                return configured;
            }

            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is ISessionService discoveredSession)
                {
                    _sessionServiceBehaviour = behaviour;
                    Debug.Log($"[StationMatchController] Session service auto-resolved to {behaviour.GetType().Name}.");
                    return discoveredSession;
                }
            }

            Debug.LogWarning("[StationMatchController] No ISessionService was found. Match will run without network session control.");
            return null;
        }
    }
}
