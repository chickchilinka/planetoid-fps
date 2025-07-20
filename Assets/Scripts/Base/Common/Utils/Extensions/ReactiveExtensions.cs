using System;
using UniRx;

namespace Base.Common.Utils.Extensions
{
    public static class ReactiveExtensions
    {
        public static IObservable<Unit> EveryFrameDueTime(float timeInSeconds)
        {
            return Observable.EveryUpdate().Select(_ => Unit.Default)
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(timeInSeconds)));
        }
    }
}