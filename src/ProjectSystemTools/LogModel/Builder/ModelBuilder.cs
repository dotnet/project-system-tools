// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class ModelBuilder
    {
        private static readonly char[] Space = { ' ' };

        private static readonly Regex UsingTaskRegex = new Regex("Using \"(?<task>.+)\" task from (assembly|the task factory) \"(?<assembly>.+)\"\\.", RegexOptions.Compiled);
        private static readonly Regex ProjectStart = new Regex("Project \"(?<project>.+)\" \\(.+\\):");

        private bool _done;
        private BuildInfo _buildInfo;
        private readonly ConcurrentDictionary<int, ProjectInfo> _projectInfos = new ConcurrentDictionary<int, ProjectInfo>();
        private readonly ConcurrentDictionary<string, string> _assemblies =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _strings = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentBag<DiagnosticInfo> _diagnostics = new ConcurrentBag<DiagnosticInfo>();

        private readonly object _syncLock = new object();

        public ModelBuilder(IEventSource eventSource)
        {
            eventSource.BuildStarted += BuildStarted;
            eventSource.BuildFinished += BuildFinished;
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
            eventSource.TargetStarted += TargetStarted;
            eventSource.TargetFinished += TargetFinished;
            eventSource.TaskStarted += TaskStarted;
            eventSource.TaskFinished += TaskFinished;
            eventSource.MessageRaised += MessageRaised;
            eventSource.CustomEventRaised += CustomEventRaised;
            eventSource.StatusEventRaised += StatusEventRaised;
            eventSource.ErrorRaised += ErrorRaised;
            eventSource.WarningRaised += WarningRaised;
        }

        private string Intern(string text)
        {
            if (text == null)
            {
                return null;
            }

            if (text.Length == 0)
            {
                return string.Empty;
            }

            if (_strings.TryGetValue(text.Replace("\r\n", "\n").Replace("\r", "\n"), out var existing))
            {
                return existing;
            }

            _strings[text] = text;
            return text;
        }

        private void AddToDictionary(Dictionary<string, string> metadata, object key, object value, bool overwrite = false)
        {
            var internedKey = Intern(Convert.ToString(key));
            var internedValue = Intern(Convert.ToString(value));

            if (!overwrite && metadata.ContainsKey(internedKey))
            {
                throw new LoggerException(Resources.OverwritingMetadata);
            }

            metadata[internedKey] = internedValue;
        }

        public void BuildStarted(object sender, BuildStartedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    _buildInfo = new BuildInfo
                    {
                        StartTime = args.Timestamp,
                        Environment = args.BuildEnvironment.ToImmutableDictionary()
                    };
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void BuildFinished(object sender, BuildFinishedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    _buildInfo.EndTime = args.Timestamp;
                    _buildInfo.Succeeded = args.Succeeded;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void ProjectStarted(object sender, ProjectStartedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var projectInfo = new ProjectInfo();

                    var parentProjectId = args.ParentProjectBuildEventContext.ProjectContextId;
                    if (parentProjectId > 0)
                    {
                        projectInfo.ParentProject = parentProjectId;
                    }

                    projectInfo.NodeId = args.BuildEventContext.NodeId;
                    projectInfo.StartTime = args.Timestamp;
                    projectInfo.TargetNames = args.TargetNames.Split(';');
                    projectInfo.ToolsVersion = args.ToolsVersion;

                    var result = ProjectStart.Match(args.Message);
                    if (!result.Success)
                    {
                        throw new LoggerException(Resources.UnexpectedProjectStartMessage);
                    }

                    projectInfo.Name = Intern(result.Groups["project"].Value);
                    projectInfo.ProjectFile = Intern(args.ProjectFile);

                    if (args.GlobalProperties != null)
                    {
                        projectInfo.GlobalProperties = ImmutableDictionary<string, string>.Empty.AddRange(
                            args.GlobalProperties.Select(d => new KeyValuePair<string, string>(Intern(d.Key), Intern(d.Value)))
                        );
                    }

                    if (args.Properties != null)
                    {
                        projectInfo.Properties = ImmutableDictionary<string, string>.Empty.AddRange(
                            args
                            .Properties
                            .Cast<DictionaryEntry>()
                            .OrderBy(d => d.Key)
                            .Select(d => new KeyValuePair<string, string>(
                                Intern(Convert.ToString(d.Key)),
                                Intern(Convert.ToString(d.Value)))));
                    }

                    if (args.Items != null)
                    {
                        var itemGroups = new List<ItemGroupInfo>();
                        foreach (DictionaryEntry kvp in args.Items)
                        {
                            ItemGroupInfo itemGroup;
                            var itemName = Intern(Convert.ToString(kvp.Key));

                            if (itemGroups.Count > 0 && itemGroups[itemGroups.Count - 1].Name == itemName)
                            {
                                itemGroup = itemGroups[itemGroups.Count - 1];
                            }
                            else
                            {
                                itemGroup = new ItemGroupInfo { Name = itemName };
                                itemGroups.Add(itemGroup);
                            }

                            if (!(kvp.Value is ITaskItem taskItem))
                            {
                                continue;
                            }

                            var item = new ItemInfo { Name = Intern(taskItem.ItemSpec) };

                            foreach (DictionaryEntry metadataName in taskItem.CloneCustomMetadata())
                            {
                                AddToDictionary(item.Metadata, metadataName.Key, metadataName.Value);
                            }

                            itemGroup.Items.Add(item);
                        }

                        projectInfo.ItemGroups = itemGroups;
                    }

                    if (_projectInfos.ContainsKey(args.BuildEventContext.ProjectContextId))
                    {
                        throw new LoggerException(Resources.DoubleCreationOfProject);
                    }
                    _projectInfos[args.BuildEventContext.ProjectContextId] = projectInfo;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void ProjectFinished(object sender, ProjectFinishedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }
                    projectInfo.EndTime = args.Timestamp;
                    projectInfo.Succeeded = args.Succeeded;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TargetStarted(object sender, TargetStartedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (projectInfo.TargetInfos.ContainsKey(args.BuildEventContext.TargetId))
                    {
                        throw new LoggerException(Resources.DuplicateTarget);
                    }

                    var targetInfo = new TargetInfo
                    {
                        ParentProject = args.BuildEventContext.ProjectContextId,
                        Name = Intern(args.TargetName),
                        NodeId = args.BuildEventContext.NodeId,
                        StartTime = args.Timestamp,
                        SourceFilePath = Intern(args.TargetFile)
                    };

                    if (!string.IsNullOrEmpty(args.ParentTarget))
                    {
                        targetInfo.ParentTarget = Intern(args.ParentTarget);
                    }

                    projectInfo.TargetInfos[args.BuildEventContext.TargetId] = targetInfo;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TargetFinished(object sender, TargetFinishedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTarget);
                    }

                    targetInfo.EndTime = args.Timestamp;
                    targetInfo.Succeeded = args.Succeeded;

                    if (args.TargetOutputs == null)
                    {
                        return;
                    }

                    var targetOutputs = new List<ItemInfo>();

                    foreach (ITaskItem targetOutput in args.TargetOutputs)
                    {
                        var itemInfo = new ItemInfo { Name = Intern(targetOutput.ItemSpec) };
                        foreach (DictionaryEntry metadata in targetOutput.CloneCustomMetadata())
                        {
                            AddToDictionary(itemInfo.Metadata, metadata.Key, metadata.Value);
                        }

                        targetOutputs.Add(itemInfo);
                    }

                    targetInfo.OutputItems = new ItemGroupInfo {Items = targetOutputs};
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TaskStarted(object sender, TaskStartedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTarget);
                    }

                    if (targetInfo.TaskInfos.ContainsKey(args.BuildEventContext.TaskId))
                    {
                        throw new LoggerException(Resources.DuplicateTask);
                    }

                    var taskInfo = new TaskInfo
                    {
                        Name = Intern(args.TaskName),
                        Id = args.BuildEventContext.TaskId,
                        NodeId = args.BuildEventContext.NodeId,
                        ParentProject = args.BuildEventContext.ProjectContextId,
                        ParentTarget = args.BuildEventContext.TargetId,
                        StartTime = args.Timestamp,
                        FromAssembly = Intern(_assemblies.TryGetValue(Intern(args.TaskName), out var assembly)
                            ? assembly
                            : string.Empty),
                        SourceFilePath = Intern(args.TaskFile)
                    };

                    targetInfo.TaskInfos[taskInfo.Id] = taskInfo;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TaskFinished(object sender, TaskFinishedEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTarget);
                    }

                    if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                    {
                        throw new LoggerException(Resources.MissingTask);
                    }

                    taskInfo.EndTime = args.Timestamp;
                    taskInfo.Succeeded = args.Succeeded;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static string ParseQuotedSubstring(string text)
        {
            var firstQuote = text.IndexOf('"');
            if (firstQuote == -1)
            {
                return text;
            }

            var secondQuote = text.IndexOf('"', firstQuote + 1);
            if (secondQuote == -1)
            {
                return text;
            }

            if (secondQuote - firstQuote < 2)
            {
                return text;
            }

            return text.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        private static KeyValuePair<string, string> ParseNameValue(string nameEqualsValue, int trimFromStart = 0)
        {
            var equals = nameEqualsValue.IndexOf('=');
            if (equals == -1)
            {
                return new KeyValuePair<string, string>(nameEqualsValue, "");
            }

            var name = nameEqualsValue.Substring(trimFromStart, equals - trimFromStart);
            var value = nameEqualsValue.Substring(equals + 1);
            return new KeyValuePair<string, string>(name, value);
        }

        private bool IsEvaluationMessage(string message)
        {
            return message.StartsWith("Search paths being used")
                   || message.StartsWith("Overriding target")
                   || message.StartsWith("Trying to import")
                   || message.StartsWith("Property reassignment")
                   || message.StartsWith("Importing project")
                   || (message.StartsWith("Project \"") && message.Contains("was not imported by"));
        }

        private void AddMessage(BuildEventArgs args)
        {
            var message = Intern(args.Message);

            if (args.BuildEventContext != null)
            {
                if (args.BuildEventContext.TaskId > 0)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTarget);
                    }

                    if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTask);
                    }

                    switch (taskInfo.Name)
                    {
                        case "ResolveAssemblyReference":
                            {
                                if (!message.StartsWith("    "))
                                {
                                    var parameterInfo = new ParameterInfo { Name = Intern(message.TrimEnd(':')), ItemGroup = new ItemGroupInfo() };

                                    if (taskInfo.Results.Any() ||
                                        message.StartsWith("Unified primary reference ") ||
                                        message.StartsWith("Primary reference ") ||
                                        message.StartsWith("Dependency ") ||
                                        message.StartsWith("Unified Dependency "))
                                    {
                                        taskInfo.Results.Add(parameterInfo);
                                    }
                                    else
                                    {
                                        taskInfo.Inputs.Add(parameterInfo);
                                    }

                                    return;
                                }
                                else
                                {
                                    var list = taskInfo.Results.Any() ? taskInfo.Results : taskInfo.Inputs;

                                    message = message.Substring(4);

                                    if (list.Count == 0)
                                    {
                                        throw new LoggerException(Resources.CannotFindProperty);
                                    }

                                    if (string.IsNullOrWhiteSpace(message))
                                    {
                                        return;
                                    }

                                    var parameterInfo = list[list.Count - 1];

                                    if (message.StartsWith("    "))
                                    {
                                        message = message.Substring(4);

                                        if (message.StartsWith("For SearchPath") ||
                                            message.StartsWith("Considered"))
                                        {
                                            parameterInfo.Messages.Add(message);
                                            return;
                                        }

                                        if (parameterInfo.ItemGroup.Items.Count != 0)
                                        {
                                            var item = parameterInfo.ItemGroup.Items[parameterInfo.ItemGroup.Items.Count - 1];

                                            if (message.IndexOf('=') == -1)
                                            {
                                                throw new LoggerException(Resources.ExpectedMetadata);
                                            }

                                            var kvp = ParseNameValue(message);
                                            AddToDictionary(item.Metadata, kvp.Key.TrimEnd(Space), kvp.Value.TrimStart(Space));
                                            return;
                                        }
                                    }

                                    parameterInfo.ItemGroup.Items.Add(new ItemInfo
                                    {
                                        Name = Intern(message)
                                    });

                                    return;
                                }
                            }

                        case "MSBuild":
                            {
                                if (message.StartsWith("Global Properties") ||
                                    message.StartsWith("Additional Properties") ||
                                    message.StartsWith("Overriding Global Properties") ||
                                    message.StartsWith("Removing Properties"))
                                {
                                    taskInfo.PropertyGroups.Add(new PropertyGroupInfo {Name = message});
                                    return;
                                }

                                if (taskInfo.PropertyGroups.Count == 0)
                                {
                                    throw new LoggerException(Resources.CannotFindPropertyGroup);
                                }

                                var list = taskInfo.PropertyGroups[taskInfo.PropertyGroups.Count - 1];

                                if (message[0] == ' ' && message[1] == ' ')
                                {
                                    message = message.Substring(2);
                                }

                                var kvp = ParseNameValue(message);
                                AddToDictionary(list.Properties, kvp.Key, kvp.Value);
                                return;
                            }
                    }

                    taskInfo.Messages.Add(message);
                    return;
                }

                if (args.BuildEventContext.TargetId > 0)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                    {
                        throw new LoggerException(Resources.CannotFindTarget);
                    }
                    targetInfo.Messages.Add(message);
                    return;
                }

                if (args.BuildEventContext.ProjectContextId > 0)
                {
                    if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindProject);
                    }

                    if (message.StartsWith("Target") && message.Contains("skipped"))
                    {
                        var targetName = Intern(ParseQuotedSubstring(message));
                        if (targetName != null)
                        {
                            projectInfo.SkippedTargets.Add(new TargetInfo
                            {
                                Name = targetName,
                                ParentProject = args.BuildEventContext.ProjectContextId
                            });
                            return;
                        }
                    }

                    projectInfo.Messages.Add(message);
                    return;
                }

                if (args.BuildEventContext.EvaluationId != -1)
                {
                    if (!_buildInfo.EvaluatedProjects.TryGetValue(args.BuildEventContext.EvaluationId,
                        out var projectInfo))
                    {
                        throw new LoggerException(Resources.CannotFindEvaluatedProject);
                    }

                    if (projectInfo.Messages.Any(m => m == message))
                    {
                        // avoid duplicate messages
                        return;
                    }

                    projectInfo.Messages.Add(message);
                    return;
                }
            }

            if (!IsEvaluationMessage(message) &&
                !(message.StartsWith("The target") && message.Contains("does not exist in the project, and will be ignored")) &&
                (args.BuildEventContext == null ||
                args.BuildEventContext.NodeId != 0 ||
                args.BuildEventContext.ProjectContextId != 0 ||
                args.BuildEventContext.ProjectInstanceId != 0 ||
                args.BuildEventContext.TargetId != 0 ||
                args.BuildEventContext.TaskId != 0))
            {
                throw new LoggerException(Resources.UnexpectedMessage);
            }

            _buildInfo.Messages.Add(message);
        }

        private static int GetNumberOfLeadingSpaces(string line)
        {
            var result = 0;
            while (result < line.Length && line[result] == ' ')
            {
                result++;
            }

            return result;
        }

        private KeyValuePair<string, string> ParseProperty(string message, string prefix)
        {
            var nameValue = ParseNameValue(message, trimFromStart: prefix.Length);
            var propertyInfo = new KeyValuePair<string, string>(Intern(nameValue.Key), Intern(nameValue.Value));
            return propertyInfo;
        }

        private ItemGroupInfo ParseItemGroup(string message, string prefix, ItemGroupType type = ItemGroupType.None)
        {
            message = message.Replace("\r\n", "\n");
            message = message.Replace('\r', '\n');
            var lines = message.Split('\n');

            var itemGroupInfo = new ItemGroupInfo { Type = type };

            // If no items were produced, we only get one line.
            if (lines[0].Length > prefix.Length)
            {
                var nameValue = ParseNameValue(lines[0].Substring(prefix.Length));

                itemGroupInfo.Name = Intern(nameValue.Key);

                if (!string.IsNullOrEmpty(nameValue.Value))
                {
                    itemGroupInfo.Items.Add(new ItemInfo
                    {
                        Name = Intern(nameValue.Value)
                    });
                }

                if (lines.Length > 1)
                {
                    throw new LoggerException(Resources.UnexpectedMessage);
                }

                return itemGroupInfo;
            }

            ItemInfo currentItem = null;
            string currentMetadata = null;
            foreach (var line in lines)
            {
                var numberOfLeadingSpaces = GetNumberOfLeadingSpaces(line);
                switch (numberOfLeadingSpaces)
                {
                    case 4:
                        if (line.EndsWith("=", StringComparison.Ordinal))
                        {
                            itemGroupInfo.Name = Intern(line.Substring(4, line.Length - 5));
                        }
                        else
                        {
                            throw new LoggerException(Resources.ExpectedItemGroupName);
                        }
                        break;
                    case 8:
                        currentItem = new ItemInfo
                        {
                            Name = Intern(line.Substring(8))
                        };
                        itemGroupInfo.Items.Add(currentItem);
                        currentMetadata = null;
                        break;
                    case 16:
                        var currentLine = line.Substring(16);
                        if (currentItem != null)
                        {
                            if (!currentLine.Contains("="))
                            {
                                if (currentMetadata != null)
                                {
                                    var metadata = currentItem.Metadata[currentMetadata];
                                    currentItem.Metadata[currentMetadata] = (metadata ?? "") + line;
                                }
                                else
                                {
                                    throw new LoggerException(Resources.BadMetadataContinuation);
                                }
                            }
                            else
                            {
                                var nameValue = ParseNameValue(currentLine);
                                AddToDictionary(currentItem.Metadata, nameValue.Key, nameValue.Value);
                                currentMetadata = nameValue.Key;
                            }
                        }
                        break;
                    default:
                        if (numberOfLeadingSpaces == 0 && line == prefix)
                        {
                            continue;
                        }

                        if (currentItem != null)
                        {
                            if (currentMetadata != null)
                            {
                                var metadata = currentItem.Metadata[currentMetadata];
                                currentItem.Metadata[currentMetadata] = (metadata ?? "") + line;
                            }
                            else
                            {
                                throw new LoggerException(Resources.BadMetadataContinuation);
                            }
                        }
                        break;
                }
            }

            return itemGroupInfo;
        }

        private ParameterInfo ParseParameter(string message, string prefix) =>
            // Property values can have line breaks in them, for item groups, it will be
            // right after the prefix.
            message.IndexOf('\n') == prefix.Length
                ? new ParameterInfo {ItemGroup = ParseItemGroup(message, prefix)}
                : new ParameterInfo {Property = ParseProperty(message, prefix)};

        private void ProcessMessage(BuildMessageEventArgs args)
        {
            const string taskParameterMessagePrefix = @"Task Parameter:";
            const string outputItemsMessagePrefix = @"Output Item(s): ";
            const string outputPropertyMessagePrefix = @"Output Property: ";
            const string propertyGroupMessagePrefix = @"Set Property: ";
            const string itemGroupIncludeMessagePrefix = @"Added Item(s): ";
            const string itemGroupRemoveMessagePrefix = @"Removed Item(s): ";

            var message = args?.Message;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            switch (message[0])
            {
                case 'A':
                    if (message.StartsWith(itemGroupIncludeMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        var itemGroupInfo = ParseItemGroup(args.Message, itemGroupIncludeMessagePrefix, ItemGroupType.Add);
                        targetInfo.ItemGroups.Add(itemGroupInfo);
                        return;
                    }
                    break;

                case 'O':
                    if (message.StartsWith(outputItemsMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTask);
                        }
                        taskInfo.OutputItems.Add(ParseItemGroup(message, outputItemsMessagePrefix));
                        return;
                    }

                    if (message.StartsWith(outputPropertyMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTask);
                        }

                        var property = ParseProperty(message, outputPropertyMessagePrefix);
                        AddToDictionary(taskInfo.OutputProperties, property.Key, property.Value);
                        return;
                    }
                    break;

                case 'R':
                    if (message.StartsWith(itemGroupRemoveMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        var itemGroupInfo = ParseItemGroup(args.Message, itemGroupRemoveMessagePrefix, ItemGroupType.Remove);
                        targetInfo.ItemGroups.Add(itemGroupInfo);
                        return;
                    }
                    break;

                case 'S':
                    if (message.StartsWith(propertyGroupMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        message = args.Message.Substring(propertyGroupMessagePrefix.Length);

                        var kvp = ParseNameValue(message);
                        targetInfo.Properties.Add(new KeyValuePair<string, string>(Intern(kvp.Key), Intern(kvp.Value)));
                        return;
                    }
                    break;

                case 'T':
                    if (message.StartsWith(taskParameterMessagePrefix))
                    {
                        if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                        {
                            throw new LoggerException(Resources.CannotFindProject);
                        }

                        if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTarget);
                        }

                        if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                        {
                            throw new LoggerException(Resources.CannotFindTask);
                        }
                        var parameterInfo = ParseParameter(message, taskParameterMessagePrefix);
                        taskInfo.Parameters.Add(parameterInfo);
                        return;
                    }
                    break;

                case 'U':
                    // A task from assembly message (parses out the task name and assembly path).
                    var match = UsingTaskRegex.Match(message);
                    if (match.Success)
                    {
                        var taskName = Intern(match.Groups["task"].Value);
                        var assembly = Intern(match.Groups["assembly"].Value);
                        _assemblies.GetOrAdd(taskName, t => assembly);
                        return;
                    }

                    break;
            }

            if (args is TaskCommandLineEventArgs taskArgs)
            {
                if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
                {
                    throw new LoggerException(Resources.CannotFindProject);
                }

                if (!projectInfo.TargetInfos.TryGetValue(args.BuildEventContext.TargetId, out var targetInfo))
                {
                    throw new LoggerException(Resources.CannotFindTarget);
                }

                if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
                {
                    throw new LoggerException(Resources.CannotFindTask);
                }

                taskInfo.CommandLineArguments = Intern(taskArgs.CommandLine);
                return;
            }

            AddMessage(args);
        }

        public void MessageRaised(object sender, BuildMessageEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    ProcessMessage(args);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void CustomEventRaised(object sender, CustomBuildEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    ProcessMessage(new BuildMessageEventArgs(
                            Intern(args.Message),
                            Intern(args.HelpKeyword),
                            Intern(args.SenderName),
                            MessageImportance.Low));
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void StatusEventRaised(object sender, BuildStatusEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    switch (args)
                    {
                        case ProjectEvaluationStartedEventArgs projectEvaluationStarted:
                            if (!_buildInfo.EvaluatedProjects.ContainsKey(args.BuildEventContext.EvaluationId))
                            {
                                _buildInfo.EvaluatedProjects[args.BuildEventContext.EvaluationId] =
                                    new EvaluatedProjectInfo { Name = projectEvaluationStarted.ProjectFile };
                            }
                            break;

                        case ProjectEvaluationFinishedEventArgs _:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void WarningRaised(object sender, BuildWarningEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var warning = new DiagnosticInfo
                    {
                        IsError = false,
                        Text = Intern(args.Message),
                        Timestamp = args.Timestamp,
                        Code = Intern(args.Code),
                        ColumnNumber = args.ColumnNumber,
                        EndColumnNumber = args.EndColumnNumber,
                        EndLineNumber = args.EndLineNumber,
                        LineNumber = args.LineNumber,
                        File = Intern(args.File),
                        ProjectFile = Intern(args.ProjectFile),
                        Subcategory = Intern(args.Subcategory),
                        ProjectParent = args.BuildEventContext.ProjectContextId,
                        TargetParent = args.BuildEventContext.TargetId,
                        TaskParent = args.BuildEventContext.TaskId
                    };

                    _diagnostics.Add(warning);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void ErrorRaised(object sender, BuildErrorEventArgs args)
        {
            if (args == null || _done)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var error = new DiagnosticInfo
                    {
                        IsError = true,
                        Text = Intern(args.Message),
                        Timestamp = args.Timestamp,
                        Code = Intern(args.Code),
                        ColumnNumber = args.ColumnNumber,
                        EndColumnNumber = args.EndColumnNumber,
                        EndLineNumber = args.EndLineNumber,
                        LineNumber = args.LineNumber,
                        File = Intern(args.File),
                        ProjectFile = Intern(args.ProjectFile),
                        Subcategory = Intern(args.Subcategory),
                        ProjectParent = args.BuildEventContext.ProjectContextId,
                        TargetParent = args.BuildEventContext.TargetId,
                        TaskParent = args.BuildEventContext.TaskId
                    };

                    _diagnostics.Add(error);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception ex)
        {
            lock (_syncLock)
            {
                _diagnostics.Add(new DiagnosticInfo { IsError = true, Text = ex.ToString() });
            }
        }

        private static Item ConstructItem(ItemInfo itemInfo) =>
            new Item(
                itemInfo.Name,
                itemInfo.Metadata.ToImmutableDictionary());

        private static ItemGroup ConstructItemGroup(ItemGroupInfo itemGroupInfo) =>
            new ItemGroup(
                itemGroupInfo.Name,
                itemGroupInfo.Type,
                itemGroupInfo.Items == null
                    ? ImmutableList<Item>.Empty
                    : itemGroupInfo.Items.Select(ConstructItem).ToImmutableList());

        private static EvaluatedProject ConstructEvaluatedProject(EvaluatedProjectInfo evaluatedProjectInfo) =>
            new EvaluatedProject(
                evaluatedProjectInfo.Name,
                evaluatedProjectInfo.Messages.ToImmutableList()
            );

        private static Project ConstructProject(ProjectInfo projectInfo) =>
            new Project(
                projectInfo.NodeId,
                projectInfo.Name,
                projectInfo.ProjectFile,
                projectInfo.GlobalProperties,
                projectInfo.Properties,
                projectInfo.ItemGroups == null
                    ? ImmutableList<ItemGroup>.Empty
                    : projectInfo.ItemGroups.Select(ConstructItemGroup).ToImmutableList(),
                projectInfo.TargetNames?.ToImmutableArray() ?? ImmutableArray<string>.Empty,
                projectInfo.ToolsVersion,
                projectInfo.Messages.ToImmutableList(),
                projectInfo.StartTime,
                projectInfo.EndTime,
                projectInfo.Succeeded);

        private Build ConstructBuild()
        {
            var evaluatedProjects = ImmutableList<EvaluatedProject>.Empty;

            if (_buildInfo.EvaluatedProjects.Count > 0)
            {
                evaluatedProjects = evaluatedProjects.AddRange(_buildInfo.EvaluatedProjects.Values.Select(ConstructEvaluatedProject));
            }

            return new Build(
                _buildInfo.Messages.ToImmutableList(),
                _buildInfo.StartTime,
                _buildInfo.EndTime,
                ConstructProject(_projectInfos.Values.Single(p => p.ParentProject <= 0)),
                _buildInfo.Succeeded,
                _buildInfo.Environment,
                evaluatedProjects);
        }

        public Build Finish()
        {
            if (_done)
            {
                throw new InvalidOperationException();
            }

            _done = true;
            return ConstructBuild();
        }
    }
}