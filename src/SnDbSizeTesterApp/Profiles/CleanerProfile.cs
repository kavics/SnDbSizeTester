using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp.Profiles
{
    public class CleanerProfile : Profile
    {
        public override string Name => "Cleaner";
        public override Task Action(CancellationToken cancellation)
        {
            Print("C");
            return Task.CompletedTask;
        }
    }
}
