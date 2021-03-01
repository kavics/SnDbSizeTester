using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public class CleanerProfile : Profile
    {
        public override string Name => "Cleaner";

        public override async Task Action(CancellationToken cancellation)
        {
            try
            {
                var root = await base.GetTestFolderAsync().ConfigureAwait(false);
                var query = $"InFolder:'{root.Path}' .AUTOFILTERS:OFF .SORT:Id .TOP:1";
                var result = await Content.QueryAsync(query).ConfigureAwait(false);
                var content = result.FirstOrDefault();
                if (content != null)
                    await Content.DeleteAsync(content.Id, true, cancellation);
                Print("C");
            }
            catch (Exception e)
            {
                Print(e.ToString());
            }

        }
    }
}
