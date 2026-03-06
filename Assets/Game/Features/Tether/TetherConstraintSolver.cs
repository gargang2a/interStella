using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Tether
{
    [RequireComponent(typeof(TetherLink))]
    public sealed class TetherConstraintSolver : MonoBehaviour
    {
        [SerializeField]
        private TetherLink _link;

        [SerializeField]
        private float _tensionCorrectionStrength = 8f;

        [SerializeField]
        private float _hardLimitCorrectionStrength = 16f;

        private void Awake()
        {
            if (_link == null)
            {
                _link = GetComponent<TetherLink>();
            }
        }

        private void FixedUpdate()
        {
            if (_link == null || !_link.TryGetEndpoints(out TetherEndpoint endpointA, out TetherEndpoint endpointB))
            {
                return;
            }

            if (endpointA.Body == null || endpointB.Body == null)
            {
                return;
            }

            Vector3 delta = endpointB.Body.position - endpointA.Body.position;
            float distance = delta.magnitude;
            TetherTensionLevel tensionLevel = _link.EvaluateTensionLevel(distance);
            _link.SetRuntimeState(distance, tensionLevel);

            bool isConstrained = tensionLevel == TetherTensionLevel.Tension || tensionLevel == TetherTensionLevel.HardLimit;
            if (endpointA.PlayerMotor != null)
            {
                endpointA.PlayerMotor.SetTetherConstrained(isConstrained);
            }

            if (endpointB.PlayerMotor != null)
            {
                endpointB.PlayerMotor.SetTetherConstrained(isConstrained);
            }

            if (tensionLevel == TetherTensionLevel.Broken || distance <= 0.0001f)
            {
                return;
            }

            float overshoot = distance - _link.MaxLength;
            if (overshoot <= 0f)
            {
                return;
            }

            float correctionStrength = tensionLevel == TetherTensionLevel.HardLimit
                ? _hardLimitCorrectionStrength
                : _tensionCorrectionStrength;
            Vector3 direction = delta / distance;
            Vector3 correction = direction * (overshoot * correctionStrength * Time.fixedDeltaTime);

            endpointA.Body.AddForce(correction, ForceMode.VelocityChange);
            endpointB.Body.AddForce(-correction, ForceMode.VelocityChange);
        }
    }
}
