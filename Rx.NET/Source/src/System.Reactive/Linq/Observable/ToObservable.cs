﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class ToObservable<TSource> : Producer<TSource, ToObservable<TSource>._>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly IScheduler _scheduler;

        public ToObservable(IEnumerable<TSource> source, IScheduler scheduler)
        {
            _source = source;
            _scheduler = scheduler;
        }

        protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => sink.Run(this);

        internal sealed class _ : IdentitySink<TSource>
        {
            public _(IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public IDisposable Run(ToObservable<TSource> parent)
            {
                var e = default(IEnumerator<TSource>);
                try
                {
                    e = parent._source.GetEnumerator();
                }
                catch (Exception exception)
                {
                    base.ForwardOnError(exception);
                    return Disposable.Empty;
                }

                var longRunning = parent._scheduler.AsLongRunning();
                if (longRunning != null)
                {
                    //
                    // Long-running schedulers have the contract they should *never* prevent
                    // the work from starting, such that the scheduled work has the chance
                    // to observe the cancellation and perform proper clean-up. In this case,
                    // we're sure Loop will be entered, allowing us to dispose the enumerator.
                    //
                    return longRunning.ScheduleLongRunning(e, Loop);
                }
                else
                {
                    //
                    // We never allow the scheduled work to be cancelled. Instead, the flag
                    // is used to have LoopRec bail out and perform proper clean-up of the
                    // enumerator.
                    //
                    var flag = new BooleanDisposable();
                    parent._scheduler.Schedule(new State(flag, e), LoopRec);
                    return flag;
                }
            }

            private sealed class State
            {
                public readonly ICancelable flag;
                public readonly IEnumerator<TSource> enumerator;

                public State(ICancelable flag, IEnumerator<TSource> enumerator)
                {
                    this.flag = flag;
                    this.enumerator = enumerator;
                }
            }

            private void LoopRec(State state, Action<State> recurse)
            {
                var hasNext = false;
                var ex = default(Exception);
                var current = default(TSource);

                if (state.flag.IsDisposed)
                {
                    state.enumerator.Dispose();
                    return;
                }

                try
                {
                    hasNext = state.enumerator.MoveNext();
                    if (hasNext)
                        current = state.enumerator.Current;
                }
                catch (Exception exception)
                {
                    ex = exception;
                }

                if (ex != null)
                {
                    state.enumerator.Dispose();

                    base.ForwardOnError(ex);
                    return;
                }

                if (!hasNext)
                {
                    state.enumerator.Dispose();

                    base.ForwardOnCompleted();
                    return;
                }

                base.ForwardOnNext(current);
                recurse(state);
            }

            private void Loop(IEnumerator<TSource> enumerator, ICancelable cancel)
            {
                while (!cancel.IsDisposed)
                {
                    var hasNext = false;
                    var ex = default(Exception);
                    var current = default(TSource);

                    try
                    {
                        hasNext = enumerator.MoveNext();
                        if (hasNext)
                            current = enumerator.Current;
                    }
                    catch (Exception exception)
                    {
                        ex = exception;
                    }

                    if (ex != null)
                    {
                        base.ForwardOnError(ex);
                        break;
                    }

                    if (!hasNext)
                    {
                        base.ForwardOnCompleted();
                        break;
                    }

                    base.ForwardOnNext(current);
                }

                enumerator.Dispose();
                base.Dispose();
            }
        }
    }
}
