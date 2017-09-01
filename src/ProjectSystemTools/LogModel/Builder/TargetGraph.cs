using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel.Builder
{
    internal sealed class TargetGraph
    {
        private static readonly char[] TargetsSplitChars = { ';', '\r', '\n', '\t', ' ' };

        private readonly ProjectInstance _projectInstance;
        private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> _dependencies = new Dictionary<string, HashSet<string>>();

        public TargetGraph(ProjectInstance projectInstance)
        {
            _projectInstance = projectInstance;
            Calculate();
        }

        private void Calculate()
        {
            foreach (var target in _projectInstance.Targets)
            {
                _dependents[target.Key] = new HashSet<string>();
                _dependencies[target.Key] = new HashSet<string>();
            }

            foreach (var target in _projectInstance.Targets)
            {
                var targetDependencies = GetTargetDependencies(target.Key);
                _dependencies[target.Key].UnionWith(targetDependencies);
                foreach (var dependency in targetDependencies)
                {
                    if (!_dependents.ContainsKey(dependency))
                    {
                        _dependents.Add(dependency, new HashSet<string>());
                    }

                    _dependents[dependency].Add(target.Key);
                }
            }
        }

        public IEnumerable<string> GetDependents(string target)
        {
            _dependents.TryGetValue(target, out var bucket);
            return bucket;
        }

        public IEnumerable<string> GetDependencies(string target)
        {
            _dependencies.TryGetValue(target, out var bucket);
            return bucket ?? Enumerable.Empty<string>();
        }

        private static IEnumerable<string> SplitTargets(string targets) => targets.Split(TargetsSplitChars, StringSplitOptions.RemoveEmptyEntries);

        private IEnumerable<string> GetTargetDependencies(string targetName) =>
            _projectInstance.Targets.TryGetValue(targetName, out var targetInstance)
                ? SplitTargets(_projectInstance.ExpandString(targetInstance.DependsOnTargets))
                : Enumerable.Empty<string>();
    }
}
