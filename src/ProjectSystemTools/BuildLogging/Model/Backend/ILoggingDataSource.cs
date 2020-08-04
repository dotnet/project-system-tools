using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.Backend
{
    public interface ILoggingDataSource
    {
        bool SupportsRoslynLogging { get; }
        void Start(Action notifyCallback);
        void Stop();
        void Clear();
        string GetLogForBuild(int buildID);
        ImmutableList<BuildSummary> GetAllBuilds();
    }
}
