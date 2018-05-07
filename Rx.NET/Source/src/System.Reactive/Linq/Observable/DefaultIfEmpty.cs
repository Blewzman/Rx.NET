﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class DefaultIfEmpty<TSource> : Producer<TSource, DefaultIfEmpty<TSource>._>
    {
        private readonly IObservable<TSource> _source;
        private readonly TSource _defaultValue;

        public DefaultIfEmpty(IObservable<TSource> source, TSource defaultValue)
        {
            _source = source;
            _defaultValue = defaultValue;
        }

        protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(_defaultValue, observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : IdentitySink<TSource>
        {
            private readonly TSource _defaultValue;
            private bool _found;

            public _(TSource defaultValue, IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _defaultValue = defaultValue;
                _found = false;
            }

            public override void OnNext(TSource value)
            {
                _found = true;
                base.ForwardOnNext(value);
            }

            public override void OnCompleted()
            {
                if (!_found)
                    base.ForwardOnNext(_defaultValue);

                base.ForwardOnCompleted();
            }
        }
    }
}
