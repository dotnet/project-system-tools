// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.BuildLogging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    [Export(typeof(IBuildLoggerProviderAsync))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    [Export(typeof(IVsBuildLoggerProvider))]
    internal sealed class BuildLoggerProvider : IBuildLoggerProviderAsync, IVsBuildLoggerProvider
    {
        /// <summary>
        /// The set of build logging events we want to be produced during build.
        /// This value is reported via <see cref="IVsBuildLoggerProvider.Events"/>,
        /// but only when <see cref="ILoggingController.IsLogging"/> is <see langword="true"/>.
        /// </summary>
        private const BuildLoggerEvents EventsWhenLogging =
            BuildLoggerEvents.BuildStartedEvent |
            BuildLoggerEvents.BuildFinishedEvent |
            BuildLoggerEvents.ErrorEvent |
            BuildLoggerEvents.WarningEvent |
            BuildLoggerEvents.HighMessageEvent |
            BuildLoggerEvents.NormalMessageEvent |
            BuildLoggerEvents.ProjectStartedEvent |
            BuildLoggerEvents.ProjectFinishedEvent |
            BuildLoggerEvents.TargetStartedEvent |
            BuildLoggerEvents.TargetFinishedEvent |
            BuildLoggerEvents.CommandLine |
            BuildLoggerEvents.TaskStartedEvent |
            BuildLoggerEvents.TaskFinishedEvent |
            BuildLoggerEvents.LowMessageEvent |
            BuildLoggerEvents.ProjectEvaluationStartedEvent |
            BuildLoggerEvents.ProjectEvaluationFinishedEvent |
            BuildLoggerEvents.CustomEvent;

        private readonly ILoggingController _loggingController;

        [ImportingConstructor]
        public BuildLoggerProvider(ILoggingController loggingController)
        {
            _loggingController = loggingController;
        }

        public LoggerVerbosity Verbosity => _loggingController.IsLogging ? LoggerVerbosity.Diagnostic : LoggerVerbosity.Quiet;

        public BuildLoggerEvents Events => _loggingController.IsLogging ? EventsWhenLogging : BuildLoggerEvents.None;

        public ILogger? GetLogger(string projectPath, IEnumerable<string> targets, IDictionary<string, string> properties, bool isDesignTimeBuild) => 
            _loggingController.IsLogging ? _loggingController.CreateLogger(isDesignTimeBuild) : null;

        public Task<IImmutableSet<ILogger>> GetLoggersAsync(IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
        {
            var loggers = (IImmutableSet<ILogger>)ImmutableHashSet<ILogger>.Empty;

            if (_loggingController.IsLogging)
            {
                var isDesignTime = properties.TryGetValue("DesignTimeBuild", out var value) &&
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

                loggers = loggers.Add(_loggingController.CreateLogger(isDesignTime));
            }

            return Task.FromResult(loggers);
        }
    }
}
