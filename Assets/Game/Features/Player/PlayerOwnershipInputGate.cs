using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerOwnershipInputGate : MonoBehaviour
    {
        [SerializeField]
        private PlayerNetworkBridge _networkBridge;

        [SerializeField]
        private PlayerInputReader _inputReader;

        [SerializeField]
        private PlayerInteraction _playerInteraction;

        [SerializeField]
        private bool _enableWhenBridgeMissing = true;

        private void Awake()
        {
            ResolveDependenciesIfMissing();
            ApplyGate();
        }

        private void LateUpdate()
        {
            ApplyGate();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependenciesIfMissing();
        }
#endif

        private void ResolveDependenciesIfMissing()
        {
            if (_networkBridge == null)
            {
                _networkBridge = GetComponent<PlayerNetworkBridge>();
            }

            if (_inputReader == null)
            {
                _inputReader = GetComponent<PlayerInputReader>();
            }

            if (_playerInteraction == null)
            {
                _playerInteraction = GetComponent<PlayerInteraction>();
            }
        }

        private void ApplyGate()
        {
            if (_inputReader == null)
            {
                return;
            }

            bool shouldEnable;
            if (_networkBridge == null)
            {
                shouldEnable = _enableWhenBridgeMissing;
            }
            else
            {
                shouldEnable = _networkBridge.IsAuthoritativeOwner;
            }

            if (_inputReader.enabled != shouldEnable)
            {
                if (!shouldEnable)
                {
                    _inputReader.ClearSample();
                }

                _inputReader.enabled = shouldEnable;
            }

            if (_playerInteraction != null)
            {
                bool shouldEnableInteraction = _networkBridge == null
                    ? _enableWhenBridgeMissing
                    : _networkBridge.IsAuthoritativeOwner;
                if (_playerInteraction.enabled != shouldEnableInteraction)
                {
                    _playerInteraction.enabled = shouldEnableInteraction;
                }
            }
        }
    }
}
