﻿namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class FileCopyOperation
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        /// <summary>
        /// We need to represent both "Copied" and "Did not copy" cases
        /// </summary>
        public bool Copied { get; set; }
    }
}
