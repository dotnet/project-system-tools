// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    [Guid(ProjectSystemToolsPackage.BinaryLogEditorFactoryGuidString)]
    public sealed class BinaryLogEditorFactory : IVsEditorFactory, IVsEditorFactory2, IVsEditorFactory3
    {
        private OLE.Interop.IServiceProvider _site;

        int IVsEditorFactory.CreateEditorInstance(uint vsCreateEditorFlags, string fileName, string physicalView,
            IVsHierarchy hierarchy, uint itemid, IntPtr existingDocData,
            out IntPtr docView, out IntPtr docData,
            out string caption, out Guid cmdUIGuid, out int flags)
        {
            object view = null;
            docView = IntPtr.Zero;
            docData = IntPtr.Zero;
            caption = null;
            cmdUIGuid = Guid.Empty;
            flags = 0;

            var oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                if ((vsCreateEditorFlags & (uint)(__VSCREATEEDITORFLAGS.CEF_OPENFILE | __VSCREATEEDITORFLAGS.CEF_SILENT)) == 0)
                {
                    throw new ArgumentException(Resources.BadCreateFlags, nameof(vsCreateEditorFlags));
                }

            }
            finally
            {
                IDisposable d;
                if ((d = view as IDisposable) != null)
                {
                    d.Dispose();
                }

                Cursor.Current = oldCursor;
            }

            return VSConstants.S_OK;
        }

        int IVsEditorFactory.SetSite(OLE.Interop.IServiceProvider site)
        {
            _site = site;
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.Close()
        {
            _site = null;
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.MapLogicalView(ref Guid logicalView, out string physicalView)
        {
            physicalView = null;

            return logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdAnyGuid) ||
                   logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdPrimaryGuid) ||
                   logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdDebuggingGuid) ||
                   logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdCodeGuid) ||
                   logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdDesignerGuid) ||
                   logicalView.Equals(ProjectSystemToolsPackage.LogicalViewIdTextViewGuid)
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsEditorFactory2.RetargetCodeOrDesignerToOpen(string pszMkDocumentSource, ref Guid rguidLogicalViewSource, IVsHierarchy pvHier,
            uint itemidSource, out uint pitemidTarget, out uint pgrfEditorFlags, out Guid pguidEditorTypeTarget,
            out string pbstrPhysicalViewTarget, out Guid pguidLogicalViewTarget)
        {
            pitemidTarget = VSConstants.VSITEMID_NIL;
            pgrfEditorFlags = (uint)__VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_DoOpen;
            pguidEditorTypeTarget = Editor.DefGuidList.CLSID_TextEditorFactory;
            pbstrPhysicalViewTarget = null;
            pguidLogicalViewTarget = rguidLogicalViewSource;
            return VSConstants.S_OK;
        }

        bool IVsEditorFactory3.IsProjectLoadRequired() => false;
    }
}
