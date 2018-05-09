// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace System.Reactive
{
    /// <summary>
    /// Interface with variance annotation; allows for better type checking when detecting capabilities in SubscribeSafe.
    /// </summary>
    /// <typeparam name="TSource">Type of the resulting sequence's elements.</typeparam>
    internal interface IProducer<out TSource> : IObservable<TSource>
    {
        IDisposable SubscribeRaw(IObserver<TSource> observer, bool enableSafeguard);
    }

    /// <summary>
    /// Base class for implementation of query operators, providing performance benefits over the use of Observable.Create.
    /// </summary>
    /// <typeparam name="TSource">Type of the resulting sequence's elements.</typeparam>
    internal abstract class BasicProducer<TSource> : IProducer<TSource>
    {
        /// <summary>
        /// Publicly visible Subscribe method.
        /// </summary>
        /// <param name="observer">Observer to send notifications on. The implementation of a producer must ensure the correct message grammar on the observer.</param>
        /// <returns>IDisposable to cancel the subscription. This causes the underlying sink to be notified of unsubscription, causing it to prevent further messages from being sent to the observer.</returns>
        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            return SubscribeRaw(observer, enableSafeguard: true);
        }

        public IDisposable SubscribeRaw(IObserver<TSource> observer, bool enableSafeguard)
        {
            var subscription = new SingleAssignmentDisposable();

            //
            // See AutoDetachObserver.cs for more information on the safeguarding requirement and
            // its implementation aspects.
            //
            if (enableSafeguard)
            {
                observer = SafeObserver<TSource>.Create(observer, subscription);
            }

            if (CurrentThreadScheduler.IsScheduleRequired)
            {
                var state = new State { parent = this, subscription = subscription, observer = observer };
                CurrentThreadScheduler.Instance.Schedule(state, RunRun);
            }
            else
            {
                subscription.Disposable = Run(observer);
            }

            return subscription;
        }

        private struct State
        {
            public BasicProducer<TSource> parent;
            public SingleAssignmentDisposable subscription;
            public IObserver<TSource> observer;
        }

        static IDisposable RunRun(IScheduler _, State x)
        {
            x.subscription.Disposable = x.parent.Run(x.observer);
            return Disposable.Empty;
        }

        /// <summary>
        /// Core implementation of the query operator, called upon a new subscription to the producer object.
        /// </summary>
        /// <param name="observer">Observer to send notifications on. The implementation of a producer must ensure the correct message grammar on the observer.</param>
        /// <returns>Disposable representing all the resources and/or subscriptions the operator uses to process events.</returns>
        /// <remarks>The <paramref name="observer">observer</paramref> passed in to this method is not protected using auto-detach behavior upon an OnError or OnCompleted call. The implementation must ensure proper resource disposal and enforce the message grammar.</remarks>
        protected abstract IDisposable Run(IObserver<TSource> observer);
    }

    internal abstract class Producer<TTarget, TSink> : IProducer<TTarget>
        where TSink : IDisposable
    {
        /// <summary>
        /// Publicly visible Subscribe method.
        /// </summary>
        /// <param name="observer">Observer to send notifications on. The implementation of a producer must ensure the correct message grammar on the observer.</param>
        /// <returns>IDisposable to cancel the subscription. This causes the underlying sink to be notified of unsubscription, causing it to prevent further messages from being sent to the observer.</returns>
        public IDisposable Subscribe(IObserver<TTarget> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            return SubscribeRaw(observer, enableSafeguard: true);
        }

        public IDisposable SubscribeRaw(IObserver<TTarget> observer, bool enableSafeguard)
        {
            //This is passed to Sink creation and later assigned with the outcome of Run,
            //so this allocation is (still) inevitable.
            var runDisposable = new SingleAssignmentDisposable();

            //This will only ever be allocated if we need to put a safeguard around the observer.
            //or if the created Sink does not inherit from the internal Sink class (see below).
            BinarySingleAssignmentDisposable safeObserverDisposable = null;

            //
            // See AutoDetachObserver.cs for more information on the safeguarding requirement and
            // its implementation aspects.
            //
            if (enableSafeguard)
            {
                safeObserverDisposable = new BinarySingleAssignmentDisposable();
                observer = SafeObserver<TTarget>.Create(observer, safeObserverDisposable);
            }

            var sink = CreateSink(observer, runDisposable);
            var sinkIsInternal = sink is Sink<TTarget>;

            if (!sinkIsInternal && safeObserverDisposable == null)
            {
                //If sink inherits from the internal class Sink<TTarget>, we know that disposing it
                //will dispose the runDisposable passed in during Sink creation. Is is therefore
                //sufficient, to continue working with sink. If not, we have to combine the sink and the run.
                //Again, on the hot path, this allocation is avoided.
                safeObserverDisposable = new BinarySingleAssignmentDisposable();
            }

            if (safeObserverDisposable != null)
            {
                //So we either needed to enable a safeguard or our Sink is some obscure type
                //we don't know too much about...
                safeObserverDisposable.Disposable1 = sink;

                //...if the latter is true, also set the run.
                if (!sinkIsInternal)
                    safeObserverDisposable.Disposable2 = runDisposable;
            }
            
            if (CurrentThreadScheduler.IsScheduleRequired)
            {
                var state = new State { parent = this, sink = sink, inner = runDisposable };

                CurrentThreadScheduler.Instance.Schedule(state, RunRun);
            }
            else
            {
                runDisposable.Disposable = Run(sink);
            }

            //On the hot path, returning sink will suffice. This will save one allocation.
            return safeObserverDisposable ?? (IDisposable)sink;
        }

        private struct State
        {
            public Producer<TTarget, TSink> parent;
            public TSink sink;
            public SingleAssignmentDisposable inner;
        }

        static IDisposable RunRun(IScheduler _, State x)
        {
            x.inner.Disposable = x.parent.Run(x.sink);
            return Disposable.Empty;
        }

        /// <summary>
        /// Core implementation of the query operator, called upon a new subscription to the producer object.
        /// </summary>
        /// <param name="sink">The sink object.</param>
        protected abstract IDisposable Run(TSink sink);

        protected abstract TSink CreateSink(IObserver<TTarget> observer, IDisposable cancel);
    }
}
