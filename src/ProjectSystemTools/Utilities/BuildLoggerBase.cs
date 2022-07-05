// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.BuildLogging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools;

public abstract class BuildLoggerBase : IBuildLoggerProviderAsync, IVsBuildLoggerProvider
{
    // TODO investigate these other event kinds:
    //
    //private const BuildLoggerEvents AllBuildLoggerEvents =
    //    BuildLoggerEvents.BuildStartedEvent |              // Build started event
    //    BuildLoggerEvents.BuildFinishedEvent |             // Build finished event
    //    BuildLoggerEvents.ProjectStartedEvent |            // Project started event
    //    BuildLoggerEvents.ProjectFinishedEvent |           // Project finished event
    //    BuildLoggerEvents.ProjectEvaluationStartedEvent |  // Project evaluation started event
    //    BuildLoggerEvents.ProjectEvaluationFinishedEvent | // Project evaluation finished event
    //    BuildLoggerEvents.TargetStartedEvent |             // Target started event
    //    BuildLoggerEvents.TargetFinishedEvent |            // Target finished event
    //    BuildLoggerEvents.TaskStartedEvent |               // Task started event
    //    BuildLoggerEvents.TaskFinishedEvent |              // Task finished event
    //    BuildLoggerEvents.ErrorEvent |                     // Error event
    //    BuildLoggerEvents.WarningEvent |                   // Warning event
    //    BuildLoggerEvents.HighMessageEvent |               // High priority message event
    //    BuildLoggerEvents.NormalMessageEvent |             // Normal priority message event
    //    BuildLoggerEvents.LowMessageEvent |                // Low priority message event
    //    BuildLoggerEvents.CustomEvent |                    // Custom event
    //    BuildLoggerEvents.CommandLine |                    // Command line
    //    BuildLoggerEvents.PerformanceSummary |             // Build performance summary
    //    //BuildLoggerEvents.NoSummary |                    // No build performance summary
    //    BuildLoggerEvents.ShowCommandLine |                // Show command line
    //    BuildLoggerEvents.IncludeEvaluationProfile |       // Include evaluation profiles
    //    BuildLoggerEvents.IncludeTaskInputs;               // Include task inputs

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

    public LoggerVerbosity Verbosity => IsLogging ? LoggerVerbosity.Diagnostic : LoggerVerbosity.Quiet;

    public BuildLoggerEvents Events => IsLogging ? EventsWhenLogging : BuildLoggerEvents.None;

    protected abstract bool IsLogging { get; }

    /// <summary>
    /// Creates an instance of the logger used by this implementation of <see cref="BuildLoggerBase"/>
    /// for a single solution build. The logger will be notified of all build events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In general this method won't be called if <see cref="IsLogging"/> is <see langword="false"/>.
    /// However there's no synchronisation around these calls so consider race conditions.
    /// </para>
    /// <para>
    /// Implementations may decide they don't want to log this build, and return <see langword="null"/>.
    /// </para>
    /// <para>
    /// Currently there is no support for disposing the logger. The logger should clean itself up
    /// when the build complete event is received.
    /// </para>
    /// </remarks>
    /// <param name="targets">The list of MSBuild targets being invoked on the build.</param>
    /// <param name="properties">Global properties to use during the build.</param>
    /// <param name="isDesignTimeBuild">Whether this is a design-time build or not.</param>
    /// <returns>
    /// An instance of a logger for this build, or <see langword="null"/> if the implementation
    /// does not wish to log this build.
    /// </returns>
    protected abstract ILogger? CreateLogger(
        ImmutableArray<string> targets,
        IImmutableDictionary<string, string>? properties,
        bool isDesignTimeBuild);

    public ILogger? GetLogger(string projectPath, IEnumerable<string> targets, IDictionary<string, string>? properties, bool isDesignTimeBuild)
    {
        // Called for CSPROJ (legacy project system) projects.

        return IsLogging ? CreateLogger(targets.ToImmutableArray(), properties?.ToImmutableDictionary(), isDesignTimeBuild) : null;
    }

    public Task<IImmutableSet<ILogger>> GetLoggersAsync(
        IReadOnlyList<string> targets,
        IImmutableDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        // Called for CPS-based projects.

        if (!IsLogging)
        {
            return Empty.LoggersTask;
        }

        var isDesignTimeBuild = properties.GetBoolean("DesignTimeBuild", defaultValue: false);

        ILogger? logger = CreateLogger(targets.ToImmutableArray(), properties, isDesignTimeBuild);

        if (logger == null)
        {
            return Empty.LoggersTask;
        }

        var loggers = ImmutableHashSet<ILogger>.Empty.Add(logger);

        return Task.FromResult<IImmutableSet<ILogger>>(loggers);
    }
}
