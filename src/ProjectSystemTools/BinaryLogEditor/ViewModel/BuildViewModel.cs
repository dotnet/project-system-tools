﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class BuildViewModel : NodeViewModel
    {
        private readonly LogModel.Build _build;
        private string? _text;
        private SelectedObjectWrapper? _properties;
        private IEnumerable<object>? _children;

        public override IEnumerable<object> Children => _children ??= GetChildren();

        protected override Node Node => _build;

        public override string Text => _text ??= "Build";

        public override SelectedObjectWrapper Properties => _properties ??=
            new SelectedObjectWrapper(
                "Build",
                "Build",
                _build.Messages,
                new Dictionary<string, IDictionary<string, string?>?> {
                    {"Build", new Dictionary<string, string?>
                        {
                            {"Started", _build.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _build.EndTime.ToString(CultureInfo.InvariantCulture)}
                        }
                    },
                    {"Environment", _build.Environment!}});

        public BuildViewModel(LogModel.Build build)
        {
            _build = build;
        }

        private IEnumerable<object> GetChildren()
        {
            return _build.Project is null
                ? Enumerable.Empty<object>()
                : new[] { new ProjectViewModel(_build.Project) };
        }
    }
}
