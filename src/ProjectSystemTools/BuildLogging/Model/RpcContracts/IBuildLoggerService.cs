// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts
{
    /// <summary>
    /// The main interface between the client and the server.
    /// These operations are async
    /// </summary>
    internal interface IBuildLoggerService
    {
        /// <summary>
        /// Returns whether or not the build logging window is currently tracking logs or not
        /// </summary>
        bool IsLogging { get; }

        /// <summary>
        /// Gets whether build logging supports roslyn logging or not
        /// </summary>
        bool SupportsRoslynLogging { get; }

        /// <summary>
        /// Tell build logging to start tracking logs
        /// </summary>
        void Start(Action notifyCallback);

        /// <summary>
        /// Tell build logging to stop tracking logs
        /// </summary>
        void Stop();

        /// <summary>
        /// Tell build logging to clear out all the accumulated logs contained on the server.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gives the user a log of a requested build
        /// </summary>
        /// <param name="buildId">an ID used to retrieve a unique log for a build</param>
        /// <returns>The log tied to the requested BuildHandle</returns>
        string? GetLogForBuild(int buildId);

        /// <summary>
        /// Gives the user a requested build
        /// </summary>
        /// <returns>List of summary information of all builds on the server</returns>
        ImmutableArray<BuildSummary> GetAllBuilds();
    }
}
