// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    public interface IFrontEndBuildTableDataSource : ITableDataSource
    {
        /// <summary>
        /// Gets whether the logging service is logging
        /// </summary>
        bool IsLogging { get; }

        /// <summary>
        /// Tells the attached logging service to start logging
        /// </summary>
        void Start();

        /// <summary>
        /// Tells the attached logging service to stop logging
        /// </summary>
        void Stop();

        /// <summary>
        /// Tells the attached logging service to clear out
        /// and removes all currently stored build entries
        /// </summary>
        void Clear();

        /// <summary>
        /// Ask the logging service to give back a log file associated
        /// with a certain buildId
        /// </summary>
        /// <param name="buildId">an Id that refers to a specific build on the logging service</param>
        /// <returns>The filepath to the requested build's log file</returns>
        string? GetLogForBuild(int buildId);
    }
}
