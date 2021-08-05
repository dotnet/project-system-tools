// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class RootViewModel
    {
        public ObservableCollection<BaseViewModel> Children { get; }

        public RootViewModel(ObservableCollection<BaseViewModel> logs)
        {
            Children = logs;
        }
    }
}
