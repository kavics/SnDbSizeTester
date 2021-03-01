﻿using System;
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

private bool _startup = true;
        protected async Task<Content> GetTestFolderAsync()
        {
            var uploadRootPath = "/Root/UploadTests";

            var uploadFolder = await Content.LoadAsync(uploadRootPath).ConfigureAwait(false);
if (uploadFolder != null && _startup)
{
    await uploadFolder.DeleteAsync();
    uploadFolder = null;
    _startup = false;
}
            if (uploadFolder == null)
            {
                uploadFolder = Content.CreateNew("/Root", "SystemFolder", "UploadTests");
                await uploadFolder.SaveAsync().ConfigureAwait(false);
            }


            return uploadFolder;
        }
    }
}
