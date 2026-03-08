using UnityEngine;

namespace InterStella.Game.Features.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraModeController : MonoBehaviour
    {
        private enum CameraMode
        {
            FirstPerson = 0,
            Overview = 1,
            ThirdPerson = 2
        }

        [Header("Targets")]
        [SerializeField]
        private Transform _primaryTarget;

        [SerializeField]
        private Transform _secondaryTarget;

        [SerializeField]
        private PlayerNetworkBridge[] _playerBridges;

        [Header("Key Bindings")]
        [SerializeField]
        private KeyCode _firstPersonKey = KeyCode.Alpha1;

        [SerializeField]
        private KeyCode _overviewKey = KeyCode.Alpha2;

        [SerializeField]
        private KeyCode _thirdPersonKey = KeyCode.Alpha3;

        [Header("Offsets")]
        [SerializeField]
        private Vector3 _firstPersonLocalOffset = new Vector3(0f, 0.35f, 0.05f);

        [SerializeField]
        private Vector3 _thirdPersonLocalOffset = new Vector3(0f, 1.35f, -4f);

        [SerializeField]
        private Vector3 _overviewWorldOffset = new Vector3(0f, 18f, -18f);

        [Header("Follow")]
        [SerializeField]
        private float _followSharpness = 14f;

        [SerializeField]
        private CameraMode _startMode = CameraMode.FirstPerson;

        [SerializeField]
        private bool _preferLocalOwnerTarget = true;

        [SerializeField]
        private bool _logTargetRetarget = true;

        private CameraMode _currentMode;
        private bool _isInitialized;
        private Transform _configuredPrimaryTarget;
        private Transform _configuredSecondaryTarget;

        public string CurrentModeName => _currentMode.ToString();

        private void Awake()
        {
            _currentMode = _startMode;
            _configuredPrimaryTarget = _primaryTarget;
            _configuredSecondaryTarget = _secondaryTarget;
            ResolveTargetsIfMissing();
        }

        private void OnEnable()
        {
            _isInitialized = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_firstPersonKey))
            {
                SetFirstPersonMode();
            }
            else if (Input.GetKeyDown(_overviewKey))
            {
                SetOverviewMode();
            }
            else if (Input.GetKeyDown(_thirdPersonKey))
            {
                SetThirdPersonMode();
            }
        }

        public void SetFirstPersonMode()
        {
            SetMode(CameraMode.FirstPerson);
        }

        public void SetOverviewMode()
        {
            SetMode(CameraMode.Overview);
        }

        public void SetThirdPersonMode()
        {
            SetMode(CameraMode.ThirdPerson);
        }

        public bool ForceSnapToCurrentMode()
        {
            ResolveTargetsIfMissing();
            if (_primaryTarget == null)
            {
                return false;
            }

            ComputeDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation);
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            _isInitialized = true;
            return true;
        }

        private void LateUpdate()
        {
            ResolveTargetsIfMissing();
            if (_primaryTarget == null)
            {
                return;
            }

            Vector3 desiredPosition;
            Quaternion desiredRotation;
            ComputeDesiredPose(out desiredPosition, out desiredRotation);

            if (!_isInitialized || _currentMode == CameraMode.FirstPerson)
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                _isInitialized = true;
                return;
            }

            float lerpT = 1f - Mathf.Exp(-Mathf.Max(0f, _followSharpness) * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, lerpT);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, lerpT);
            _isInitialized = true;
        }

        private void SetMode(CameraMode mode)
        {
            if (_currentMode == mode)
            {
                return;
            }

            _currentMode = mode;
            _isInitialized = false;
            Debug.Log("[InterStella][CameraMode] switched=" + _currentMode);
        }

        private void ComputeDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
        {
            if (_currentMode == CameraMode.FirstPerson)
            {
                desiredPosition = _primaryTarget.TransformPoint(_firstPersonLocalOffset);
                desiredRotation = _primaryTarget.rotation;
                return;
            }

            if (_currentMode == CameraMode.ThirdPerson)
            {
                desiredPosition = _primaryTarget.TransformPoint(_thirdPersonLocalOffset);
                Vector3 lookDirection = _primaryTarget.position - desiredPosition;
                if (lookDirection.sqrMagnitude <= 0.0001f)
                {
                    lookDirection = _primaryTarget.forward;
                }

                desiredRotation = Quaternion.LookRotation(lookDirection.normalized, _primaryTarget.up);
                return;
            }

            Vector3 focusPoint = _primaryTarget.position;
            if (_secondaryTarget != null)
            {
                focusPoint = (focusPoint + _secondaryTarget.position) * 0.5f;
            }

            desiredPosition = focusPoint + _overviewWorldOffset;
            Vector3 overviewDirection = focusPoint - desiredPosition;
            if (overviewDirection.sqrMagnitude <= 0.0001f)
            {
                overviewDirection = Vector3.forward;
            }

            desiredRotation = Quaternion.LookRotation(overviewDirection.normalized, Vector3.up);
        }

        private void ResolveTargetsIfMissing()
        {
            ResolvePlayerBridgesIfMissing();
            if (_preferLocalOwnerTarget && TryResolveLocalOwnerTargets(out Transform localPrimaryTarget, out Transform localSecondaryTarget))
            {
                ApplyTargets(localPrimaryTarget, localSecondaryTarget);
                return;
            }

            Transform fallbackPrimaryTarget = _configuredPrimaryTarget;
            if (fallbackPrimaryTarget == null)
            {
                GameObject primaryPlayer = GameObject.Find("PlayerA");
                if (primaryPlayer != null)
                {
                    fallbackPrimaryTarget = primaryPlayer.transform;
                }
            }

            Transform fallbackSecondaryTarget = _configuredSecondaryTarget;
            if (fallbackSecondaryTarget == null)
            {
                GameObject secondaryPlayer = GameObject.Find("PlayerB");
                if (secondaryPlayer != null)
                {
                    fallbackSecondaryTarget = secondaryPlayer.transform;
                }
            }

            ApplyTargets(fallbackPrimaryTarget, fallbackSecondaryTarget);
        }

        private void ResolvePlayerBridgesIfMissing()
        {
            if (_playerBridges == null || _playerBridges.Length == 0)
            {
                _playerBridges = FindObjectsOfType<PlayerNetworkBridge>();
                return;
            }

            for (int i = 0; i < _playerBridges.Length; i++)
            {
                if (_playerBridges[i] != null)
                {
                    continue;
                }

                _playerBridges = FindObjectsOfType<PlayerNetworkBridge>();
                return;
            }
        }

        private bool TryResolveLocalOwnerTargets(out Transform primaryTarget, out Transform secondaryTarget)
        {
            primaryTarget = null;
            secondaryTarget = null;

            if (_playerBridges == null || _playerBridges.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < _playerBridges.Length; i++)
            {
                PlayerNetworkBridge bridge = _playerBridges[i];
                if (bridge == null || !bridge.IsAuthoritativeOwner)
                {
                    continue;
                }

                primaryTarget = bridge.transform;
                break;
            }

            if (primaryTarget == null)
            {
                return false;
            }

            for (int i = 0; i < _playerBridges.Length; i++)
            {
                PlayerNetworkBridge bridge = _playerBridges[i];
                if (bridge == null || bridge.transform == primaryTarget)
                {
                    continue;
                }

                secondaryTarget = bridge.transform;
                break;
            }

            return true;
        }

        private void ApplyTargets(Transform primaryTarget, Transform secondaryTarget)
        {
            if (_primaryTarget == primaryTarget && _secondaryTarget == secondaryTarget)
            {
                return;
            }

            _primaryTarget = primaryTarget;
            _secondaryTarget = secondaryTarget;
            _isInitialized = false;

            if (_logTargetRetarget)
            {
                Debug.Log("[InterStella][CameraMode] retargeted primary=" + GetTargetName(_primaryTarget) + ", secondary=" + GetTargetName(_secondaryTarget) + ", mode=" + _currentMode);
            }
        }

        private static string GetTargetName(Transform target)
        {
            return target == null ? "null" : target.name;
        }
    }
}
