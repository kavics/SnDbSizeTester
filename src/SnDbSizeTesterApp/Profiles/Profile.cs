using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp.Profiles
{
    public abstract class Profile
    {
        public bool Recurring { get; set; } = true;
        public int WaitMilliseconds { get; set; } = 1250;

        public abstract string Name { get; }
        public abstract Task Action(CancellationToken cancellation);

        internal Action<string> _printAction { get; set; }
        protected void Print(string text)
        {
            _printAction(text);
        }
    }
}
