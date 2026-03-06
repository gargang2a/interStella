using UnityEngine;

namespace InterStella.Game.Features.Player
{
    public readonly struct PlayerMotorState
    {
        public PlayerMotorState(
            Vector3 velocity,
            Vector3 angularVelocity,
            bool isBoosting,
            bool isBraking,
            bool isFuelDepleted,
            bool isTetherConstrained)
        {
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            IsBoosting = isBoosting;
            IsBraking = isBraking;
            IsFuelDepleted = isFuelDepleted;
            IsTetherConstrained = isTetherConstrained;
        }

        public Vector3 Velocity { get; }
        public Vector3 AngularVelocity { get; }
        public bool IsBoosting { get; }
        public bool IsBraking { get; }
        public bool IsFuelDepleted { get; }
        public bool IsTetherConstrained { get; }
    }
}
