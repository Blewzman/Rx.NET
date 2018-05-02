// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive
{
    /// <summary>
    /// Base class for implementation of query operators, providing a lightweight sink that can be disposed to mute the outgoing observer.
    /// </summary>
    /// <typeparam name="TSource">Type of the resulting sequence's elements.</typeparam>
    /// <remarks>Implementations of sinks are responsible to enforce the message grammar on the associated observer. Upon sending a terminal message, a pairing Dispose call should be made to trigger cancellation of related resources and to mute the outgoing observer.</remarks>
    internal abstract class Sink<TSource> : IDisposable
    {
        protected internal volatile IObserver<TSource> _observer;
        private IDisposable _cancel;

        public Sink(IObserver<TSource> observer)
        {
            _observer = observer;
        }

        public Sink(IObserver<TSource> observer, IDisposable cancel)
        {
            _observer = observer;
            // cancel is ignored because Sink is the new self-contained cancellable component
            // Producer.CreateSink wraps or provides the upstream as a Sink<T> anyway
            // Set via OnSubscribe()
        }

        internal virtual bool OnSubscribe(IDisposable upstream)
        {
            if (Interlocked.CompareExchange(ref _cancel, upstream, null) != null)
            {
                upstream?.Dispose();
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _observer = NopObserver<TSource>.Instance;

            var d = Interlocked.Exchange(ref _cancel, BooleanDisposable.True);
            if (d != BooleanDisposable.True)
            {
                d?.Dispose();
            }
        }

        internal bool IsDisposed()
        {
            return Volatile.Read(ref _cancel) == BooleanDisposable.True;
        }

        protected void ForwardOnNext(TSource value)
        {
            _observer.OnNext(value);
        }

        protected void ForwardOnCompleted()
        {
            _observer.OnCompleted();
            Dispose();
        }

        protected void ForwardOnError(Exception error)
        {
            _observer.OnError(error);
            Dispose();
        }

        public IObserver<TSource> GetForwarder() => new _(this);

        private sealed class _ : IObserver<TSource>
        {
            private readonly Sink<TSource> _forward;

            public _(Sink<TSource> forward)
            {
                _forward = forward;
            }

            public void OnNext(TSource value)
            {
                _forward.ForwardOnNext(value);
            }

            public void OnError(Exception error)
            {
                _forward.ForwardOnError(error);
            }

            public void OnCompleted()
            {
                _forward.ForwardOnCompleted();
            }
        }
    }
}
