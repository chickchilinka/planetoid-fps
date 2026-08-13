using System;
using Modules.Character.Simulation;
using Modules.SurfaceGravity.Core;

namespace Modules.Character.UnityRuntime
{
    public sealed class CharacterSurfaceGravityAdapter : ICharacterGravityProvider
    {
        private readonly ISurfaceGravitySolver _solver;

        public CharacterSurfaceGravityAdapter(ISurfaceGravitySolver solver)
        {
            _solver = solver ?? throw new ArgumentNullException(nameof(solver));
        }

        public CharacterGravityResult Solve(in CharacterGravityQuery query)
        {
            var result = _solver.Solve(new GravityStepInput(
                query.Position,
                query.BodyUp,
                query.PreviousState,
                query.TickDelta));
            return new CharacterGravityResult(
                result.Acceleration,
                result.TargetUp,
                result.State);
        }
    }
}
