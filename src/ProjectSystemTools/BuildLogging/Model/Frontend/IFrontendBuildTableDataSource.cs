// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.FrontEnd
{
    public interface IFrontEndBuildTableDataSource : ITableDataSource
    {
        /// <summary>
        /// Provide access to the Table manager associated with this TableDataSource
        /// </summary>
        ITableManager Manager { get; set; }

        /// <summary>
        /// Returns whether or not the the logging service is logging
        /// true if logging, false if not logging
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
        string GetLogForBuild(int buildId);
    }
}
