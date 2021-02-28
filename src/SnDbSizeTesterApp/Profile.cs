using System;
using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp
{
    public class Profile
    {
        public string Name { get; set; }
        public bool Recurring { get; set; }
        public int WaitMilliseconds { get; set; }
        public Func<CancellationToken, Task> Action { get; set; }
    }
}
