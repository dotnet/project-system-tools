﻿// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class ItemViewModel : BaseViewModel
    {
        private readonly Item _item;
        private SelectedObjectWrapper? _properties;

        public override string Text => _item.Name;

        public override SelectedObjectWrapper Properties => _properties ??=
            new SelectedObjectWrapper(
                _item.Name,
                "Item",
                null,
                new Dictionary<string, IDictionary<string, string?>?> {{"Metadata", _item.Metadata!}});

        public ItemViewModel(Item item)
        {
            _item = item;
        }
    }
}
