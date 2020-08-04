// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

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

        private Action NotifyUI { get; set; }

        public BackEndBuildTableDataSource()
        {
            _evaluationLogger = new EvaluationLogger(this);
            _roslynLogger = new RoslynLogger(this);
        }

        public void Start(Action notifyCallback)
        {
            NotifyUI = notifyCallback;

            IsLogging = true;
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
            foreach (var build in _entries)
            {
                build.Dispose();
            }
            _entries = ImmutableList<Build>.Empty;
        }

        public ILogger CreateLogger(bool isDesignTime) => new ProjectLogger(this, isDesignTime);

        /// <summary>
        /// return log path on server for a given build
        /// </summary>
        /// <param name="buildId">ID to return build for</param>
        /// <returns> returns filepath to log path (on server)
        /// returns null if no match is found.</returns>
        public string GetLogForBuild(int buildId)
        {
            return _entries.Find(x => x.BuildId == buildId).LogPath;
        }

        ImmutableList<BuildSummary> ILoggingDataSource.GetAllBuilds()
        {
            return _entries.Select(build => build.BuildSummary).ToImmutableList();
        }

        public void NotifyChange()
        {
            NotifyUI?.Invoke();
        }

        public void AddEntry(Build build)
        {
            _entries = _entries.Add(build);
            NotifyChange();
        }
    }
}
