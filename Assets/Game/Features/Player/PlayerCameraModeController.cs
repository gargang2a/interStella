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

        private CameraMode _currentMode;
        private bool _isInitialized;

        public string CurrentModeName => _currentMode.ToString();

        private void Awake()
        {
            _currentMode = _startMode;
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
            if (_primaryTarget == null)
            {
                GameObject primaryPlayer = GameObject.Find("PlayerA");
                if (primaryPlayer != null)
                {
                    _primaryTarget = primaryPlayer.transform;
                }
            }

            if (_secondaryTarget == null)
            {
                GameObject secondaryPlayer = GameObject.Find("PlayerB");
                if (secondaryPlayer != null)
                {
                    _secondaryTarget = secondaryPlayer.transform;
                }
            }
        }
    }
}
