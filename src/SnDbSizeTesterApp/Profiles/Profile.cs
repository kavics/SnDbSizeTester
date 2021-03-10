using System;
using System.Text;
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
        public int Id { get; set; }
        public abstract Task Action(CancellationToken cancellation);

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

        internal Action<string> _logAction { get; set; }
        protected void Log(string text)
        {
            _logAction(text);
        }
        internal Action<Exception> _logErrorAction { get; set; }
        protected void LogError(Exception error)
        {
            _logErrorAction(error);
        }
        protected void Print(Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine(e.ToString());
            while ((e = e.InnerException) != null)
            {
                sb.AppendLine("-------------- InnerException:");
                sb.AppendLine(e.ToString());
            }
            sb.AppendLine("==============================");
            Log(sb.ToString());
        }

    }
}
