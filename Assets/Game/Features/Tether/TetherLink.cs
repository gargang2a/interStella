using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Tether
{
    public sealed class TetherLink : MonoBehaviour
    {
        [SerializeField]
        private TetherEndpoint _endpointA;

        [SerializeField]
        private TetherEndpoint _endpointB;

        [SerializeField]
        private bool _isEnabled = true;

        [SerializeField]
        private float _maxLength = 10f;

        [SerializeField, Range(0.1f, 1f)]
        private float _nearLimitRatio = 0.8f;

        [SerializeField, Range(0.1f, 1f)]
        private float _tensionRatio = 0.95f;

        [SerializeField]
        private float _breakDistanceMultiplier = 1.35f;

        private bool _isBroken;
        private float _currentDistance;
        private TetherTensionLevel _currentLevel;

        public TetherEndpoint EndpointA => _endpointA;
        public TetherEndpoint EndpointB => _endpointB;
        public bool IsEnabled => _isEnabled;
        public bool IsBroken => _isBroken;
        public float MaxLength => Mathf.Max(0.01f, _maxLength);
        public float CurrentDistance => _currentDistance;
        public TetherTensionLevel CurrentLevel => _currentLevel;

        public bool TryGetEndpoints(out TetherEndpoint endpointA, out TetherEndpoint endpointB)
        {
            endpointA = _endpointA;
            endpointB = _endpointB;
            return endpointA != null && endpointB != null;
        }

        public TetherTensionLevel EvaluateTensionLevel(float distance)
        {
            if (!_isEnabled)
            {
                return TetherTensionLevel.Slack;
            }

            if (_isBroken)
            {
                return TetherTensionLevel.Broken;
            }

            float safeMaxLength = MaxLength;
            if (distance >= safeMaxLength * Mathf.Max(1f, _breakDistanceMultiplier))
            {
                return TetherTensionLevel.Broken;
            }

            if (distance >= safeMaxLength)
            {
                return TetherTensionLevel.HardLimit;
            }

            if (distance >= safeMaxLength * _tensionRatio)
            {
                return TetherTensionLevel.Tension;
            }

            if (distance >= safeMaxLength * _nearLimitRatio)
            {
                return TetherTensionLevel.NearLimit;
            }

            return TetherTensionLevel.Slack;
        }

        public void SetRuntimeState(float distance, TetherTensionLevel level)
        {
            _currentDistance = distance;
            _currentLevel = level;

            if (level == TetherTensionLevel.Broken)
            {
                _isBroken = true;
            }
        }

        public void MarkBroken(float distance)
        {
            _isBroken = true;
            _currentDistance = distance;
            _currentLevel = TetherTensionLevel.Broken;
        }

        public void Restore()
        {
            _isBroken = false;
            _currentDistance = 0f;
            _currentLevel = TetherTensionLevel.Slack;
        }

        public TetherState BuildState()
        {
            int endpointAId = _endpointA == null ? -1 : _endpointA.EndpointId;
            int endpointBId = _endpointB == null ? -1 : _endpointB.EndpointId;
            return new TetherState(
                endpointAId,
                endpointBId,
                _isEnabled,
                _isBroken,
                MaxLength,
                _currentDistance,
                _currentLevel);
        }
    }
}
