using UnityEngine;

namespace InterStella.Game.Netcode.Runtime
{
    public sealed class LocalSessionService : MonoBehaviour, ISessionService
    {
        [SerializeField]
        private bool _startAsHost = true;

        private bool _isSessionActive;

        public bool IsSessionActive => _isSessionActive;
        public bool IsHost => _startAsHost;

        public bool StartSession()
        {
            _isSessionActive = true;
            return true;
        }

        public void StopSession()
        {
            _isSessionActive = false;
        }
    }
}
