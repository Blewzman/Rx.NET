﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static IAsyncEnumerable<T> CreateEnumerable<T>(Func<IAsyncEnumerator<T>> getEnumerator)
        {
            return new AnonymousAsyncEnumerable<T>(getEnumerator);
        }

        public static IAsyncEnumerator<T> CreateEnumerator<T>(Func<CancellationToken, Task<bool>> moveNext, Func<T> current, Action dispose)
        {
            return new AnonymousAsyncIterator<T>(moveNext, current, dispose);
        }

        private static IAsyncEnumerator<T> CreateEnumerator<T>(Func<CancellationToken, TaskCompletionSource<bool>, Task<bool>> moveNext, Func<T> current, Action dispose)
        {
            var self = default(IAsyncEnumerator<T>);
            self = new AnonymousAsyncIterator<T>(
                async ct =>
                {
                    var tcs = new TaskCompletionSource<bool>();

                    var stop = new Action(
                        () =>
                        {
                            tcs.TrySetCanceled();
                        });

                    using (ct.Register(stop))
                    {
                        return await moveNext(ct, tcs)
                                   .ConfigureAwait(false);
                    }
                },
                current,
                dispose
            );
            return self;
        }

        

        private class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly Func<IAsyncEnumerator<T>> getEnumerator;

            public AnonymousAsyncEnumerable(Func<IAsyncEnumerator<T>> getEnumerator)
            {
                this.getEnumerator = getEnumerator;
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                return getEnumerator();
            }
        }

        private sealed class AnonymousAsyncIterator<T> : AsyncIterator<T>
        {
            private readonly Func<T> currentFunc;
            private readonly Action dispose;
            private readonly Func<CancellationToken, Task<bool>> moveNext;


            public AnonymousAsyncIterator(Func<CancellationToken, Task<bool>> moveNext, Func<T> currentFunc, Action dispose)
            {
                this.moveNext = moveNext;
                this.currentFunc = currentFunc;
                this.dispose = dispose;

                // Explicit call to initialize enumerator mode
                GetEnumerator();
            }

            public override AsyncIterator<T> Clone()
            {
                throw new NotSupportedException("Iterator only");
            }

            public override void Dispose()
            {
                dispose?.Invoke();

                base.Dispose();
            }

            protected override async Task<bool> MoveNextCore(CancellationToken cancellationToken)
            {
                if (await moveNext(cancellationToken).ConfigureAwait(false))
                {
                    current = currentFunc();
                    return true;
                }

                return false;
            }

            protected override Task Initialize(CancellationToken cancellationToken)
            {
                return TaskExt.True;
            }
        }
    }
}