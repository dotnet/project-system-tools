// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.ProjectSystemTools.FrontEnd;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class BuildViewModel : NodeViewModel
    {
        private readonly ProjectSystemTools.FrontEnd.Build _build;
        private string _text;
        private SelectedObjectWrapper _properties;
        private IEnumerable<object> _children;

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        protected override Node Node => _build;

        public override string Text => _text ?? (_text = "Build");

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                "Build",
                "Build",
                _build.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Started", _build.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _build.EndTime.ToString(CultureInfo.InvariantCulture)}
                        }
                    },
                    {"Environment", _build.Environment}}));

        public BuildViewModel(ProjectSystemTools.FrontEnd.Build build)
        {
            _build = build;
        }

        private IEnumerable<object> GetChildren()
        {
            var list = new List<object>();

            if (_build.Project != null)
            {
                list.Add(new ProjectViewModel(_build.Project));
            }

            return list;
        }
    }
}
