// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class ModelBuilder
    {
        private static readonly char[] Space = { ' ' };

        private static readonly Regex UsingTaskRegex = new Regex("Using \"(?<task>.+)\" task from (assembly|the task factory) \"(?<assembly>.+)\"\\.", RegexOptions.Compiled);

        public Build Build { get; }

        private readonly ConcurrentDictionary<int, Project> _projectIdToProjectMap = new ConcurrentDictionary<int, Project>();

        private readonly ConcurrentDictionary<string, string> _taskToAssemblyMap =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<Project, ProjectInstance> _projectToProjectInstanceMap =
            new ConcurrentDictionary<Project, ProjectInstance>();

        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        private readonly object _syncLock = new object();

        private readonly HashSet<string> _evaluationMessagesAlreadySeen = new HashSet<string>(StringComparer.Ordinal);

        private Folder _evaluationFolder;

        public ModelBuilder(IEventSource eventSource)
        {
            Build = new Build { Name = "Build" };

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

        public void BuildStarted(object sender, BuildStartedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    Build.StartTime = args.Timestamp;
                    Build.Environment = args.BuildEnvironment.ToImmutableDictionary();

                    _evaluationFolder = Build.GetOrCreateNodeWithName<Folder>("Evaluation");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void CalculateTargetGraph(Project project)
        {
            if (!_projectToProjectInstanceMap.TryGetValue(project, out var projectInstance))
            {
                // if for some reason we weren't able to fish out the project instance from MSBuild,
                // just add all orphans directly to the project
                var unparented = project.GetUnparentedTargets();
                foreach (var orphan in unparented)
                {
                    project.TryAddTarget(orphan);
                }

                return;
            }

            var targetGraph = new TargetGraph(projectInstance);

            IEnumerable<Target> unparentedTargets;
            while ((unparentedTargets = project.GetUnparentedTargets()).Any())
            {
                foreach (var unparentedTarget in unparentedTargets)
                {
                    var parents = targetGraph.GetDependents(unparentedTarget.Name);
                    if (parents != null && parents.Any())
                    {
                        foreach (var parent in parents)
                        {
                            var parentNode = project.GetOrAddTargetByName(parent);
                            if (parentNode == null || parentNode.Id == -1 && !parentNode.HasChildren)
                            {
                                continue;
                            }
                            parentNode.TryAddTarget(unparentedTarget);
                            break;
                        }
                    }

                    project.TryAddTarget(unparentedTarget);
                }
            }

            project.VisitAllChildren<Target>(t =>
            {
                if (t.Project != project)
                {
                    return;
                }
                var dependencies = targetGraph.GetDependencies(t.Name);
                if (dependencies != null && dependencies.Any())
                {
                    t.DependsOnTargets = Intern(string.Join(",", dependencies));
                }
            });
        }

        public void BuildFinished(object sender, BuildFinishedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    Build.EndTime = args.Timestamp;
                    Build.Succeeded = args.Succeeded;

                    Build.VisitAllChildren<Project>(CalculateTargetGraph);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void ProjectStarted(object sender, ProjectStartedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    Project parent = null;

                    var parentProjectId = args.ParentProjectBuildEventContext.ProjectContextId;
                    if (parentProjectId > 0)
                    {
                        parent = GetOrAddProject(parentProjectId);
                    }

                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    project.NodeId = args.BuildEventContext.NodeId;

                    if (project.Name == null)
                    {
                        project.StartTime = args.Timestamp;
                        project.Name = Intern(args.Message);
                        project.ProjectFile = Intern(args.ProjectFile);

                        if (args.GlobalProperties != null)
                        {
                            project.GlobalProperties = ImmutableDictionary<string, string>.Empty.AddRange(
                                args.GlobalProperties.Select(d => new KeyValuePair<string, string>(Intern(d.Key), Intern(d.Value)))
                            );
                        }

                        if (args.Properties != null)
                        {
                            project.Properties = ImmutableDictionary<string, string>.Empty.AddRange(
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
                            RetrieveProjectInstance(project, args);

                            var items = project.GetOrCreateNodeWithName<Folder>("Items");
                            foreach (DictionaryEntry kvp in args.Items)
                            {
                                var itemName = Intern(Convert.ToString(kvp.Key));
                                var itemGroup = items.GetOrCreateNodeWithName<Folder>(itemName);

                                var item = new Item();

                                if (!(kvp.Value is ITaskItem taskItem))
                                {
                                    continue;
                                }

                                item.Text = Intern(taskItem.ItemSpec);
                                foreach (DictionaryEntry metadataName in taskItem.CloneCustomMetadata())
                                {
                                    item.AddChild(new Metadata
                                    {
                                        Name = Intern(Convert.ToString(metadataName.Key)),
                                        Value = Intern(Convert.ToString(metadataName.Value))
                                    });
                                }

                                itemGroup.AddChild(item);
                            }
                        }
                    }

                    if (parent != null)
                    {
                        parent.AddChild(project);
                    }
                    else
                    {
                        // This is a "Root" project (no parent project).
                        Build.AddChild(project);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void ProjectFinished(object sender, ProjectFinishedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    project.EndTime = args.Timestamp;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TargetStarted(object sender, TargetStartedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    var targetName = Intern(args.TargetName);
                    var target = project.CreateTarget(targetName, args.BuildEventContext.TargetId);
                    target.NodeId = args.BuildEventContext.NodeId;
                    target.StartTime = args.Timestamp;

                    if (!string.IsNullOrEmpty(args.ParentTarget))
                    {
                        var parentTarget = project.GetOrAddTargetByName(Intern(args.ParentTarget));
                        parentTarget.TryAddTarget(target);
                        project.TryAddTarget(parentTarget);
                    }
                    else
                    {
                        project.TryAddTarget(target);
                    }

                    target.SourceFilePath = Intern(args.TargetFile);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TargetFinished(object sender, TargetFinishedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    var target = project.GetTarget(args.TargetName, args.BuildEventContext.TargetId);

                    target.EndTime = args.Timestamp;
                    target.Succeeded = args.Succeeded;

                    if (args.TargetOutputs == null)
                    {
                        return;
                    }

                    var targetOutputs = new List<Item>();

                    foreach (ITaskItem targetOutput in args.TargetOutputs)
                    {
                        var item = new Item { Text = Intern(targetOutput.ItemSpec) };
                        foreach (DictionaryEntry metadata in targetOutput.CloneCustomMetadata())
                        {
                            var metadataNode = new Metadata
                            {
                                Name = Intern(Convert.ToString(metadata.Key)),
                                Value = Intern(Convert.ToString(metadata.Value))
                            };
                            item.AddChild(metadataNode);
                        }

                        targetOutputs.Add(item);
                    }

                    target.OutputItems = targetOutputs.ToImmutableList();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TaskStarted(object sender, TaskStartedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    var target = project.GetTargetById(args.BuildEventContext.TargetId);

                    var taskName = Intern(args.TaskName);
                    var assembly = Intern(_taskToAssemblyMap.TryGetValue(taskName, out var assembly1) ? assembly1 : string.Empty);
                    var taskId = args.BuildEventContext.TaskId;
                    var startTime = args.Timestamp;

                    var result = taskName == "Copy" ? new CopyTask() : new Task();

                    result.Name = taskName;
                    result.Id = taskId;
                    result.NodeId = args.BuildEventContext.NodeId;
                    result.StartTime = startTime;
                    result.FromAssembly = assembly;
                    result.SourceFilePath = Intern(args.TaskFile);
                    target.AddChild(result);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void TaskFinished(object sender, TaskFinishedEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                    var target = project.GetTargetById(args.BuildEventContext.TargetId);
                    var task = target.GetTaskById(args.BuildEventContext.TaskId);

                    task.EndTime = args.Timestamp;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public void MessageRaised(object sender, BuildMessageEventArgs args)
        {
            if (args == null)
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
            if (args == null)
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
            if (args == null)
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
                            {
                                var projectName = projectEvaluationStarted.ProjectFile;
                                var project = _evaluationFolder.GetOrCreateNodeWithName<Project>(projectName);
                                project.Id = args.BuildEventContext.ProjectContextId;
                                break;
                            }

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
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var parent = FindParent(args.BuildEventContext) ?? Build;

                    var warnings = parent.GetOrCreateNodeWithName<Folder>("Warnings");
                    var warning = new Diagnostic
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
                        Subcategory = Intern(args.Subcategory)
                    };
                    warnings.AddChild(warning);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private NodeWithName FindParent(BuildEventContext buildEventContext)
        {
            var project = GetOrAddProject(buildEventContext.ProjectContextId);
            NodeWithName result = project;
            if (buildEventContext.TargetId <= 0)
            {
                return result;
            }

            var target = project.GetTargetById(buildEventContext.TargetId);
            if (target == null)
            {
                return result;
            }

            result = target;
            if (buildEventContext.TaskId <= 0)
            {
                return result;
            }

            var task = target.GetTaskById(buildEventContext.TaskId);
            if (task != null)
            {
                result = task;
            }

            return result;
        }

        public void ErrorRaised(object sender, BuildErrorEventArgs args)
        {
            if (args == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                lock (_syncLock)
                {
                    var parent = FindParent(args.BuildEventContext) ?? Build;

                    var errors = parent.GetOrCreateNodeWithName<Folder>("Errors");
                    var error = new Diagnostic
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
                        Subcategory = Intern(args.Subcategory)
                    };
                    errors.AddChild(error);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception ex)
        {
            try
            {
                lock (_syncLock)
                {
                    Build.AddChild(new Diagnostic { IsError = true, Text = ex.ToString() });
                }
            }
            catch (Exception)
            {
            }
        }

        private Project GetOrAddProject(int projectId) => _projectIdToProjectMap.GetOrAdd(projectId, id => new Project { Id = id });

        private void RetrieveProjectInstance(Project project, ProjectStartedEventArgs args)
        {
            if (_projectToProjectInstanceMap.ContainsKey(project))
            {
                return;
            }

            object GetField(object instance, string fieldName) =>
                instance?
                    .GetType()
                    .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?
                    .GetValue(instance);

            var projectItemInstanceEnumeratorProxy = args?.Items;
            if (projectItemInstanceEnumeratorProxy == null)
            {
                return;
            }

            var backingItems = GetField(projectItemInstanceEnumeratorProxy, "_backingItems");
            if (backingItems == null)
            {
                return;
            }

            var backingEnumerable = GetField(backingItems, "_backingEnumerable");
            if (backingEnumerable == null)
            {
                return;
            }

            if (!(GetField(backingEnumerable, "_nodes") is IDictionary nodes) || nodes.Count == 0)
            {
                return;
            }

            if (!(nodes.Keys.OfType<object>().FirstOrDefault() is ProjectItemInstance projectItemInstance))
            {
                return;
            }

            var projectInstance = projectItemInstance.Project;
            if (projectInstance == null)
            {
                return;
            }

            _projectToProjectInstanceMap[project] = projectInstance;
        }

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
                        AddItemGroup(args, itemGroupIncludeMessagePrefix, new AddItem());
                        return;
                    }
                    break;

                case 'O':
                    if (message.StartsWith(outputItemsMessagePrefix))
                    {
                        var task = GetTask(args);
                        var folder = task.GetOrCreateNodeWithName<Folder>("OutputItems");
                        var parameter = ParsePropertyOrItemList(message, outputItemsMessagePrefix);
                        folder.AddChild(parameter);
                        return;
                    }

                    if (message.StartsWith(outputPropertyMessagePrefix))
                    {
                        var task = GetTask(args);
                        var folder = task.GetOrCreateNodeWithName<Folder>("OutputProperties");
                        var parameter = ParsePropertyOrItemList(message, outputPropertyMessagePrefix);
                        folder.AddChild(parameter);
                        return;
                    }
                    break;

                case 'R':
                    if (message.StartsWith(itemGroupRemoveMessagePrefix))
                    {
                        AddItemGroup(args, itemGroupRemoveMessagePrefix, new RemoveItem());
                        return;
                    }
                    break;

                case 'S':
                    if (message.StartsWith(propertyGroupMessagePrefix))
                    {
                        AddPropertyGroup(args, propertyGroupMessagePrefix);
                        return;
                    }
                    break;

                case 'T':
                    if (message.StartsWith(taskParameterMessagePrefix))
                    {
                        var task = GetTask(args);
                        var folder = task.GetOrCreateNodeWithName<Folder>("Parameters");
                        var parameter = ParsePropertyOrItemList(message, taskParameterMessagePrefix);
                        folder.AddChild(parameter);
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
                        _taskToAssemblyMap.GetOrAdd(taskName, t => assembly);
                        return;
                    }

                    break;
            }

            if (args is TaskCommandLineEventArgs taskArgs)
            {
                var project = GetOrAddProject(taskArgs.BuildEventContext.ProjectContextId);
                var target = project.GetTargetById(taskArgs.BuildEventContext.TargetId);
                var task = target.GetTaskById(taskArgs.BuildEventContext.TaskId);

                task.CommandLineArguments = Intern(taskArgs.CommandLine);
                return;
            }

            // Just the generic log message or something we currently don't handle in the object model.
            AddMessage(args, message);
        }

        private Task GetTask(BuildMessageEventArgs args)
        {
            var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
            var target = project.GetTargetById(args.BuildEventContext.TargetId);
            var task = target.GetTaskById(args.BuildEventContext.TaskId);
            return task;
        }

        /// <summary>
        /// Handles BuildMessage event when a property discovery/evaluation is logged.
        /// </summary>
        private void AddPropertyGroup(BuildMessageEventArgs args, string prefix)
        {
            var message = args.Message.Substring(prefix.Length);

            var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
            var target = project.GetTargetById(args.BuildEventContext.TargetId);

            var kvp = Utilities.ParseNameValue(message);
            target.AddChild(new Property
            {
                Name = Intern(kvp.Key),
                Value = Intern(kvp.Value)
            });
        }

        /// <summary>
        /// Handles BuildMessage event when an ItemGroup discovery/evaluation is logged.
        /// </summary>
        private void AddItemGroup(BuildMessageEventArgs args, string prefix, NodeWithName containerNode)
        {
            var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
            var target = project.GetTargetById(args.BuildEventContext.TargetId);
            var itemGroup = ParsePropertyOrItemList(args.Message, prefix);
            if (itemGroup is Property property)
            {
                itemGroup = new Item
                {
                    Name = property.Name,
                    Text = property.Value
                };
                containerNode.Name = Intern(property.Name);
            }

            containerNode.AddChild(itemGroup);
            target.AddChild(containerNode);
        }

        private static bool IsEvaluationMessage(string message)
        {
            return message.StartsWith("Search paths being used")
                   || message.StartsWith("Overriding target")
                   || message.StartsWith("Trying to import")
                   || message.StartsWith("Property reassignment")
                   || message.StartsWith("Importing project")
                   || message.StartsWith("Project \"") && message.Contains("was not imported by");
        }

        /// <summary>
        /// Handles a generic BuildMessage event and assigns it to the appropriate logging node.
        /// </summary>
        private void AddMessage(LazyFormattedBuildEventArgs args, string message)
        {
            message = Intern(message);

            NodeWithName node = null;
            var messageNode = new Message { Text = message };
            object nodeToAdd = messageNode;

            if (args.BuildEventContext?.TaskId > 0)
            {
                node = GetOrAddProject(args.BuildEventContext.ProjectContextId)
                    .GetTargetById(args.BuildEventContext.TargetId)
                    .GetTaskById(args.BuildEventContext.TaskId);
                var task = (Task)node;
                switch (task.Name)
                {
                    case "ResolveAssemblyReference":
                        {
                            var inputs = task.GetOrCreateNodeWithName<Folder>("Inputs");
                            var results = task.FindChild<Folder>("Results");
                            node = results ?? inputs;

                            if (message.StartsWith("    "))
                            {
                                message = message.Substring(4);

                                var parameter = node.FindLastChild<Parameter>();
                                if (parameter != null)
                                {
                                    if (string.IsNullOrWhiteSpace(message))
                                    {
                                        return;
                                    }

                                    node = parameter;

                                    if (message.StartsWith("    "))
                                    {
                                        message = message.Substring(4);

                                        var lastItem = parameter.FindLastChild<Item>();

                                        // only indent if it's not a "For SearchPath..." message - that one needs to be directly under parameter
                                        if (lastItem != null && !message.StartsWith("For SearchPath"))
                                        {
                                            node = lastItem;
                                        }
                                    }

                                    if (string.IsNullOrEmpty(message))
                                    {
                                        return;
                                    }

                                    if (message.IndexOf('=') != -1)
                                    {
                                        var kvp = Utilities.ParseNameValue(message);
                                        node.AddChild(new Metadata
                                        {
                                            Name = Intern(kvp.Key.TrimEnd(Space)),
                                            Value = Intern(kvp.Value.TrimStart(Space))
                                        });
                                    }
                                    else
                                    {
                                        node.AddChild(new Item
                                        {
                                            Text = Intern(message)
                                        });
                                    }

                                    return;
                                }
                            }
                            else
                            {
                                if (results == null)
                                {
                                    var isResult = message.StartsWith("Unified primary reference ") ||
                                                   message.StartsWith("Primary reference ") ||
                                                   message.StartsWith("Dependency ") ||
                                                   message.StartsWith("Unified Dependency ");

                                    if (isResult)
                                    {
                                        results = task.GetOrCreateNodeWithName<Folder>("Results");
                                        node = results;
                                    }
                                    else
                                    {
                                        node = inputs;
                                    }
                                }
                                else
                                {
                                    node = results;
                                }

                                node.GetOrCreateNodeWithName<Parameter>(Intern(message.TrimEnd(':')));
                                return;
                            }
                            break;
                        }
                    case "MSBuild":
                        {
                            if (message.StartsWith("Global Properties") ||
                                message.StartsWith("Additional Properties") ||
                                message.StartsWith("Overriding Global Properties") ||
                                message.StartsWith("Removing Properties"))
                            {
                                node.GetOrCreateNodeWithName<Folder>(message);
                                return;
                            }

                            node = node.FindLastChild<Folder>();
                            if (message[0] == ' ' && message[1] == ' ')
                            {
                                message = message.Substring(2);
                            }

                            var kvp = Utilities.ParseNameValue(message);
                            if (kvp.Value == "")
                            {
                                nodeToAdd = new Item
                                {
                                    Text = Intern(kvp.Key)
                                };
                            }
                            else
                            {
                                nodeToAdd = new Property
                                {
                                    Name = Intern(kvp.Key),
                                    Value = Intern(kvp.Value)
                                };
                            }
                            break;
                        }
                }
            }
            else if (args.BuildEventContext?.TargetId > 0)
            {
                node = GetOrAddProject(args.BuildEventContext.ProjectContextId)
                    .GetTargetById(args.BuildEventContext.TargetId);
            }
            else if (args.BuildEventContext?.ProjectContextId > 0)
            {
                var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
                node = project;

                if (message.StartsWith("Target") && message.Contains("skipped"))
                {
                    var targetName = Intern(Utilities.ParseQuotedSubstring(message));
                    if (targetName != null)
                    {
                        node = project.GetOrAddTargetByName(targetName);
                    }
                }
            }
            else if (args.BuildEventContext != null && args.BuildEventContext.EvaluationId != int.MinValue)
            {
                var project = _evaluationFolder.FindChild<Project>(p => p.Id == args.BuildEventContext.EvaluationId);
                node = project;

                if (node?.FindChild<Message>(message) != null)
                {
                    // avoid duplicate messages
                    return;
                }
            }

            if (node == null)
            {
                node = Build;

                if (IsEvaluationMessage(message))
                {
                    if (!_evaluationMessagesAlreadySeen.Add(message))
                    {
                        return;
                    }

                    node = _evaluationFolder;
                }
                else if (message.StartsWith("The target") && message.Contains("does not exist in the project, and will be ignored"))
                {
                    node = _evaluationFolder;
                }
                else if (args.BuildEventContext != null &&
                         args.BuildEventContext.NodeId == 0 &&
                         args.BuildEventContext.ProjectContextId == 0 &&
                         args.BuildEventContext.ProjectInstanceId == 0 &&
                         args.BuildEventContext.TargetId == 0 &&
                         args.BuildEventContext.TaskId == 0)
                {
                    // must be Detailed Build Summary
                    // https://github.com/Microsoft/msbuild/blob/master/src/XMakeBuildEngine/BackEnd/Components/Scheduler/Scheduler.cs#L509
                    node = Build.GetOrCreateNodeWithName<Folder>("DetailedSummary");
                }
            }

            node.AddChild(nodeToAdd);
        }

        private object ParsePropertyOrItemList(string message, string prefix)
        {
            if (!Utilities.ContainsLineBreak(message))
            {
                var nameValue = Utilities.ParseNameValue(message, trimFromStart: prefix.Length);
                var property = new Property
                {
                    Name = Intern(nameValue.Key),
                    Value = Intern(nameValue.Value)
                };
                return property;
            }

            message = message.Replace("\r\n", "\n");
            message = message.Replace('\r', '\n');
            var lines = message.Split('\n');

            var parameter = new Parameter();

            if (lines[0].Length > prefix.Length)
            {
                // we have a weird case of multi-line value
                var nameValue = Utilities.ParseNameValue(lines[0].Substring(prefix.Length));

                parameter.Name = Intern(nameValue.Key);

                parameter.AddChild(new Item
                {
                    Text = Intern(nameValue.Value)
                });

                for (var i = 1; i < lines.Length; i++)
                {
                    parameter.AddChild(new Item
                    {
                        Text = Intern(lines[i])
                    });
                }

                return parameter;
            }

            Item currentItem = null;
            Property currentProperty = null;
            foreach (var line in lines)
            {
                var numberOfLeadingSpaces = Utilities.GetNumberOfLeadingSpaces(line);
                switch (numberOfLeadingSpaces)
                {
                    case 4:
                        if (line.EndsWith("=", StringComparison.Ordinal))
                        {
                            parameter.Name = Intern(line.Substring(4, line.Length - 5));
                        }
                        break;
                    case 8:
                        if (line.IndexOf('=') != -1)
                        {
                            var kvp = Utilities.ParseNameValue(line.Substring(8));
                            currentProperty = new Property
                            {
                                Name = Intern(kvp.Key),
                                Value = Intern(kvp.Value)
                            };
                            parameter.AddChild(currentProperty);
                            currentItem = null;
                        }
                        else
                        {
                            currentItem = new Item
                            {
                                Text = Intern(line.Substring(8))
                            };
                            parameter.AddChild(currentItem);
                            currentProperty = null;
                        }
                        break;
                    case 16:
                        var currentLine = line.Substring(16);
                        if (currentItem != null)
                        {
                            if (!currentLine.Contains("="))
                            {
                                // must be a continuation of the metadata value from the previous line
                                if (currentItem.HasChildren)
                                {
                                    if (currentItem.Children[currentItem.Children.Count - 1] is Metadata metadata)
                                    {
                                        metadata.Value = Intern((metadata.Value ?? "") + currentLine);
                                    }
                                }
                            }
                            else
                            {
                                var nameValue = Utilities.ParseNameValue(currentLine);
                                var metadata = new Metadata
                                {
                                    Name = Intern(nameValue.Key),
                                    Value = Intern(nameValue.Value)
                                };
                                currentItem.AddChild(metadata);
                            }
                        }
                        break;
                    default:
                        if (numberOfLeadingSpaces == 0 && line == prefix)
                        {
                            continue;
                        }

                        // must be a continuation of a multi-line value
                        if (currentProperty != null)
                        {
                            currentProperty.Value += "\n" + line;
                        }
                        else if (currentItem != null && currentItem.HasChildren)
                        {
                            if (currentItem.Children[currentItem.Children.Count - 1] is Metadata metadata)
                            {
                                metadata.Value = (metadata.Value ?? "") + line;
                            }
                        }
                        break;
                }
            }

            return parameter;
        }
    }
}