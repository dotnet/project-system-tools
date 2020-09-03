﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystemTools.FrontEnd;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal abstract class NodeViewModel : BaseViewModel
    {
        protected abstract Node Node { get; }

        public virtual bool IsPrimary => false;

        public string Elapsed => $"{Node.EndTime - Node.StartTime:mm':'ss'.'ffff}";

        public Result Result => Node.Result;
    }
}
