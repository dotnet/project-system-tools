// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Tools;

internal static class PathUtil
{
    public static string GetTempFileName(string fileName)
    {
        // Put all our temp files in a subfolder of the %TEMP% directory.
        string tempPath = Path.Combine(Path.GetTempPath(), "project-system-tools");

        // Ensure our subfolder exists.
        Directory.CreateDirectory(tempPath);

        return Path.Combine(tempPath, fileName);
    }
}
