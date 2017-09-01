using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class Project : NodeWithTiming
    {
        public string ProjectFile { get; set; }

        public ImmutableDictionary<string, string> GlobalProperties { get; set; }

        public ImmutableDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// A lookup table mapping of target names to targets.
        /// Target names are unique to a project and the id is not always specified in the log.
        /// </summary>
        private readonly ConcurrentDictionary<string, Target> _targetNameToTargetMap = new ConcurrentDictionary<string, Target>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Target> _targetsById = new Dictionary<int, Target>();

        public void TryAddTarget(Target target)
        {
            if (target.Parent == null)
            {
                AddChild(target);
            }
        }

        public IEnumerable<Target> GetUnparentedTargets()
        {
            return _targetNameToTargetMap
                .Values
                .Union(_targetsById.Values)
                .Where(t => t.Parent == null)
                .OrderBy(t => t.StartTime)
                .ToArray();
        }

        public Target GetTargetById(int id)
        {
            if (_targetsById.TryGetValue(id, out var target))
            {
                return target;
            }

            target = _targetNameToTargetMap.Values.First(t => t.Id == id);
            _targetsById[id] = target;
            return target;
        }

        public override string ToString() => $"Project Name={Name} File={ProjectFile}";

        public Target GetOrAddTargetByName(string targetName)
        {
            Target result = _targetNameToTargetMap.GetOrAdd(targetName, CreateTargetInstance);
            return result;
        }

        public Target CreateTarget(string name, int id)
        {
            if (!_targetNameToTargetMap.TryGetValue(name, out var target) || target.Id != -1 && target.Id != id)
            {
                target = CreateTargetInstance(name);
                _targetNameToTargetMap.TryAdd(name, target);
            }

            target.Id = id;
            _targetsById[id] = target;

            return target;
        }

        private static Target CreateTargetInstance(string name)
        {
            return new Target
            {
                Name = name
            };
        }

        public Target GetTarget(string targetName, int targetId)
        {
            if (string.IsNullOrEmpty(targetName))
            {
                return GetTargetById(targetId);
            }

            _targetNameToTargetMap.TryGetValue(targetName, out var result);
            return result;
        }
    }
}
