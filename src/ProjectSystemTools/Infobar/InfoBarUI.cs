// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Infobar
{
    public struct InfoBarUI
    {
        public readonly string Title;
        public readonly UIKind Kind;
        public readonly Action Action;
        public readonly bool CloseAfterAction;

        public InfoBarUI(string title, UIKind kind, Action action, bool closeAfterAction = true)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Kind = kind;
            Action = action;
            CloseAfterAction = closeAfterAction;
        }

        public bool IsDefault => Title == null;

        public enum UIKind
        {
            Button,
            HyperLink,
            Close
        }
    }
}
