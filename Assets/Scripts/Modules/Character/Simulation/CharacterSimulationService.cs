using System;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public sealed class CharacterSimulationService : ICharacterSimulationService
    {
        private const float DirectionEpsilon = 0.000001f;

        private readonly ICharacterGravityProvider _gravityProvider;
        private readonly CharacterMovementSettings _settings;

        public CharacterSimulationService(
            ICharacterGravityProvider gravityProvider,
            CharacterMovementSettings settings)
        {
            _gravityProvider = gravityProvider ?? throw new ArgumentNullException(nameof(gravityProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public CharacterStepResult Simulate(
            in CharacterInput input,
            in CharacterBodySnapshot body,
            in CharacterSimulationState state,
            float tickDelta)
        {
            Validate(input, body, state, tickDelta);

            var bodyUp = (body.Rotation * Vector3.up).normalized;
            var gravityQuery = new CharacterGravityQuery(
                body.Position,
                bodyUp,
                state.Gravity,
                tickDelta);
            var gravity = _gravityProvider.Solve(gravityQuery);
            ValidateGravityResult(gravity);

            var up = gravity.TargetUp.normalized;
            BuildMovementBasis(
                body.Rotation,
                up,
                state.HeadingForward,
                state.ViewYaw,
                input.ViewYaw,
                out var forward,
                out var right);

            var move = Vector2.ClampMagnitude(input.Move, 1f);
            var desiredTangentVelocity =
                (forward * move.y + right * move.x) * _settings.MoveSpeed;
            var verticalVelocity = Vector3.Project(body.LinearVelocity, up);
            var tangentVelocity = Vector3.ProjectOnPlane(body.LinearVelocity, up);
            tangentVelocity = body.IsGrounded
                ? desiredTangentVelocity
                : Vector3.MoveTowards(
                    tangentVelocity,
                    desiredTangentVelocity,
                    _settings.AirAcceleration * tickDelta);

            var nextState = AdvanceJump(
                input,
                body.IsGrounded,
                state,
                up,
                ref verticalVelocity,
                tickDelta);
            nextState = new CharacterSimulationState(
                gravity.State,
                nextState.JumpPhase,
                nextState.JumpElapsed,
                input.ViewYaw,
                forward);

            return new CharacterStepResult(
                tangentVelocity + verticalVelocity,
                gravity.Acceleration,
                Quaternion.LookRotation(forward, up),
                nextState);
        }

        private CharacterSimulationState AdvanceJump(
            in CharacterInput input,
            bool isGrounded,
            in CharacterSimulationState state,
            Vector3 up,
            ref Vector3 verticalVelocity,
            float tickDelta)
        {
            if (input.JumpPressed && isGrounded && state.JumpPhase != JumpPhase.Holding)
            {
                verticalVelocity = up * _settings.InitialJumpSpeed;
                return input.JumpHeld
                    ? new CharacterSimulationState(
                        state.Gravity,
                        JumpPhase.Holding,
                        0f,
                        input.ViewYaw)
                    : new CharacterSimulationState(
                        state.Gravity,
                        JumpPhase.None,
                        0f,
                        input.ViewYaw);
            }

            if (state.JumpPhase != JumpPhase.Holding || !input.JumpHeld)
            {
                return new CharacterSimulationState(
                    state.Gravity,
                    JumpPhase.None,
                    0f,
                    input.ViewYaw);
            }

            var remainingHoldTime = _settings.MaxJumpHoldTime - state.JumpElapsed;
            if (remainingHoldTime <= 0f)
            {
                return new CharacterSimulationState(
                    state.Gravity,
                    JumpPhase.None,
                    0f,
                    input.ViewYaw);
            }

            var appliedHoldTime = Mathf.Min(tickDelta, remainingHoldTime);
            verticalVelocity += up * (_settings.HoldJumpAcceleration * appliedHoldTime);
            var jumpElapsed = state.JumpElapsed + appliedHoldTime;
            var jumpPhase = jumpElapsed >= _settings.MaxJumpHoldTime
                ? JumpPhase.None
                : JumpPhase.Holding;

            return new CharacterSimulationState(
                state.Gravity,
                jumpPhase,
                jumpPhase == JumpPhase.Holding ? jumpElapsed : 0f,
                input.ViewYaw);
        }

        private static void BuildMovementBasis(
            Quaternion bodyRotation,
            Vector3 up,
            Vector3 previousHeadingForward,
            float previousViewYaw,
            float currentViewYaw,
            out Vector3 forward,
            out Vector3 right)
        {
            var priorForward = Vector3.ProjectOnPlane(previousHeadingForward, up);
            if (priorForward.sqrMagnitude <= DirectionEpsilon)
            {
                priorForward = Vector3.ProjectOnPlane(
                    bodyRotation * Vector3.forward,
                    up);
                if (priorForward.sqrMagnitude <= DirectionEpsilon)
                {
                    var priorRight = Vector3.ProjectOnPlane(
                        bodyRotation * Vector3.right,
                        up);
                    priorForward = priorRight.sqrMagnitude > DirectionEpsilon
                        ? Vector3.Cross(priorRight.normalized, up)
                        : DeterministicTangent(up);
                }
            }

            priorForward.Normalize();
            var yawDelta = Mathf.DeltaAngle(previousViewYaw, currentViewYaw);
            forward = Quaternion.AngleAxis(yawDelta, up) * priorForward;
            forward = Vector3.ProjectOnPlane(forward, up).normalized;
            right = Vector3.Cross(up, forward).normalized;
        }

        private static Vector3 DeterministicTangent(Vector3 up)
        {
            var absoluteX = Mathf.Abs(up.x);
            var absoluteY = Mathf.Abs(up.y);
            var absoluteZ = Mathf.Abs(up.z);
            var leastAlignedAxis = absoluteX <= absoluteY && absoluteX <= absoluteZ
                ? Vector3.right
                : absoluteY <= absoluteZ
                    ? Vector3.up
                    : Vector3.forward;
            return Vector3.ProjectOnPlane(leastAlignedAxis, up).normalized;
        }

        private static void Validate(
            in CharacterInput input,
            in CharacterBodySnapshot body,
            in CharacterSimulationState state,
            float tickDelta)
        {
            if (!IsFinite(tickDelta) || tickDelta <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tickDelta),
                    "Tick delta must be finite and positive.");
            }

            if (!IsFinite(input.Move) || !IsFinite(input.ViewYaw))
                throw new ArgumentException("Character input must be finite.", nameof(input));

            if (!IsFinite(body.Position) ||
                !IsFinite(body.Rotation) ||
                QuaternionSqrMagnitude(body.Rotation) <= DirectionEpsilon ||
                !IsFinite(body.LinearVelocity) ||
                !IsFinite(body.GroundNormal) ||
                (body.IsGrounded && body.GroundNormal.sqrMagnitude <= DirectionEpsilon))
            {
                throw new ArgumentException("Character body snapshot is invalid.", nameof(body));
            }

            if ((state.JumpPhase != JumpPhase.None && state.JumpPhase != JumpPhase.Holding) ||
                !IsFinite(state.JumpElapsed) ||
                state.JumpElapsed < 0f ||
                !IsFinite(state.ViewYaw) ||
                !IsFinite(state.HeadingForward) ||
                !IsFinite(state.Gravity.SmoothedUp) ||
                state.Gravity.SmoothedUp.sqrMagnitude <= DirectionEpsilon)
            {
                throw new ArgumentException("Character simulation state is invalid.", nameof(state));
            }
        }

        private static void ValidateGravityResult(in CharacterGravityResult result)
        {
            if (!IsFinite(result.Acceleration) ||
                !IsFinite(result.TargetUp) ||
                result.TargetUp.sqrMagnitude <= DirectionEpsilon ||
                !IsFinite(result.State.SmoothedUp) ||
                result.State.SmoothedUp.sqrMagnitude <= DirectionEpsilon)
            {
                throw new InvalidOperationException("The character gravity provider returned an invalid result.");
            }
        }

        private static float QuaternionSqrMagnitude(Quaternion value)
        {
            return value.x * value.x +
                   value.y * value.y +
                   value.z * value.z +
                   value.w * value.w;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
