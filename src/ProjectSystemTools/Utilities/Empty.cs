// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools;

internal static class Empty
{
    public static Task<IImmutableSet<ILogger>> LoggersTask { get; } = Task.FromResult<IImmutableSet<ILogger>>(ImmutableHashSet<ILogger>.Empty);
}
