// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.ProjectQuery
{
    /// <summary>
    /// Interaction logic for ProjectQueryDialog.xaml
    /// </summary>
    internal partial class ProjectQueryDialog : DialogWindow
    {
        public ProjectQueryDialog(string title, string message)
        {
            InitializeComponent();

            Title = title;
            MessageBlock.Text = message;

            InputField.Focus();
        }

        private void OnOKButton(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelButton(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
