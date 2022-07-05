// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.BackEnd
{
    [Export(typeof(ILoggingController))]
    [Export(typeof(ILoggingDataSource))]
    internal sealed class BackEndBuildTableDataSource : ILoggingController, ILoggingDataSource
    {
        private readonly EvaluationLogger _evaluationLogger;
        private readonly RoslynLogger _roslynLogger;

        private ImmutableList<Build> _entries = ImmutableList<Build>.Empty;

        public bool IsLogging { get; private set; }

        public bool SupportsRoslynLogging => _roslynLogger.Supported;

        private Action? NotifyUI { get; set; }

        public BackEndBuildTableDataSource()
        {
            _evaluationLogger = new EvaluationLogger(this);
            _roslynLogger = new RoslynLogger(this);
        }

        public void Start(Action notifyCallback)
        {
            NotifyUI = notifyCallback;

            IsLogging = true;

            // CPS projects are not present in the global project collection.
            // Legacy CSPROJ projects are present.
            ProjectCollection.GlobalProjectCollection.RegisterLogger(_evaluationLogger);

            _roslynLogger.Start();
        }

        public void Stop()
        {
            NotifyUI = null;

            IsLogging = false;
            ProjectCollection.GlobalProjectCollection.UnregisterAllLoggers();
            _roslynLogger.Stop();
        }

        public void Clear()
        {
            var entries = Interlocked.Exchange(ref _entries, ImmutableList<Build>.Empty);

            foreach (var build in entries)
            {
                build.Dispose();
            }
        }

        public ILogger CreateLogger(bool isDesignTime) => new ProjectLogger(this, isDesignTime);

        public string? GetLogForBuild(int buildId)
        {
            return _entries.Find(x => x.BuildId == buildId)?.LogPath;
        }

        ImmutableArray<BuildSummary> ILoggingDataSource.GetAllBuilds()
        {
            var entries = _entries; // snapshot value to prevent exceptions
            var builder = ImmutableArray.CreateBuilder<BuildSummary>(initialCapacity: entries.Count);
            
            foreach (Build entry in entries)
            {
                builder.Add(entry.BuildSummary);
            }

            return builder.MoveToImmutable();
        }

        public void NotifyChange()
        {
            NotifyUI?.Invoke();
        }

        public void AddEntry(Build build)
        {
            ImmutableInterlocked.Update(
                ref _entries,
                static (entries, build) => entries.Add(build),
                build);

            NotifyChange();
        }
    }
}
