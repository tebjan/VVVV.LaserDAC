using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laser
{
    public sealed class ReferenceCounter
    {
        readonly Action onSuspended;
        readonly Action onResumed;
        uint counter;

        public ReferenceCounter(Action onResumed = null, Action onSuspended = null)
        {
            this.onResumed = onResumed;
            this.onSuspended = onSuspended;
        }

        public bool IsReferenced { get { return counter > 0; } }

        public IDisposable Suspend()
        {
            OneSuspended();
            return new DisposeMember(OneDisposed);
        }

        void OneSuspended()
        {
            bool first = counter == 0;
            counter++;

            if (first && onSuspended != null)
                onSuspended();
        }

        void OneDisposed()
        {
            if (counter == 0)
                throw new InvalidOperationException("Reference counter can not be already zero");

            counter--;
            if (counter == 0 && onResumed != null)
                onResumed();
        }
    }
}
