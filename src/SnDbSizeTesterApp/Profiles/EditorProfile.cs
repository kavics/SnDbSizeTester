﻿using System.Threading;
using System.Threading.Tasks;

namespace SnDbSizeTesterApp.Profiles
{
    public class EditorProfile : Profile
    {
        public override string Name => "Editor";
        public override Task Action(CancellationToken cancellation)
        {
            Print("E");
            return Task.CompletedTask;
        }
    }
}
