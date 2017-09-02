using System;
using System.Collections;
using System.IO;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    public sealed class RawLogger : ILogger
    {
        private StreamWriter _streamWriter;
        private JsonWriter _jsonWriter;
        private JsonSerializer _jsonSerializer;

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Diagnostic;

        public string Parameters { get; set; }

        private string FilePath { get; set; }

        private void ProcessParameters()
        {
            if (Parameters != null)
            {
                var parameters = Parameters.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var parameter in parameters)
                {
                    if (parameter.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        FilePath = parameter;
                        if (FilePath.StartsWith("LogFile=", StringComparison.OrdinalIgnoreCase))
                        {
                            FilePath = FilePath.Substring("LogFile=".Length);
                        }

                        FilePath = parameter.TrimStart('"').TrimEnd('"');
                    }
                    else
                    {
                        throw new LoggerException();
                    }
                }
            }


            if (FilePath == null)
            {
                FilePath = "msbuild.json";
            }

            try
            {
                FilePath = Path.GetFullPath(FilePath);
            }
            catch (Exception)
            {
                throw new LoggerException();
            }
        }

        public void Initialize(IEventSource eventSource)
        {
            ProcessParameters();
            try
            {
                _streamWriter = new StreamWriter(FilePath);
                _jsonSerializer = JsonSerializer.Create();
                _jsonWriter = new JsonTextWriter(_streamWriter) {Formatting = Formatting.Indented};
                _jsonWriter.WriteStartArray();
            }
            catch (Exception)
            {
                throw new LoggerException();
            }
            eventSource.AnyEventRaised += AnyEvent;
        }

        private void WriteProperty<T>(string propertyName, T value)
        {
            _jsonWriter.WritePropertyName(propertyName);
            _jsonWriter.WriteValue(value);
        }

        private void SerializeProperty(string propertyName, object value)
        {
            _jsonWriter.WritePropertyName(propertyName);
            _jsonSerializer.Serialize(_jsonWriter, value);
        }

        private void WriteBuildEventContext(string propertyName, BuildEventContext context)
        {
            _jsonWriter.WritePropertyName(propertyName);

            if (context != null)
            {
                _jsonWriter.WriteStartObject();
                WriteProperty("evaluationId", context.EvaluationId);
                WriteProperty("nodeId", context.NodeId);
                WriteProperty("targetId", context.TargetId);
                WriteProperty("projectContextId", context.ProjectContextId);
                WriteProperty("taskId", context.TaskId);
                WriteProperty("projectInstanceId", context.ProjectInstanceId);
                WriteProperty("submissionId", context.SubmissionId);
                WriteProperty("buildRequestId", context.BuildRequestId);
                _jsonWriter.WriteEndObject();
            }
            else
            {
                _jsonWriter.WriteNull();
            }
        }

        private void WriteProperties(IEnumerable properties)
        {
            _jsonWriter.WritePropertyName("properties");

            if (properties == null)
            {
                _jsonWriter.WriteNull();
            }
            else
            {
                _jsonWriter.WriteStartObject();

                foreach (DictionaryEntry entry in properties)
                {
                    _jsonWriter.WritePropertyName(Convert.ToString(entry.Key));
                    _jsonWriter.WriteValue(Convert.ToString(entry.Value));
                }

                _jsonWriter.WriteEndObject();
            }
        }

        private void WriteTaskItem(ITaskItem taskItem)
        {
            WriteProperty("evaluatedInclude", taskItem.ItemSpec);

            _jsonWriter.WritePropertyName("metadata");
            _jsonWriter.WriteStartObject();
            foreach (DictionaryEntry metadataName in taskItem.CloneCustomMetadata())
            {
                _jsonWriter.WritePropertyName(Convert.ToString(metadataName.Key));
                _jsonWriter.WriteValue(Convert.ToString(metadataName.Value));
            }
        }

        private void WriteItems(IEnumerable items)
        {
            _jsonWriter.WritePropertyName("items");

            if (items == null)
            {
                _jsonWriter.WriteNull();
            }
            else
            {
                _jsonWriter.WriteStartObject();

                foreach (DictionaryEntry entry in items)
                {
                    _jsonWriter.WritePropertyName(Convert.ToString(entry.Key));

                    if (!(entry.Value is ITaskItem taskItem))
                    {
                        _jsonWriter.WriteNull();
                    }
                    else
                    {
                        WriteTaskItem(taskItem);
                    }
                }

                _jsonWriter.WriteEndObject();
            }
        }

        private void WriteTargetOutputs(IEnumerable targetOutputs)
        {
            _jsonWriter.WritePropertyName("targetOutputs");

            if (targetOutputs == null)
            {
                _jsonWriter.WriteNull();
            }
            else
            {
                _jsonWriter.WriteStartArray();

                foreach (ITaskItem taskItem in targetOutputs)
                {
                    WriteTaskItem(taskItem);
                }

                _jsonWriter.WriteEndArray();
            }
        }

        private void AnyEvent(object sender, BuildEventArgs e)
        {
            _jsonWriter.WriteStartObject();

            WriteProperty("type", e.GetType().ToString());
            WriteProperty("threadId", e.ThreadId);
            WriteProperty("message", e.Message);
            WriteProperty("helpKeyword", e.HelpKeyword);
            WriteProperty("senderName", e.SenderName);
            WriteBuildEventContext("buildEventContext", e.BuildEventContext);

            switch (e)
            {
                case BuildStartedEventArgs buildStartedEventArgs:
                    SerializeProperty("buildEnvironment", buildStartedEventArgs.BuildEnvironment);
                    break;

                case BuildFinishedEventArgs buildFinishedEventArgs:
                    WriteProperty("succeeded", buildFinishedEventArgs.Succeeded);
                    break;

                case ProjectStartedEventArgs projectStartedEventArgs:
                    WriteProperty("projectId", projectStartedEventArgs.ProjectId);
                    WriteBuildEventContext("parentProjectBuildEventContext", projectStartedEventArgs.ParentProjectBuildEventContext);
                    WriteProperty("projectFile", projectStartedEventArgs.ProjectFile);
                    WriteProperty("targetNames", projectStartedEventArgs.TargetNames);
                    SerializeProperty("globalProperties", projectStartedEventArgs.GlobalProperties);
                    WriteProperty("toolsVersion", projectStartedEventArgs.ToolsVersion);
                    WriteProperties(projectStartedEventArgs.Properties);
                    WriteItems(projectStartedEventArgs.Items);
                    break;

                case ProjectFinishedEventArgs projectFinishedEventArgs:
                    WriteProperty("projectFile", projectFinishedEventArgs.ProjectFile);
                    WriteProperty("succeeded", projectFinishedEventArgs.Succeeded);
                    break;

                case TargetStartedEventArgs targetStartedEventArgs:
                    WriteProperty("targetName", targetStartedEventArgs.TargetName);
                    WriteProperty("parentTarget", targetStartedEventArgs.ParentTarget);
                    WriteProperty("projectFile", targetStartedEventArgs.ProjectFile);
                    WriteProperty("targetFile", targetStartedEventArgs.TargetFile);
                    break;

                case TargetFinishedEventArgs targetFinishedEventArgs:
                    WriteProperty("targetName", targetFinishedEventArgs.TargetName);
                    WriteProperty("projectFile", targetFinishedEventArgs.ProjectFile);
                    WriteProperty("targetFile", targetFinishedEventArgs.TargetFile);
                    WriteProperty("succeeded", targetFinishedEventArgs.Succeeded);
                    WriteTargetOutputs(targetFinishedEventArgs.TargetOutputs);
                    break;

                case TaskStartedEventArgs taskStartedEventArgs:
                    WriteProperty("taskName", taskStartedEventArgs.TaskName);
                    WriteProperty("projectFile", taskStartedEventArgs.ProjectFile);
                    WriteProperty("taskFile", taskStartedEventArgs.TaskFile);
                    break;

                case TaskFinishedEventArgs taskFinishedEventArgs:
                    WriteProperty("taskName", taskFinishedEventArgs.TaskName);
                    WriteProperty("projectFile", taskFinishedEventArgs.ProjectFile);
                    WriteProperty("taskFile", taskFinishedEventArgs.TaskFile);
                    WriteProperty("succeeded", taskFinishedEventArgs.Succeeded);
                    break;

                case BuildMessageEventArgs buildMessageEventArgs:
                    break;

                case BuildWarningEventArgs buildWarningEventArgs:
                    break;

                case BuildErrorEventArgs buildErrorEventArgs:
                    break;

                case CustomBuildEventArgs customBuildEventArgs:
                    break;

                case BuildStatusEventArgs buildStatusEventArgs:
                    break;

                default:
                    _jsonWriter.WriteComment($"Unexpected build event argument type: '{e.GetType()}'.");
                    break;
            }

            _jsonWriter.WriteEndObject();
        }

        public void Shutdown()
        {
            _jsonWriter.WriteEndArray();
            _jsonWriter.Close();
            _streamWriter.Dispose();
        }
    }
}
