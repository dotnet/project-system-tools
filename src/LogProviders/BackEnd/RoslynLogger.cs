﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.BackEnd
{
    internal class RoslynLogger
    {
        private static readonly ImmutableHashSet<string> RoslynEventSet;

        private readonly BackEndBuildTableDataSource _dataSource;

        // lazily set but once set, will never change again
        private Action<TraceSource> _setLoggerCall;
        private Action<TraceSource> _removeLoggerCall;

        // mutable state
        private Build _build;
        private TraceSource _roslynTraceSource;

        static RoslynLogger()
        {
            RoslynEventSet = ImmutableHashSet.Create(
                "WorkCoordinator_DocumentWorker_Enqueue",
                "WorkCoordinator_ProcessProjectAsync",
                "WorkCoordinator_ProcessDocumentAsync",
                "WorkCoordinator_SemanticChange_Enqueue",
                "WorkCoordinator_SemanticChange_EnqueueFromMember",
                "WorkCoordinator_SemanticChange_EnqueueFromType",
                "WorkCoordinator_SemanticChange_FullProjects",
                "WorkCoordinator_Project_Enqueue",
                "WorkCoordinator_AsyncWorkItemQueue_LastItem",
                "WorkCoordinator_AsyncWorkItemQueue_FirstItem",
                "WorkCoordinator_ActiveFileEnqueue",
                "WorkCoordinator_WaitForHigherPriorityOperationsAsync",
                "WorkCoordinator_SolutionCrawlerOption",

                "Diagnostics_SyntaxDiagnostic",
                "Diagnostics_SemanticDiagnostic",
                "Diagnostics_ProjectDiagnostic",
                "Diagnostics_DocumentReset",
                "Diagnostics_DocumentOpen",
                "Diagnostics_RemoveDocument",
                "Diagnostics_RemoveProject",
                "Diagnostics_DocumentClose",

                "GlobalOperationRegistration",
                "IntellisenseBuild_Failed",
                "LiveTableDataSource_OnDiagnosticsUpdated",
                "MetadataOnlyImage_EmitFailure",
                "ExternalErrorDiagnosticUpdateSource_AddError",
                "DiagnosticIncrementalAnalyzer_SynchronizeWithBuildAsync",
                "StorageDatabase_Exceptions");
        }

        public RoslynLogger(BackEndBuildTableDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public bool Supported => EnsureRoslynLogger();

        public void Start()
        {
            EnsureRoslynLogger();

            if (_setLoggerCall == null)
            {
                return;
            }

            // add live entry to the toolwindow
            _build = new Build(BuildLoggingResources.ProjectSolutionWide, Array.Empty<string>(), Array.Empty<string>(), BuildType.Roslyn, DateTime.Now);
            _dataSource.AddEntry(_build);

            _roslynTraceSource = new TraceSource("RoslynTraceSource", SourceLevels.Verbose);

            // add our own listener
            _roslynTraceSource.Listeners.Clear();
            _roslynTraceSource.Listeners.Add(new RoslynTraceListener(RoslynEventSet));

            _setLoggerCall(_roslynTraceSource);
        }

        public void Stop()
        {
            if (_roslynTraceSource == null)
            {
                return;
            }

            _roslynTraceSource.Flush();
            _roslynTraceSource.Close();

            _removeLoggerCall(_roslynTraceSource);

            var listener = (RoslynTraceListener)_roslynTraceSource.Listeners[0];
            _roslynTraceSource = null;

            _build.Finish(succeeded: true, time: DateTime.Now);
            _build.SetLogPath(listener.LogPath);

            _dataSource.NotifyChange();

            listener.Dispose();
        }

        private bool EnsureRoslynLogger()
        {
            if (_setLoggerCall == null)
            {
                try
                {
                    var assembly = GetAssembly("Microsoft.VisualStudio.LanguageServices");
                    if (assembly == null)
                    {
                        return false;
                    }

                    var type = assembly.GetType("Microsoft.VisualStudio.LanguageServices.RoslynActivityLogger");
                    if (type == null)
                    {
                        return false;
                    }

                    var setLoggerMethodInfo = type.GetMethod("SetLogger", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (setLoggerMethodInfo != null)
                    {
                        _setLoggerCall = (Action<TraceSource>)setLoggerMethodInfo.CreateDelegate(typeof(Action<TraceSource>));
                    }

                    var removeLoggerMethodInfo = type.GetMethod("RemoveLogger", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (removeLoggerMethodInfo != null)
                    {
                        _removeLoggerCall = (Action<TraceSource>)removeLoggerMethodInfo.CreateDelegate(typeof(Action<TraceSource>));
                    }
                }
                catch
                {
                    return false;
                }
            }

            return _setLoggerCall != null;
        }

        private static Assembly GetAssembly(string assemblyName) =>
            (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where assembly.FullName.IndexOf(assemblyName, StringComparison.OrdinalIgnoreCase) >= 0
                let current = assembly.GetName()
                where string.Equals(current.Name, assemblyName, StringComparison.OrdinalIgnoreCase)
                select assembly).FirstOrDefault();

        private class RoslynTraceListener : TraceListener
        {
            private const int LogEventId = 0;
            private const int StartEventId = 1;
            private const int EndEventId = 2;

            private readonly IImmutableSet<string> _set;
            private readonly StreamWriter _writer;

            public RoslynTraceListener(IImmutableSet<string> roslynEvents)
            {
                LogPath = Path.Combine(Path.GetTempPath(), $"RoslynLog-{Guid.NewGuid()}.txt");

                _set = roslynEvents;

                // if there is an issue, this will crash VS. we can swallow, but then, user won't know
                // why logging didn't work.
                _writer = new StreamWriter(LogPath);
            }

            public readonly string LogPath;
            public override bool IsThreadSafe => true;

            public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
            {
                var functionId = (string)data[0];
                if (!_set.Contains(functionId))
                {
                    return;
                }

                switch (id)
                {
                    case LogEventId:
                        AddLog($"(Log) [{functionId}] [{(string)data[1] ?? "N/A"}]");
                        break;
                    case StartEventId:
                        AddLog($"(Start [{(int)data[1]}]) [{functionId}]");
                        break;
                    case EndEventId:
                        AddLog($"(End   [{(int)data[1]}]) [{functionId}] cancellation:{(bool)data[2]}, delta:{(int)data[3]}, [{(string)data[4] ?? "N/A"}]");
                        break;
                    default:
                        throw new NotSupportedException("shouldn't reach here");
                }
            }

            private void AddLog(string message)
            {
                try
                {
                    lock (_writer)
                    {
                        _writer.WriteLine(message);
                    }
                }
                catch
                {
                    // don't crash VS
                }
            }

            public override void Write(string message)
            {
                throw new NotSupportedException("this should never be called");
            }

            public override void WriteLine(string message)
            {
                throw new NotSupportedException("this should never be called");
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                try
                {
                    lock (_writer)
                    {
                        _writer.Flush();
                        _writer.Dispose();
                    }
                }
                catch
                {
                    // fail to dispose. 
                    // fine
                }
            }
        }
    }
}
