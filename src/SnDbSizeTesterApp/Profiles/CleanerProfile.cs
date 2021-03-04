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
                var query = $"InFolder:'{root.Path}' .AUTOFILTERS:OFF .SORT:Id .TOP:10";
                var select = new[] {"Id", "ParentId", "Path", "Name"};
                var result = await Content.QueryAsync(query, select).ConfigureAwait(false);
                var ids = result.Select(x => x.Id).ToArray();
                await Content.DeleteAsync(ids, true, cancellation);
                Print("C");
            }
            catch (Exception e)
            {
                Print(e);
            }

        }
    }
}
