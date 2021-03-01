using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp.Profiles
{
    public class ApproverProfile : Profile
    {
        public override string Name => "Approver";
        public override Task Action(CancellationToken cancellation)
        {
            Print("A");
            return Task.CompletedTask;
        }
    }
}
