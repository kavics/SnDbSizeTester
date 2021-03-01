using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp.Profiles
{
    public class UploaderProfile : Profile
    {
        public override string Name => "Uploader";
        public override Task Action(CancellationToken cancellation)
        {
            Print("U");
            return Task.CompletedTask;
        }

    }
}
