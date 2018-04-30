﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class IsEmpty<TSource> : Producer<bool, IsEmpty<TSource>._>
    {
        private readonly IObservable<TSource> _source;

        public IsEmpty(IObservable<TSource> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<bool> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : Sink<bool>, IObserver<TSource>
        {
            public _(IObserver<bool> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public void OnNext(TSource value)
            {
                base.ForwardOnNext(false);
                base.ForwardOnCompleted();
            }

            public void OnError(Exception error)
            {
                base.ForwardOnError(error);
            }

            public void OnCompleted()
            {
                base.ForwardOnNext(true);
                base.ForwardOnCompleted();
            }
        }
    }
}
