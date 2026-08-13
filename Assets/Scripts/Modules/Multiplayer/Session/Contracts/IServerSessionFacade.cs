using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public interface IServerSessionFacade
    {
        ValueTask<Result<PlayerId, SessionError>> JoinAsync(SessionConnection connection, CancellationToken token);
        ValueTask<Result<Unit, SessionError>> LeaveAsync(PlayerId playerId, CancellationToken token);
        ValueTask<Result<Unit, SessionError>> SetReadyAsync(PlayerId playerId, bool ready, CancellationToken token);
        ValueTask<Result<Unit, SessionError>> NotifyServerWorldReadyAsync(
            OperationId operationId,
            MatchId matchId,
            CancellationToken token);
        ValueTask<Result<Unit, SessionError>> NotifyPlayerWorldReadyAsync(
            OperationId operationId,
            PlayerId playerId,
            MatchId matchId,
            CancellationToken token);
        SessionSnapshot Snapshot { get; }
    }
}
