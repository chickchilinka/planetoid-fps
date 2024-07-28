using System;
using UniRx;

namespace Utils.Extensions
{
    public static class ReactiveExtensions
    {
        public static IObservable<Unit> EveryFrameDueTime(float timeInSeconds = 5f)
        {
            return Observable.EveryUpdate().Select(_ => Unit.Default)
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(timeInSeconds)));
        }
    }
}