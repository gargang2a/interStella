using UnityEngine;

namespace InterStella.Game.Features.Scavenge
{
    [CreateAssetMenu(
        fileName = "ScrapItemDefinition",
        menuName = "interStella/Scavenge/Scrap Item Definition")]
    public sealed class ScrapItemDefinition : ScriptableObject
    {
        [SerializeField]
        private string _displayName = "Generic Scrap";

        [SerializeField]
        private int _deliveryValue = 1;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public int DeliveryValue => Mathf.Max(1, _deliveryValue);
    }
}
