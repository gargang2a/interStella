using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Tether
{
    [RequireComponent(typeof(TetherLink))]
    public sealed class TetherView : MonoBehaviour
    {
        [SerializeField]
        private TetherLink _link;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private Color _slackColor = new Color(0.35f, 0.8f, 1f);

        [SerializeField]
        private Color _tensionColor = new Color(1f, 0.8f, 0.2f);

        [SerializeField]
        private Color _hardLimitColor = new Color(1f, 0.4f, 0.2f);

        [SerializeField]
        private Color _brokenColor = new Color(0.7f, 0.2f, 0.2f);

        private void Awake()
        {
            if (_link == null)
            {
                _link = GetComponent<TetherLink>();
            }
        }

        private void LateUpdate()
        {
            if (_link == null || _lineRenderer == null)
            {
                return;
            }

            if (!_link.TryGetEndpoints(out TetherEndpoint endpointA, out TetherEndpoint endpointB))
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, endpointA.transform.position);
            _lineRenderer.SetPosition(1, endpointB.transform.position);

            Color targetColor;
            switch (_link.CurrentLevel)
            {
                case TetherTensionLevel.HardLimit:
                    targetColor = _hardLimitColor;
                    break;
                case TetherTensionLevel.Tension:
                case TetherTensionLevel.NearLimit:
                    targetColor = _tensionColor;
                    break;
                case TetherTensionLevel.Broken:
                    targetColor = _brokenColor;
                    break;
                default:
                    targetColor = _slackColor;
                    break;
            }

            _lineRenderer.startColor = targetColor;
            _lineRenderer.endColor = targetColor;
        }
    }
}
