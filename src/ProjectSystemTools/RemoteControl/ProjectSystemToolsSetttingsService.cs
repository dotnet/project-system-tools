// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.RemoteControl
{
    [Export(typeof(IProjectSystemToolsSetttingsService))]
    internal class ProjectSystemToolsSetttingsService : IProjectSystemToolsSetttingsService
    {
        public const string HostId = "DesignTimeBuildPerfHelper";

        private const int _pollingMinutes = 1440; // (int)TimeSpan.FromDays(1).TotalMinutes;
        private static readonly TimeSpan expectedFailureDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan updateSucceededDelay = TimeSpan.FromDays(1);
        private static readonly TimeSpan catastrophicFailureDelay = TimeSpan.FromDays(1);

        Dictionary<string, object> settings = new Dictionary<string, object>();
        private readonly IRemoteControlService _remoteControlService;
        private bool currentlyUpdating = false;

        [ImportingConstructor]
        public ProjectSystemToolsSetttingsService(IRemoteControlService remoteControlService)
        {
            _remoteControlService = remoteControlService;
        }

        public bool TryGetSetting<T>(string name, out T value)
        {
            value = default;
            if (settings.TryGetValue(name, out var result))
            {
                value = (T)result;
                return true;
            }

            return false;
        }

        public Task UpdateContinuouslyAsync(string serverPath, CancellationToken token)
        {
            // Only the first thread to try to update this source should succeed
            // and cause us to actually begin the update loop. 
            if (currentlyUpdating)
            {
                // We already have an update loop.  Nothing for us to do.
                return Task.CompletedTask;
            }

            // We were the first ones to try to update this source.  Spawn off a task to do
            // the updating.
            currentlyUpdating = true;

            return UpdateInBackgroundAsync(serverPath, token);
        }

        private async Task UpdateInBackgroundAsync(string serverPath, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await UpdateSettingsAsync(serverPath, token).ConfigureAwait(false);
                        await UpdateSucceededDelay(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception)
                    {
                        await ExpectedFailureDelay(token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                // operation was cancelled
                currentlyUpdating = false;
            }
        }

        private static Task UpdateSucceededDelay(CancellationToken token) => Task.Delay(updateSucceededDelay, token);

        private static Task ExpectedFailureDelay(CancellationToken token) => Task.Delay(expectedFailureDelay, token);

        private async Task UpdateSettingsAsync(string serverPath, CancellationToken token)
        {
            var lastSyncedSettingsRoot = await GetXMLConfigurationAsync(serverPath, token).ConfigureAwait(false);
            UpdateSettings(lastSyncedSettingsRoot);
        }

        private async Task<XElement> GetXMLConfigurationAsync(string serverPath, CancellationToken token)
        {
            using (var client = _remoteControlService.CreateClient(HostId, serverPath, _pollingMinutes))
            {
                while (true)
                {
                    var resultXElement = await TryDownloadFileAsync(client).ConfigureAwait(false);
                    if (resultXElement == null)
                    {
                        await ExpectedFailureDelay(token).ConfigureAwait(false);
                    }
                    else
                    {
                        // File was downloaded.  
                        return resultXElement;
                    }
                }
            }
        }

        private void UpdateSettings(XElement lastSyncedSettingsRoot)
        {
            if (TryGetDesignTimeBuildThreshHold(lastSyncedSettingsRoot, out var threshHold))
            {
                settings[RemoteControlConstants.DesignTimeBuildThreshHold] = threshHold;
            }

            if (TryGetListOfKnownTargets(lastSyncedSettingsRoot, out var knownTargets))
            {
                settings[RemoteControlConstants.ListOfKnownTargets] = threshHold;
            }
        }

        private async Task<XElement> TryDownloadFileAsync(IRemoteControlClient client)
        {
            using (var stream = await client.ReadFileAsync(BehaviorOnStale.ReturnNull).ConfigureAwait(false))
            {
                if (stream == null)
                {
                    return null;
                }

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };
                using (var reader = XmlReader.Create(stream, settings))
                {
                    var result = XElement.Load(reader);
                    return result;
                }
            }
        }

        private bool TryGetListOfKnownTargets(XElement root, out string[] value)
        {
            value = default;
            try
            {
                var result = root?.Element("KnownTargetNames")?.Elements("TargetName")?.Select(x => x.Value).ToArray();
                if (result != null)
                {
                    value = result;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private bool TryGetDesignTimeBuildThreshHold(XElement root, out double threshHold)
        {
            threshHold = default;
            try
            {
                var value = root?.Element("ThreshHold")?.Value;
                if (double.TryParse(value, out var result))
                {
                    threshHold = result;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private class CancellationCoordinator
        {
            private CancellationToken token;
            private Action onCancelled;

            public CancellationCoordinator(CancellationToken token, Action onCancelled)
            {
                this.token = token;
                this.onCancelled = onCancelled;
            }
        }
    }
}
