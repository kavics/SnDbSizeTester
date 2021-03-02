using System;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public abstract class Profile
    {
        public bool Recurring { get; set; } = false;
        public int WaitMilliseconds { get; set; } = 5000;

        public abstract string Name { get; }
        public abstract Task Action(CancellationToken cancellation);

        internal Action<string> _printAction { get; set; }
        protected void Print(string text)
        {
            _printAction(text);
        }

        protected async Task<Content> GetTestFolderAsync()
        {
            var uploadRootPath = "/Root/Content/UploadTests";

            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root/Content", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }

            return uploadFolder;
        }
    }
}
