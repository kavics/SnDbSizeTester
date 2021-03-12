using System;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnDbSizeTesterApp.Profiles
{
    public class OrganizerProfile : Profile
    {
        public override string Name => "Organizer";

        public override async Task Action(CancellationToken cancellation)
        {
            try
            {
                var source = await EnsureStructure().ConfigureAwait(false);
                var parentPath = source.ParentPath;
                var targetSuffix = parentPath.EndsWith("1") ? "2" : "1";
                var targetPath = parentPath.Substring(0, parentPath.Length - 1) + targetSuffix;
                var target = await Content.LoadAsync(targetPath).ConfigureAwait(false);

                Log($"> Moving: {source.Path} to {target.Path}");
                await MoveAsync(new[] {source}, target);
                Log($"| Moved: {source.Path} to {target.Path}");
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private async Task<Content> EnsureStructure()
        {
            var root = await GetTestFolderAsync();
            var thisName = $"{Name}-{Id}";
            var source =
                await Content.LoadAsync($"{root.Path}/{thisName}/Target-1/Source").ConfigureAwait(false) ??
                await Content.LoadAsync($"{root.Path}/{thisName}/Target-2/Source").ConfigureAwait(false);
            if (source != null)
                return source;

            var profileRoot = await EnsureFolder(root.Path, thisName);
            var target1 = await EnsureFolder(profileRoot.Path, "Target-1");
            var target2 = await EnsureFolder(profileRoot.Path, "Target-2");
            source = await EnsureFolder(target1.Path, "Source");

            var tasks = new Task<Content>[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                if (i > 0)
                    await Task.Delay(250); // avoid exceeding the request/sec limitation.
                tasks[i] = UploaderProfile.UploadAsync(source.Id, "File-" + (i + 1));
            }
            await Task.WhenAll(tasks);

            return source;
        }

        private async Task<Content> EnsureFolder(string parentPath, string name)
        {
            var path = parentPath + "/" + name;
            var content = await Content.LoadAsync(path).ConfigureAwait(false);
            if (content == null)
            {
                content = Content.CreateNew(parentPath, "Folder", name);
                await content.SaveAsync().ConfigureAwait(false);
            }
            return content;
        }


        private async Task<Content> CreateFolderAsync()
        {
            var folder = Content.CreateNew("/Root/Content/UploadTests", "Folder", Guid.NewGuid().ToString());
            await folder.SaveAsync().ConfigureAwait(false);
            return folder;
        }

        private async Task MoveAsync(Content[] sources, Content target)
        {
            //var paths = string.Join(", ", sources.Select(x => "\"\"" + x.Path + "\"\""));
            var paths = string.Join(", ", sources.Select(x => x.Id.ToString()));
            var body = @$"models=[{{""targetPath"": ""{target.Path}"",
                        ""paths"": [{paths}]}}]";
            var result = await RESTCaller.GetResponseStringAsync(
                "/Root", "MoveBatch", HttpMethod.Post, body);
            //Console.WriteLine(result);
        }
    }
}
