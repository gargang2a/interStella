using InterStella.Game.Features.Scavenge;
using InterStella.Game.Shared.Interaction;
using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Repair
{
    public sealed class RepairStationObjective : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private int _requiredScrapCount = 5;

        [SerializeField]
        private bool _lockAfterComplete = true;

        [SerializeField]
        private bool _logDeliveries = true;

        private int _deliveredCount;

        public RepairObjectiveState CurrentState => new RepairObjectiveState(_requiredScrapCount, _deliveredCount, _lockAfterComplete);
        public int DeliveredCount => _deliveredCount;
        public int RequiredScrapCount => _requiredScrapCount;

        public bool TryInteract(in InteractionContext context)
        {
            if (context.Interactor == null)
            {
                return false;
            }

            RepairObjectiveState state = CurrentState;
            if (state.IsCompleted && _lockAfterComplete)
            {
                return false;
            }

            if (!context.Interactor.TryGetComponent(out PlayerCarrySocket carrySocket))
            {
                return false;
            }

            if (!carrySocket.TryConsumeForDelivery(out ScrapItem scrapItem) || scrapItem == null)
            {
                return false;
            }

            int deliveredValue = scrapItem.Definition == null ? 1 : scrapItem.Definition.DeliveryValue;
            _deliveredCount += Mathf.Max(1, deliveredValue);
            scrapItem.MarkDelivered();
            if (_logDeliveries)
            {
                Debug.Log($"[RepairStationObjective] Delivery accepted. delivered={_deliveredCount}/{_requiredScrapCount}, station={name}");
            }

            return true;
        }

        public void ResetObjective()
        {
            _deliveredCount = 0;
        }

        public void SetDeliveredCountAuthoritative(int deliveredCount)
        {
            _deliveredCount = Mathf.Clamp(deliveredCount, 0, Mathf.Max(0, _requiredScrapCount));
        }
    }
}
