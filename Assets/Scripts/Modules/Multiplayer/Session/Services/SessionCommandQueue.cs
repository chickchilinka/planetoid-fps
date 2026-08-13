using System;
using System.Threading;
using System.Threading.Tasks;

namespace Modules.Multiplayer.Session
{
    public sealed class SessionCommandQueue : IDisposable
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public async ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> command, CancellationToken token)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                return await command().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
        }
    }
}
