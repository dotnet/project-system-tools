// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class DiagnosticInfo
    {
        public bool IsError;
        public string Text;
        public DateTime Timestamp;
        public string Code;
        public int ColumnNumber;
        public int EndColumnNumber;
        public int EndLineNumber;
        public int LineNumber;
        public string File;
        public string ProjectFile;
        public string Subcategory;
        public int ProjectParent;
        public int TargetParent;
        public int TaskParent;
    }
}
