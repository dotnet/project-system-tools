﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class EvaluatedPassViewModel : BaseViewModel
    {
        private readonly EvaluatedPass _evaluatedPass;
        private string? _text;
        private List<object>? _children;

        public override string Text => _text ??= $"{_evaluatedPass.Description} [{FormatTime(_evaluatedPass.Time)}]";

        public override IEnumerable<object> Children => _children ??= GetChildren();

        public EvaluatedPassViewModel(EvaluatedPass evaluatedPass)
        {
            _evaluatedPass = evaluatedPass;
        }

        private List<object> GetChildren() => ((IEnumerable<object>)_evaluatedPass.Locations.Select(location => new EvaluatedLocationViewModel(location))).ToList();
    }
}
