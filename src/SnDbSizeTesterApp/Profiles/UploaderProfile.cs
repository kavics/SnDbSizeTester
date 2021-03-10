using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public class UploaderProfile : Profile
    {
        public override string Name => "Uploader";

        private static readonly int _sharedBufferSize = 1 * 1000 * 1024;
        private static byte[] _sharedBuffer;
        static UploaderProfile()
        {
            var chars = "Test file content. ".ToCharArray();
            var buffer = new byte[_sharedBufferSize];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Convert.ToByte(chars[i % chars.Length]);
            _sharedBuffer = buffer;
        }

        public override async Task Action(CancellationToken cancellation)
        {
            try
            {
                var root = await base.GetTestFolderAsync().ConfigureAwait(false);

                
                var start = DateTime.Now;
                Log("> Uploading...");
                
                //var stream = new MemoryStream(_sharedBuffer);
                //var content = await Content.UploadAsync(root.Id, Guid.NewGuid().ToString(), stream).ConfigureAwait(false);
                var content = await UploadAsync(root.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);

                var duration = DateTime.Now - start;
                Log($"| Uploaded: {content.Id}. ({duration.TotalSeconds} sec)");
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        public static async Task<Content> UploadAsync(int parentId, string name)
        {
            var stream = new MemoryStream(_sharedBuffer);
            var content = await Content.UploadAsync(parentId, name, stream).ConfigureAwait(false);
            return content;
        }
    }
}
