// Copyright (c) Microsoft. All Rights Reserved. Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Tools;

internal static class PropertyDictionaryExtensions
{
    public static bool GetBoolean<T>(this IImmutableDictionary<T, string> properties, T key, bool defaultValue)
    {
        return properties.TryGetValue(key, out string valueString) && bool.TryParse(valueString, out bool value)
            ? value
            : defaultValue;
    }
}
