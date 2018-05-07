using System.Threading;

namespace System.Reactive.Disposables
{
    public abstract class AssignmentDisposable
    {
        /// <summary>
        /// Gets or sets the underlying disposable. After disposal, the result of getting this property is undefined.
        /// </summary>
        protected IDisposable Get(ref IDisposable fieldRef)
        {
            var current = fieldRef;

            if (current == BooleanDisposable.True)
            {
                return DefaultDisposable.Instance; // Don't leak the sentinel value.
            }

            return current;
        }

        /// <summary>
        /// Gets or sets the underlying disposable. After disposal, the result of getting this property is undefined.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the resource has already been assigned to.</exception>
        protected void Set(ref IDisposable fieldRef, IDisposable value)
        {
            var old = Interlocked.CompareExchange(ref fieldRef, value, null);
            if (old == null)
                return;

            if (old != BooleanDisposable.True)
                throw new InvalidOperationException(Strings_Core.DISPOSABLE_ALREADY_ASSIGNED);

            value?.Dispose();
        }

        protected bool GetIsDisposed(ref IDisposable fieldRef)
        {
            // We use a sentinel value to indicate we've been disposed. This sentinel never leaks
            // to the outside world (see the Disposable property getter), so no-one can ever assign
            // this value to us manually.
            return fieldRef == BooleanDisposable.True;
        }

        protected void Dispose(ref IDisposable fieldRef)
        {
            Interlocked.Exchange(ref fieldRef, BooleanDisposable.True)?.Dispose();
        }
    }
}