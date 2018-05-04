// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Disposables;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class Amb<TSource> : Producer<TSource, Amb<TSource>._>
    {
        private readonly IObservable<TSource> _left;
        private readonly IObservable<TSource> _right;

        public Amb(IObservable<TSource> left, IObservable<TSource> right)
        {
            _left = left;
            _right = right;
        }

        protected override _ CreateSink(IObserver<TSource> observer, IDisposable cancel) => new _(observer, cancel);

        protected override IDisposable Run(_ sink) => sink.Run(this);

        internal sealed class _ : Sink<TSource>, IObserver<TSource>
        {
            public _(IObserver<TSource> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            private AmbState _choice;

            public void OnCompleted()
            {
                ForwardOnCompleted();
            }

            public void OnError(Exception error)
            {
                ForwardOnError(error);
            }

            public void OnNext(TSource value)
            {
                ForwardOnNext(value);
            }

            public IDisposable Run(Amb<TSource> parent)
            {
                var ls = new SingleAssignmentDisposable();
                var rs = new SingleAssignmentDisposable();
                var d = StableCompositeDisposable.Create(ls, rs);

                var gate = new object();

                var lo = new AmbObserver(d, this, gate, AmbState.Left, rs);
                var ro = new AmbObserver(d, this, gate, AmbState.Right, ls);

                _choice = AmbState.Neither;

                ls.Disposable = parent._left.SubscribeSafe(lo);
                rs.Disposable = parent._right.SubscribeSafe(ro);

                return d;
            }

            private sealed class AmbObserver : IObserver<TSource>
            {
                private sealed class DecisionObserver : IObserver<TSource>
                {
                    private readonly _ _parent;
                    private readonly AmbState _me;
                    private readonly IDisposable _otherSubscription;
                    private readonly object _gate;
                    private readonly AmbObserver _observer;

                    public DecisionObserver(_ parent, object gate, AmbState me, IDisposable otherSubscription, AmbObserver observer)
                    {
                        _parent = parent;
                        _gate = gate;
                        _me = me;
                        _otherSubscription = otherSubscription;
                        _observer = observer;
                    }

                    public void OnNext(TSource value)
                    {
                        lock (_gate)
                        {
                            if (_parent._choice == AmbState.Neither)
                            {
                                _parent._choice = _me;
                                _otherSubscription.Dispose();
                                _observer._target = _parent;
                            }

                            if (_parent._choice == _me)
                            {
                                _parent.ForwardOnNext(value);
                            }
                        }
                    }

                    public void OnError(Exception error)
                    {
                        lock (_gate)
                        {
                            if (_parent._choice == AmbState.Neither)
                            {
                                _parent._choice = _me;
                                _otherSubscription.Dispose();
                                _observer._target = _parent;
                            }

                            if (_parent._choice == _me)
                            {
                                _parent.ForwardOnError(error);
                            }
                        }
                    }

                    public void OnCompleted()
                    {
                        lock (_gate)
                        {
                            if (_parent._choice == AmbState.Neither)
                            {
                                _parent._choice = _me;
                                _otherSubscription.Dispose();
                                _observer._target = _parent;
                            }

                            if (_parent._choice == _me)
                            {
                                _parent.ForwardOnCompleted();
                            }
                        }
                    }
                }

                private IObserver<TSource> _target;
                private readonly IDisposable _disposable;

                public AmbObserver(IDisposable disposable, _ parent, object gate, AmbState me, IDisposable otherSubscription)
                {
                    _disposable = disposable;
                    _target = new DecisionObserver(parent, gate, me, otherSubscription, this);
                }

                public void OnNext(TSource value)
                {
                    _target.OnNext(value);
                }

                public void OnError(Exception error)
                {
                    _target.OnError(error);
                    _disposable.Dispose();
                }

                public void OnCompleted()
                {
                    _target.OnCompleted();
                    _disposable.Dispose();
                }
            }

            private enum AmbState
            {
                Left,
                Right,
                Neither,
            }
        }
    }
}
