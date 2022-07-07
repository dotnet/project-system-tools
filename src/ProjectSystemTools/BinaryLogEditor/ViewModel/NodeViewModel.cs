// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;

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
