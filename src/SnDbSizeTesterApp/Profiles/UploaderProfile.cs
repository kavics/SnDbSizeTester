using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public class UploaderProfile : Profile
    {
        public override string Name => "Uploader";

        private readonly int size = 1 * 512 * 1024;
        public override async Task Action(CancellationToken cancellation)
        {
            Content root = await base.GetTestFolderAsync().ConfigureAwait(false);

            var chars = "Test file content. ".ToCharArray();

            var buffer = new byte[size];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Convert.ToByte(chars[i % chars.Length]);

            var stream = new MemoryStream(buffer);
            try
            {
                await Content.UploadAsync(root.Id, Guid.NewGuid().ToString(), stream).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Print(e.ToString());
            }

            Print("U");
        }

    }
}
