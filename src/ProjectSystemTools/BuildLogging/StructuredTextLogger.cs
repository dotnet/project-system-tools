using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    public sealed class StructuredTextLogger : ILogger
    {
        private ModelBuilder _builder;

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
                    if (parameter.EndsWith(".structuredlog", StringComparison.OrdinalIgnoreCase))
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
                FilePath = "msbuild.structuredlog";
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
            _builder = new ModelBuilder(eventSource);
        }

        private static string Escape(string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        literal.Append(c);
                        break;
                }
            }
            return literal.ToString();
        }

        private static void Indent(ref string indent) => indent += "    ";

        private static void Outdent(ref string indent) => indent = indent.Substring(4);

        private static void WriteLine(TextWriter writer, string line, string indent) => writer.WriteLine($"{indent}{line}");

        private static void WriteKeyValue(TextWriter writer, KeyValuePair<string, string> kvp, string indent) =>
            WriteLine(writer, $"{kvp.Key} = {Escape(kvp.Value)}", indent);

        private static void WriteDictionary<TKey, TValue>(TextWriter writer, string name, IDictionary<TKey, TValue> dictionary, Action<TextWriter, KeyValuePair<TKey, TValue>, string> valueWriter, string indent)
        {
            if (dictionary != null && dictionary.Count > 0)
            {
                if (name != null)
                {
                    WriteLine(writer, name, indent);
                }
                WriteLine(writer, "{", indent);
                Indent(ref indent);
                foreach (var kvp in dictionary.OrderBy(kvp => kvp.Key))
                {
                    valueWriter(writer, kvp, indent);
                }
                Outdent(ref indent);
                WriteLine(writer, "}", indent);
            }
        }

        private static void WriteSortedList<T>(TextWriter writer, string name, IReadOnlyList<T> list, Action<TextWriter, T, string> valueWriter, string indent)
        {
            if (list != null && list.Count > 0)
            {
                WriteLine(writer, name, indent);
                WriteLine(writer, "{", indent);
                Indent(ref indent);
                foreach (var value in list.OrderBy(s => s))
                {
                    valueWriter(writer, value, indent);
                }
                Outdent(ref indent);
                WriteLine(writer, "}", indent);
            }
        }

        private static void WriteList<T>(TextWriter writer, string name, IReadOnlyList<T> list, Action<TextWriter, T, string> valueWriter, string indent)
        {
            if (list != null && list.Count > 0)
            {
                WriteLine(writer, name, indent);
                WriteLine(writer, "{", indent);
                Indent(ref indent);
                foreach (var value in list)
                {
                    valueWriter(writer, value, indent);
                }
                Outdent(ref indent);
                WriteLine(writer, "}", indent);
            }
        }

        private static string Succeeded(bool succeeded) => succeeded ? "succeeded" : "failed";

        private static void WriteItem(TextWriter writer, Item item, string indent)
        {
            WriteLine(writer, item.Name, indent);
            WriteDictionary(writer, null, item.Metadata, WriteKeyValue, indent);
        }

        private static void WriteItemGroup(TextWriter writer, ItemGroup itemGroup, string indent)
        {
            var name = itemGroup.Name;

            switch (itemGroup.Type)
            {
                case ItemGroupType.Add:
                    name = $"Add {name}";
                    break;
                case ItemGroupType.Remove:
                    name = $"Remove {name}";
                    break;
            }

            WriteList(writer, name, itemGroup.Items, WriteItem, indent);
        }

        private static void WriteEvaluatedProject(TextWriter writer, EvaluatedProject project, string indent)
        {
            WriteBlockStart(writer, $"Evaluated Project {project.Name}", ref indent);
            WriteList(writer, "Messages", project.Messages, WriteLine, indent);
            WriteBlockEnd(writer, ref indent);
        }

        private static void WriteProject(TextWriter writer, Project project, string indent)
        {
            WriteBlockStart(writer, $"Project {project.Name} ({project.ProjectFile}) {Succeeded(project.Succeeded)}, started {project.StartTime}, elapsed {project.EndTime - project.StartTime}, node {project.NodeId}, tools version {project.ToolsVersion}", ref indent);
            WriteList(writer, "Targets", project.TargetNames, WriteLine, indent);
            WriteDictionary(writer, "GlobalProperties", project.GlobalProperties, WriteKeyValue, indent);
            WriteDictionary(writer, "Properties", project.Properties, WriteKeyValue, indent);
            WriteList(writer, "Items", project.ItemGroups, WriteItemGroup, indent);
            WriteList(writer, "Messages", project.Messages, WriteLine, indent);
            WriteBlockEnd(writer, ref indent);
        }

        private static void WriteBlockStart(TextWriter writer, string line, ref string indent)
        {
            WriteLine(writer, line, indent);
            WriteLine(writer, "{", indent);
            Indent(ref indent);
        }

        private static void WriteBlockEnd(TextWriter writer, ref string indent)
        {
            Outdent(ref indent);
            WriteLine(writer, "}", indent);
        }

        private static void WriteBuild(TextWriter writer, LogModel.Build build)
        {
            var indent = string.Empty;

            WriteBlockStart(writer, $"Build {Succeeded(build.Succeeded)}, started {build.StartTime}, elapsed {build.EndTime - build.StartTime}", ref indent);
            WriteDictionary(writer, "Environment", build.Environment, WriteKeyValue, indent);
            WriteList(writer, "Evaluated Projects", build.EvaluatedProjects, WriteEvaluatedProject, indent);
            WriteProject(writer, build.Project, indent);
            WriteList(writer, "Messages", build.Messages, WriteLine, indent);
            WriteBlockEnd(writer, ref indent);
        }

        public void Shutdown()
        {
            try
            {
                var streamWriter = new StreamWriter(FilePath);
                WriteBuild(streamWriter, _builder.Finish());
                streamWriter.Close();
            }
            catch (Exception)
            {
                throw new LoggerException();
            }
        }
    }
}
