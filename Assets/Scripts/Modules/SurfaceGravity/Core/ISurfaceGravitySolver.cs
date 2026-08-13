namespace Modules.SurfaceGravity.Core
{
    public interface ISurfaceGravitySolver
    {
        GravityStepResult Solve(in GravityStepInput input);
    }
}
