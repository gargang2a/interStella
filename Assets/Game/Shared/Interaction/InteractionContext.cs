using UnityEngine;

namespace InterStella.Game.Shared.Interaction
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject interactor, Transform origin)
        {
            Interactor = interactor;
            Origin = origin;
        }

        public GameObject Interactor { get; }
        public Transform Origin { get; }
    }
}
