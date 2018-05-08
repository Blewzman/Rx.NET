// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive.Linq.ObservableImpl
{
    internal abstract class SumSink<T> : IdentitySink<T>
    {
        protected T _sum;

        protected SumSink(IObserver<T> observer, IDisposable cancel)
            : base(observer, cancel)
        {
        }

        public override void OnCompleted()
        {
            base.ForwardOnNext(_sum);
            base.ForwardOnCompleted();
        }
    }

    internal sealed class SumDouble : Producer<double, SumDouble._>
    {
        private readonly IObservable<double> _source;

        public SumDouble(IObservable<double> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<double> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<double>
        {
            public _(IObserver<double> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(double value)
            {
                _sum += value;
            }
        }
    }

    internal sealed class SumSingle : Producer<float, SumSingle._>
    {
        private readonly IObservable<float> _source;

        public SumSingle(IObservable<float> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<float> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<float>
        {
            public _(IObserver<float> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(float value)
            {
                _sum += value; // This is what LINQ to Objects does!
            }
        }
    }

    internal sealed class SumDecimal : Producer<decimal, SumDecimal._>
    {
        private readonly IObservable<decimal> _source;

        public SumDecimal(IObservable<decimal> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<decimal> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<decimal>
        {
            public _(IObserver<decimal> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(decimal value)
            {
                _sum += value;
            }
        }
    }

    internal sealed class SumInt32 : Producer<int, SumInt32._>
    {
        private readonly IObservable<int> _source;

        public SumInt32(IObservable<int> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<int> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<int>
        {
            public _(IObserver<int> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(int value)
            {
                try
                {
                    checked
                    {
                        _sum += value;
                    }
                }
                catch (Exception exception)
                {
                    base.ForwardOnError(exception);
                }
            }
        }
    }

    internal sealed class SumInt64 : Producer<long, SumInt64._>
    {
        private readonly IObservable<long> _source;

        public SumInt64(IObservable<long> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<long> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<long>
        {
            public _(IObserver<long> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(long value)
            {
                try
                {
                    checked
                    {
                        _sum += value;
                    }
                }
                catch (Exception exception)
                {
                    base.ForwardOnError(exception);
                }
            }
        }
    }

    internal sealed class SumDoubleNullable : Producer<double?, SumDoubleNullable._>
    {
        private readonly IObservable<double?> _source;

        public SumDoubleNullable(IObservable<double?> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<double?> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<double?>
        {
            public _(IObserver<double?> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _sum = 0.0;
            }

            public override void OnNext(double? value)
            {
                if (value != null)
                    _sum += value.Value;
            }
        }
    }

    internal sealed class SumSingleNullable : Producer<float?, SumSingleNullable._>
    {
        private readonly IObservable<float?> _source;

        public SumSingleNullable(IObservable<float?> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<float?> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : IdentitySink<float?>
        {
            private double _sum; // This is what LINQ to Objects does!

            public _(IObserver<float?> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _sum = 0.0; // This is what LINQ to Objects does!
            }

            public override void OnNext(float? value)
            {
                if (value != null)
                    _sum += value.Value; // This is what LINQ to Objects does!
            }

            public override void OnCompleted()
            {
                base.ForwardOnNext((float) _sum); // This is what LINQ to Objects does!
                base.ForwardOnCompleted();
            }
        }
    }
    
    internal sealed class SumDecimalNullable : Producer<decimal?, SumDecimalNullable._>
    {
        private readonly IObservable<decimal?> _source;

        public SumDecimalNullable(IObservable<decimal?> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<decimal?> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<decimal?>
        {
            public _(IObserver<decimal?> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _sum = 0M;
            }

            public override void OnNext(decimal? value)
            {
                if (value != null)
                    _sum += value.Value;
            }
        }
    }

    internal sealed class SumInt32Nullable : Producer<int?, SumInt32Nullable._>
    {
        private readonly IObservable<int?> _source;

        public SumInt32Nullable(IObservable<int?> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<int?> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<int?>
        {
            public _(IObserver<int?> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _sum = 0;
            }

            public override void OnNext(int? value)
            {
                try
                {
                    checked
                    {
                        if (value != null)
                            _sum += value.Value;
                    }
                }
                catch (Exception exception)
                {
                    base.ForwardOnError(exception);
                }
            }
        }
    }

    internal sealed class SumInt64Nullable : Producer<long?, SumInt64Nullable._>
    {
        private readonly IObservable<long?> _source;

        public SumInt64Nullable(IObservable<long?> source)
        {
            _source = source;
        }

        protected override _ CreateSink(IObserver<long?> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => _source.SubscribeSafe(sink);

        internal sealed class _ : SumSink<long?>
        {
            public _(IObserver<long?> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _sum = 0L;
            }

            public override void OnNext(long? value)
            {
                try
                {
                    checked
                    {
                        if (value != null)
                            _sum += value.Value;
                    }
                }
                catch (Exception exception)
                {
                    base.ForwardOnError(exception);
                }
            }
        }
    }
}
