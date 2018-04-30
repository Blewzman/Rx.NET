﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal static class FirstOrDefaultAsync<TSource>
    {
        internal sealed class Sequence : Producer<TSource, Sequence._>
        {
            private readonly IObservable<TSource> _source;

            public Sequence(IObservable<TSource> source)
            {
                _source = source;
            }

            protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(observer, cancel);

            protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

            internal sealed class _ : Sink<TSource>, IObserver<TSource>
            {
                public _(IObserver<TSource> observer, IDisposable cancel)
                    : base(observer, cancel)
                {
                }

                public void OnNext(TSource value)
                {
                    base.ForwardOnNext(value);
                    base.ForwardOnCompleted();
                }

                public void OnError(Exception error)
                {
                    base.ForwardOnError(error);
                }

                public void OnCompleted()
                {
                    base.ForwardOnNext(default(TSource));
                    base.ForwardOnCompleted();
                }
            }
        }

        internal sealed class Predicate : Producer<TSource, Predicate._>
        {
            private readonly IObservable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;

            public Predicate(IObservable<TSource> source, Func<TSource, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(_predicate, observer, cancel);

            protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

            internal sealed class _ : Sink<TSource>, IObserver<TSource>
            {
                private readonly Func<TSource, bool> _predicate;

                public _(Func<TSource, bool> predicate, IObserver<TSource> observer, IDisposable cancel)
                    : base(observer, cancel)
                {
                    _predicate = predicate;
                }

                public void OnNext(TSource value)
                {
                    var b = false;

                    try
                    {
                        b = _predicate(value);
                    }
                    catch (Exception ex)
                    {
                        base.ForwardOnError(ex);
                        return;
                    }

                    if (b)
                    {
                        base.ForwardOnNext(value);
                        base.ForwardOnCompleted();
                    }
                }

                public void OnError(Exception error)
                {
                    base.ForwardOnError(error);
                }

                public void OnCompleted()
                {
                    base.ForwardOnNext(default(TSource));
                    base.ForwardOnCompleted();
                }
            }
        }
    }
}
