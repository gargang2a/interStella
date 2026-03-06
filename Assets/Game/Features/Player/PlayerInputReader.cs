using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Translation")]
        [SerializeField]
        private string _horizontalAxis = "Horizontal";

        [SerializeField]
        private string _forwardAxis = "Vertical";

        [SerializeField]
        private KeyCode _upKey = KeyCode.R;

        [SerializeField]
        private KeyCode _downKey = KeyCode.F;

        [Header("Rotation")]
        [SerializeField]
        private string _lookXAxis = "Mouse X";

        [SerializeField]
        private string _lookYAxis = "Mouse Y";

        [SerializeField]
        [Range(0.05f, 3f)]
        private float _lookSensitivity = 0.25f;

        [SerializeField]
        private KeyCode _rollLeftKey = KeyCode.Q;

        [SerializeField]
        private KeyCode _rollRightKey = KeyCode.E;

        [Header("Actions")]
        [SerializeField]
        private KeyCode _boostKey = KeyCode.LeftShift;

        [SerializeField]
        private KeyCode _brakeKey = KeyCode.Space;

        public PlayerInputSample CurrentSample { get; private set; }

        public void ClearSample()
        {
            CurrentSample = default;
        }

        private void OnDisable()
        {
            CurrentSample = default;
        }

        private void Update()
        {
            float upDown = 0f;
            if (Input.GetKey(_upKey))
            {
                upDown += 1f;
            }

            if (Input.GetKey(_downKey))
            {
                upDown -= 1f;
            }

            Vector3 translation = new Vector3(
                Input.GetAxisRaw(_horizontalAxis),
                upDown,
                Input.GetAxisRaw(_forwardAxis));
            translation = Vector3.ClampMagnitude(translation, 1f);

            Vector2 lookDelta = new Vector2(
                Input.GetAxisRaw(_lookXAxis),
                -Input.GetAxisRaw(_lookYAxis));
            lookDelta *= Mathf.Max(0f, _lookSensitivity);

            float roll = 0f;
            if (Input.GetKey(_rollRightKey))
            {
                roll += 1f;
            }

            if (Input.GetKey(_rollLeftKey))
            {
                roll -= 1f;
            }

            CurrentSample = new PlayerInputSample(
                translation,
                lookDelta,
                roll,
                Input.GetKey(_boostKey),
                Input.GetKey(_brakeKey));
        }
    }
}
