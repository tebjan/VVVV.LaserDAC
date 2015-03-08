using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laser
{
    sealed class DisposeMember : IDisposable
    {
        readonly Action onDispose;

        public DisposeMember(Action onDispose = null)
        {
            this.onDispose = onDispose;
        }

        #region IDisposable Members

        bool disposed;

        public bool IsDisposed { get { return disposed; } }

        ~DisposeMember()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                    return;

                if (!disposed && onDispose != null)
                    onDispose();
            }
            finally
            {
                disposed = true;
            }
        }

        #endregion
    }
}
