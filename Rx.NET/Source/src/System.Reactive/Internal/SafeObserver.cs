// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

namespace System.Reactive
{
    //
    // See AutoDetachObserver.cs for more information on the safeguarding requirement and
    // its implementation aspects.
    //

    internal sealed class SafeObserver<TSource> : Sink<TSource>, IObserver<TSource>
    {
        public static SafeObserver<TSource> Create(IObserver<TSource> observer)
        {
            return new SafeObserver<TSource>(observer);
        }

        private SafeObserver(IObserver<TSource> observer) : base(observer)
        {
        }

        public void OnNext(TSource value)
        {
            var __noError = false;
            try
            {
                _observer.OnNext(value);
                __noError = true;
            }
            finally
            {
                if (!__noError)
                {
                    Dispose();
                }
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                _observer.OnError(error);
            }
            finally
            {
                Dispose();
            }
        }

        public void OnCompleted()
        {
            try
            {
                _observer.OnCompleted();
            }
            finally
            {
                Dispose();
            }
        }
    }
}
