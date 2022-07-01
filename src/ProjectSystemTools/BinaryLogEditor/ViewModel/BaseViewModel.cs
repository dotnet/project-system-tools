// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal abstract class BaseViewModel : IViewModelWithProperties
    {
        public abstract string Text { get; }

        public virtual IEnumerable<object> Children => Enumerable.Empty<object>();

        public virtual SelectedObjectWrapper? Properties => null;

        protected static string FormatTime(Time time) =>
            $"In: {time.InclusiveTime:mm':'ss'.'ffff} | Ex: {time.ExclusiveTime:mm':'ss'.'ffff}";
    }
}
