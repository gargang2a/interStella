using InterStella.Game.Shared.State;
using UnityEngine;

namespace InterStella.Game.Features.Tether
{
    [RequireComponent(typeof(TetherLink))]
    public sealed class TetherVerletRopeView : MonoBehaviour
    {
        [SerializeField]
        private TetherLink _link;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField, Min(4)]
        private int _nodeCount = 24;

        [SerializeField, Min(1)]
        private int _solverIterations = 24;

        [SerializeField, Range(0.8f, 1f)]
        private float _damping = 0.985f;

        [SerializeField]
        private Vector3 _gravity = Vector3.zero;

        [SerializeField]
        private bool _drawWhenBroken = true;

        [SerializeField, Range(0f, 1f)]
        private float _smoothing = 0.45f;

        [SerializeField]
        private Color _slackColor = new Color(0.35f, 0.8f, 1f);

        [SerializeField]
        private Color _tensionColor = new Color(1f, 0.8f, 0.2f);

        [SerializeField]
        private Color _hardLimitColor = new Color(1f, 0.4f, 0.2f);

        [SerializeField]
        private Color _brokenColor = new Color(0.7f, 0.2f, 0.2f);

        private Vector3[] _nodes;
        private Vector3[] _previousNodes;
        private Vector3[] _smoothedNodes;
        private int _cachedNodeCount;
        private float _segmentLength;
        private bool _initialized;

        private void Awake()
        {
            if (_link == null)
            {
                _link = GetComponent<TetherLink>();
            }

            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
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

            Vector3 start = endpointA.transform.position;
            Vector3 end = endpointB.transform.position;

            int safeNodeCount = Mathf.Max(4, _nodeCount);
            if (!_initialized || _cachedNodeCount != safeNodeCount)
            {
                BuildNodes(start, end, safeNodeCount);
            }

            float endpointDistance = Vector3.Distance(start, end);
            TetherTensionLevel level = _link.EvaluateTensionLevel(endpointDistance);
            if (level == TetherTensionLevel.Broken && !_drawWhenBroken)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;

            SimulateNodes(start, end);
            SolveDistanceConstraints(start, end);
            DrawNodes(level);
        }

        private void BuildNodes(Vector3 start, Vector3 end, int safeNodeCount)
        {
            _cachedNodeCount = safeNodeCount;
            _nodes = new Vector3[safeNodeCount];
            _previousNodes = new Vector3[safeNodeCount];
            _smoothedNodes = new Vector3[safeNodeCount];

            for (int i = 0; i < safeNodeCount; i++)
            {
                float t = i / (safeNodeCount - 1f);
                Vector3 p = Vector3.Lerp(start, end, t);
                _nodes[i] = p;
                _previousNodes[i] = p;
                _smoothedNodes[i] = p;
            }

            _lineRenderer.positionCount = safeNodeCount;
            _initialized = true;
        }

        private void SimulateNodes(Vector3 start, Vector3 end)
        {
            float dt = Time.deltaTime;
            float dt2 = dt * dt;

            _nodes[0] = start;
            _nodes[_nodes.Length - 1] = end;
            _previousNodes[0] = start;
            _previousNodes[_nodes.Length - 1] = end;

            for (int i = 1; i < _nodes.Length - 1; i++)
            {
                Vector3 current = _nodes[i];
                Vector3 velocity = (current - _previousNodes[i]) * _damping;
                _previousNodes[i] = current;
                _nodes[i] = current + velocity + (_gravity * dt2);
            }
        }

        private void SolveDistanceConstraints(Vector3 start, Vector3 end)
        {
            float currentDistance = Vector3.Distance(start, end);
            float totalLength = Mathf.Max(currentDistance, _link.MaxLength);
            _segmentLength = Mathf.Max(0.001f, totalLength / (_nodes.Length - 1f));

            int lastIndex = _nodes.Length - 1;
            int iterations = Mathf.Max(1, _solverIterations);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                _nodes[0] = start;
                _nodes[lastIndex] = end;

                for (int i = 0; i < lastIndex; i++)
                {
                    Vector3 delta = _nodes[i + 1] - _nodes[i];
                    float distance = delta.magnitude;
                    if (distance <= 0.0001f)
                    {
                        continue;
                    }

                    float error = distance - _segmentLength;
                    Vector3 correction = (delta / distance) * error;

                    if (i == 0)
                    {
                        _nodes[i + 1] -= correction;
                    }
                    else if (i + 1 == lastIndex)
                    {
                        _nodes[i] += correction;
                    }
                    else
                    {
                        Vector3 half = correction * 0.5f;
                        _nodes[i] += half;
                        _nodes[i + 1] -= half;
                    }
                }
            }

            _previousNodes[0] = start;
            _previousNodes[lastIndex] = end;
        }

        private void DrawNodes(TetherTensionLevel level)
        {
            Color targetColor;
            switch (level)
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
            if (_smoothing <= 0f || _smoothedNodes == null || _smoothedNodes.Length != _nodes.Length)
            {
                _lineRenderer.SetPositions(_nodes);
                return;
            }

            int lastIndex = _nodes.Length - 1;
            _smoothedNodes[0] = _nodes[0];
            _smoothedNodes[lastIndex] = _nodes[lastIndex];
            for (int i = 1; i < lastIndex; i++)
            {
                Vector3 neighborAverage = (_nodes[i - 1] + _nodes[i + 1]) * 0.5f;
                _smoothedNodes[i] = Vector3.Lerp(_nodes[i], neighborAverage, _smoothing);
            }

            _lineRenderer.SetPositions(_smoothedNodes);
        }
    }
}
