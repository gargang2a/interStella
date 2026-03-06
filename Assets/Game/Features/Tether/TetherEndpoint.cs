using InterStella.Game.Features.Player;
using UnityEngine;

namespace InterStella.Game.Features.Tether
{
    public sealed class TetherEndpoint : MonoBehaviour
    {
        [SerializeField]
        private int _endpointId = -1;

        [SerializeField]
        private Rigidbody _body;

        [SerializeField]
        private PlayerMotor _playerMotor;

        public int EndpointId => _endpointId;
        public Rigidbody Body => _body;
        public PlayerMotor PlayerMotor => _playerMotor;

        private void Awake()
        {
            if (_endpointId < 0)
            {
                _endpointId = gameObject.GetInstanceID();
            }

            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (_playerMotor == null)
            {
                _playerMotor = GetComponent<PlayerMotor>();
            }
        }
    }
}
