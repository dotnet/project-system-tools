// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers.RpcContracts
{
    /// <summary>
    /// The main interface between the client and the server.
    /// These operations are async
    /// </summary>
    public interface IBuildLoggerService
    {
        /// <summary>
        /// Returns whether or not the build logging window is currently tracking logs or not
        /// </summary>
        /// <returns>True if build logging window is tracking logs and false otherwise</returns>
        bool IsLogging();

        bool SupportsRoslynLogging();

        /// <summary>
        /// Tell build logging to start tracking logs
        /// </summary>
        /// <returns>True is operation succeeded and false if not</returns>
        void Start(NotifyCallback notifyCallback);

        /// <summary>
        /// Tell build logging to stop tracking logs
        /// </summary>
        /// <returns>True if operation succeeded and false if not</returns>
        void Stop();

        /// <summary>
        /// Tell build logging to clear out all the accumulated logs contained on the server.
        /// </summary>
        /// <returns>True if operation succeeded and false if not</returns>
        void Clear();

        ///// <summary>
        ///// Gives the user a log of a requested build
        ///// </summary>
        ///// <param name="handle">an ID used to retrieve a unique log for a build</param>
        ///// <returns>The log tied to the requested BuildHandle</returns>
        string RetrieveLogForBuild(int buildID);

        ///// <summary>
        ///// Gives the user a requested build
        ///// </summary>
        ///// <param name="handle">an ID used to retrieve a unique build</param>
        ///// <returns>The Build summary information tied to the requested Build ID</returns>
        //BuildSummary RetrieveBuild(int buildID);

        ///// <summary>
        ///// Gives the user a requested build
        ///// </summary>
        ///// <returns>List of summary information of all builds on the server</returns>
        ImmutableList<IBuildSummary> RetrieveAllBuilds();
    }
}
