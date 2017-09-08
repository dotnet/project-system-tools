// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    [Serializable]
    public sealed class TargetInfo : IComparable<TargetInfo>, ISerializable
    {
        public string Name { get; }
        public string TargetFile { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public TimeSpan Elapsed { get; internal set; }

        public TargetInfo(string name)
        {
            Name = name;
        }

        public TargetInfo(string name, string targetFile, DateTime startTime) : this(name)
        {
            TargetFile = targetFile;
            StartTime = startTime;
        }

        public int CompareTo(TargetInfo other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var startComparison = StartTime.CompareTo(other.StartTime);
            return startComparison != 0
                ? startComparison
                : string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is TargetInfo targetInfo)
            {
                return CompareTo(targetInfo) == 0;
            }

            return false;
        }

        public static bool operator ==(TargetInfo left, TargetInfo right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(TargetInfo left, TargetInfo right)
            => !(left == right);

        public static bool operator <(TargetInfo left, TargetInfo right)
            => ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;

        public static bool operator <=(TargetInfo left, TargetInfo right)
            => ReferenceEquals(left, null) || left.CompareTo(right) <= 0;

        public static bool operator >(TargetInfo left, TargetInfo right)
            => !ReferenceEquals(left, null) && left.CompareTo(right) > 0;

        public static bool operator >=(TargetInfo left, TargetInfo right)
            => ReferenceEquals(left, null)
                ? ReferenceEquals(right, null)
                : left.CompareTo(right) >= 0;

        public override int GetHashCode()
        {
            var hashCode = -163920796;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + StartTime.GetHashCode();
            return hashCode;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(TargetFile), TargetFile);
            info.AddValue(nameof(StartTime), StartTime);
            info.AddValue(nameof(Elapsed), Elapsed);
        }
    }
}
