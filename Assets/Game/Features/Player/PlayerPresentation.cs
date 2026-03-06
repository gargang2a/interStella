using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public sealed class PlayerPresentation : MonoBehaviour
    {
        [SerializeField]
        private PlayerFuel _playerFuel;

        [SerializeField]
        private Renderer[] _renderers;

        [SerializeField]
        private Color _normalColor = new Color(0.3f, 0.8f, 1f);

        [SerializeField]
        private Color _lowFuelColor = new Color(1f, 0.7f, 0.2f);

        [SerializeField]
        private Color _depletedColor = new Color(1f, 0.2f, 0.2f);

        [SerializeField]
        private float _lowFuelThreshold = 0.2f;

        private MaterialPropertyBlock _propertyBlock;

        private static readonly int BaseColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (_playerFuel == null)
            {
                _playerFuel = GetComponent<PlayerFuel>();
            }

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_playerFuel == null || _renderers == null || _renderers.Length == 0)
            {
                return;
            }

            float normalized = _playerFuel.MaxFuel <= 0f ? 0f : (_playerFuel.CurrentFuel / _playerFuel.MaxFuel);
            Color targetColor = _normalColor;

            if (_playerFuel.IsDepleted)
            {
                targetColor = _depletedColor;
            }
            else if (normalized <= _lowFuelThreshold)
            {
                targetColor = _lowFuelColor;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, targetColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
