﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public class UploaderProfile : Profile
    {
        public override string Name => "Uploader";

        private readonly int size = 1 * 1000 * 1024;
        public override async Task Action(CancellationToken cancellation)
        {
            try
            {
                var root = await base.GetTestFolderAsync().ConfigureAwait(false);

                var chars = "Test file content. ".ToCharArray();

                var buffer = new byte[size];
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = Convert.ToByte(chars[i % chars.Length]);

                var stream = new MemoryStream(buffer);
                
                var start = DateTime.Now;
                Log("> Uploading...");
                var content = await Content.UploadAsync(root.Id, Guid.NewGuid().ToString(), stream).ConfigureAwait(false);
                var duration = DateTime.Now - start;
                Log($"| Uploaded: {content.Id}. ({duration.TotalSeconds} sec)");

            }
            catch (Exception e)
            {
                Print(e);
            }

        }

    }
}
