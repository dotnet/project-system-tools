// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    [Guid(ProjectSystemToolsPackage.BinaryLogEditorFactoryGuidString)]
    public sealed class BinaryLogEditorFactory : IVsEditorFactory, IVsEditorFactory2, IVsEditorFactory3
    {
        private static readonly Guid IID_IUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");
        private static readonly Guid TextEditorCmdUIGuid = new Guid(0x8B382828, 0x6202, 0x11d1, 0x88, 0x70, 0x00, 0x00, 0xF8, 0x75, 0x79, 0xD2);

        private const int CLSCTX_INPROC_SERVER = 0x1;

        private OLE.Interop.IServiceProvider _site;
        private ServiceProvider _serviceProvider;

        private object CreateView(IVsHierarchy hierarchy, uint itemid, string fileName, object docData, out Guid cmdUIGuid)
        {
            cmdUIGuid = TextEditorCmdUIGuid;
            var guidCodeWindow = typeof(IVsCodeWindow).GUID;
            IVsCodeWindow codeWindow;

            if (!(_serviceProvider.GetService(typeof(ILocalRegistry)) is ILocalRegistry localRegistry))
            {
                throw new InvalidOperationException();
            }

            ErrorHandler.ThrowOnFailure(localRegistry.CreateInstance(
                typeof(VsCodeWindowClass).GUID,
                null,
                ref guidCodeWindow,
                CLSCTX_INPROC_SERVER,
                out var codeWindowPtr)
            );

            try
            {
                codeWindow = (IVsCodeWindow)Marshal.GetObjectForIUnknown(codeWindowPtr);
            }
            finally
            {
                Marshal.Release(codeWindowPtr);
            }

            var lines = docData as IVsTextLines;
            if (lines == null)
            {
                if (docData is IVsTextBufferProvider provider)
                {
                    ErrorHandler.ThrowOnFailure(provider.GetTextBuffer(out lines));
                }
            }

            if (lines != null)
            {
                ErrorHandler.ThrowOnFailure(codeWindow.SetBuffer(lines));
            }

            return codeWindow;
        }

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

                object docDataObject;

                if (existingDocData == IntPtr.Zero)
                {
                    var localRegistry = _serviceProvider.GetService(typeof(ILocalRegistry)) as ILocalRegistry;
                    if (localRegistry == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var guidUnknown = IID_IUnknown;
                    ErrorHandler.ThrowOnFailure(localRegistry.CreateInstance(
                                                typeof(VsTextBufferClass).GUID,
                                                null,
                                                ref guidUnknown,
                                                CLSCTX_INPROC_SERVER,
                                                out var newDocDataPtr)
                    );

                    try
                    {
                        docDataObject = Marshal.GetObjectForIUnknown(newDocDataPtr);
                    }
                    finally
                    {
                        Marshal.Release(newDocDataPtr);
                    }

                    if (docDataObject is IObjectWithSite ows)
                    {
                        ows.SetSite(_site);
                    }
                }
                else
                {
                    docDataObject = Marshal.GetObjectForIUnknown(existingDocData);
                    if (!(docDataObject is IVsTextLines textLines))
                    {
                        // Need to close first
                        return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                    }
                }

                view = CreateView(hierarchy, itemid, fileName, docDataObject, out cmdUIGuid);

                caption = "foo";
                docView = Marshal.GetIUnknownForObject(view);
                docData = Marshal.GetIUnknownForObject(docDataObject);
                view = null;
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
            _serviceProvider = new ServiceProvider(_site, false);
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.Close()
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }
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
