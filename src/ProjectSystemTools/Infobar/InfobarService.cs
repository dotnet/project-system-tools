// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Infobar
{
    [Export(typeof(IInfoBarService))]
    internal class InfobarService : IInfoBarService
    {
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public InfobarService(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ShowInfoBarInActiveViewAsync(string message, params InfoBarUI[] items)
        {
            await ShowInfoBarAsync(activeView: true, message: message, items: items);
        }

        public async Task ShowInfoBarInGlobalViewAsync(string message, params InfoBarUI[] items)
        {
            await ShowInfoBarAsync(activeView: false, message: message, items: items);
        }

        private async Task ShowInfoBarAsync(bool activeView, string message, params InfoBarUI[] items)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (TryGetInfoBarData(activeView, out var infoBarHost))
            {
                CreateInfoBar(infoBarHost, message, items);
            }
        }

        private bool TryGetInfoBarData(bool activeView, out IVsInfoBarHost infoBarHost)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            infoBarHost = null;

            if (activeView)
            {
                var monitorSelectionService = _serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

                // We want to get whichever window is currently in focus (including toolbars) as we could have had an exception thrown from the error list
                // or interactive window
                if (monitorSelectionService == null ||
                    ErrorHandler.Failed(monitorSelectionService.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var value)))
                {
                    return false;
                }

                var frame = value as IVsWindowFrame;
                if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var activeViewInfoBar)))
                {
                    return false;
                }

                infoBarHost = activeViewInfoBar as IVsInfoBarHost;
                return infoBarHost != null;
            }

            // global error info, show it on main window info bar
            var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (shell == null ||
                ErrorHandler.Failed(shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var globalInfoBar)))
            {
                return false;
            }

            infoBarHost = globalInfoBar as IVsInfoBarHost;
            return infoBarHost != null;
        }

        private void CreateInfoBar(IVsInfoBarHost infoBarHost, string message, InfoBarUI[] items)
        {
            var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            if (factory == null)
            {
                // no info bar factory, don't do anything
                return;
            }

            var textSpans = new List<IVsInfoBarTextSpan>()
            {
                new InfoBarTextSpan(message)
            };

            // create action item list
            var actionItems = new List<IVsInfoBarActionItem>();

            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case InfoBarUI.UIKind.Button:
                        actionItems.Add(new InfoBarButton(item.Title));
                        break;
                    case InfoBarUI.UIKind.HyperLink:
                        actionItems.Add(new InfoBarHyperlink(item.Title));
                        break;
                    case InfoBarUI.UIKind.Close:
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected value '{item.Kind}' of type '{item.Kind.GetType().FullName}");
                }
            }

            var infoBarModel = new InfoBarModel(
                textSpans,
                actionItems.ToArray(),
                KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);

            if (!TryCreateInfoBarUI(factory, infoBarModel, out var infoBarUI))
            {
                return;
            }

            uint? infoBarCookie = null;
            var eventSink = new InfoBarEvents(items, () =>
            {
                // run given onClose action if there is one.
                items.FirstOrDefault(i => i.Kind == InfoBarUI.UIKind.Close).Action?.Invoke();

                if (infoBarCookie.HasValue)
                {
                    infoBarUI.Unadvise(infoBarCookie.Value);
                }
            });

            infoBarUI.Advise(eventSink, out var cookie);
            infoBarCookie = cookie;

            infoBarHost.AddInfoBar(infoBarUI);
        }

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
        {
            private readonly InfoBarUI[] _items;
            private readonly Action _onClose;

            public InfoBarEvents(InfoBarUI[] items, Action onClose)
            {
                _onClose = onClose ?? throw new ArgumentNullException(nameof(onClose));
                _items = items;
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                var item = _items.FirstOrDefault(i => i.Title == actionItem.Text);
                if (item.IsDefault)
                {
                    return;
                }

                item.Action?.Invoke();

                if (!item.CloseAfterAction)
                {
                    return;
                }

                infoBarUIElement.Close();
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                _onClose();
            }
        }
    }
}
