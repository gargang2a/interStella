using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public readonly struct PlayerInputSample
    {
        public PlayerInputSample(
            Vector3 translation,
            Vector2 lookDelta,
            float rollInput,
            bool isBoosting,
            bool isBraking)
        {
            Translation = translation;
            LookDelta = lookDelta;
            RollInput = rollInput;
            IsBoosting = isBoosting;
            IsBraking = isBraking;
        }

        public Vector3 Translation { get; }
        public Vector2 LookDelta { get; }
        public float RollInput { get; }
        public bool IsBoosting { get; }
        public bool IsBraking { get; }
    }
}
