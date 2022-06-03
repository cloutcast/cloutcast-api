using System;

namespace CloutCast
{
    public abstract class Disposable : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsDisposed() => Disposed;

        protected bool Disposed { get; private set; }
        protected virtual void Dispose(bool disposing) => Disposed = true;

        ~Disposable() => Dispose(false);
    }
}