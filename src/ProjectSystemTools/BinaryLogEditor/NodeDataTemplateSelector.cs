// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    public sealed class NodeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement frameworkElement)
            {
                if (item is NodeViewModel nodeViewModel)
                {
                    switch (nodeViewModel.Result)
                    {
                        case Result.Failed:
                            return frameworkElement.FindResource("FailedDataTemplate") as DataTemplate;

                        case Result.Succeeded:
                            return frameworkElement.FindResource("SucceededDataTemplate") as DataTemplate;

                        case Result.Skipped:
                            return frameworkElement.FindResource("SkippedDataTemplate") as DataTemplate;
                    }
                }

                return frameworkElement.FindResource("BaseDataTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
