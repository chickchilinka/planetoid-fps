using System.Collections.Generic;
using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.Character.Simulation.Tests
{
    internal sealed class FakeCharacterGravityProvider : ICharacterGravityProvider
    {
        private readonly CharacterGravityResult _result;

        public FakeCharacterGravityProvider(
            Vector3 acceleration,
            Vector3 targetUp,
            GravityState state)
        {
            _result = new CharacterGravityResult(acceleration, targetUp, state);
        }

        public List<CharacterGravityQuery> Queries { get; } = new List<CharacterGravityQuery>();

        public CharacterGravityResult Solve(in CharacterGravityQuery query)
        {
            Queries.Add(query);
            return _result;
        }
    }
}
