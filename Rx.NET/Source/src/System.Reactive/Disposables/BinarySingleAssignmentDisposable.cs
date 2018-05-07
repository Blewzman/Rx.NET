namespace System.Reactive.Disposables
{
    /// <summary>
    /// Represents a disposable resource which only allows a single assignment of each of its underlying disposable resources.
    /// If an underlying disposable resource has already been set, future attempts to set one of the underlying disposable resources will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    public sealed class BinarySingleAssignmentDisposable : AssignmentDisposable, ICancelable
    {
        private volatile IDisposable _current1;
        private volatile IDisposable _current2;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySingleAssignmentDisposable"/> class.
        /// </summary>
        public BinarySingleAssignmentDisposable()
        {
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed => base.GetIsDisposed(ref _current1) && base.GetIsDisposed(ref _current2);

        /// <summary>
        /// Gets or sets the first underlying disposable. After disposal, the result of getting this property is undefined.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the first disposable of the <see cref="BinarySingleAssignmentDisposable"/> has already been assigned to.</exception>
        public IDisposable Disposable1
        {
            get => this.Get(ref _current1);
            set => this.Set(ref _current1, value);
        }

        /// <summary>
        /// Gets or sets the second underlying disposable. After disposal, the result of getting this property is undefined.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the second disposable <see cref="BinarySingleAssignmentDisposable"/> has already been assigned to.</exception>
        public IDisposable Disposable2
        {
            get => this.Get(ref _current2);
            set => this.Set(ref _current2, value);
        }

        /// <summary>
        /// Disposes the underlying disposables.
        /// </summary>
        public void Dispose()
        {
            base.Dispose(ref _current1);
            base.Dispose(ref _current2);
        }
    }
}