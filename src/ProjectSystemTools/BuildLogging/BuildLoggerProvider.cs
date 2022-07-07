// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.BuildLogging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    [Export(typeof(IBuildLoggerProviderAsync))] // For CPS
    [Export(typeof(IVsBuildLoggerProvider))]    // For VS
    internal sealed class BuildLoggerProvider : BuildLoggerBase
    {
        private readonly ILoggingController _loggingController;

        [ImportingConstructor]
        public BuildLoggerProvider(ILoggingController loggingController)
        {
            _loggingController = loggingController;
        }

        protected override bool IsLogging => _loggingController.IsLogging;

        protected override ILogger? CreateLogger(
            ImmutableArray<string> targets,
            IImmutableDictionary<string, string>? properties,
            bool isDesignTimeBuild)
        {
            return _loggingController.CreateLogger(isDesignTimeBuild);
        }
    }
}
