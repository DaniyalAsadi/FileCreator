// src/GrpcScaffold.Core/IO/RegionMerger.cs
using System.Text.RegularExpressions;

namespace GrpcScaffold.Core.IO;

/// <summary>
/// Merges freshly generated content into an existing file, only replacing text
/// between matching `// <ai-generated:NAME>` ... `// </ai-generated:NAME>` markers.
/// Content outside markers (and any markers present in the existing file but absent
/// from the new template) is preserved verbatim.
/// </summary>
public static class RegionMerger
{
    private static readonly Regex RegionPattern = new(
        @"// <ai-generated:(?<name>[^>]+)>.*?// </ai-generated:\k<name>>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static string Merge(string existingContent, string newlyGeneratedContent)
    {
        var newRegions = ExtractRegions(newlyGeneratedContent);

        return RegionPattern.Replace(existingContent, match =>
        {
            var name = match.Groups["name"].Value;
            return newRegions.TryGetValue(name, out var replacement) ? replacement : match.Value;
        });
    }

    private static Dictionary<string, string> ExtractRegions(string content)
    {
        var result = new Dictionary<string, string>();
        foreach (Match match in RegionPattern.Matches(content))
            result[match.Groups["name"].Value] = match.Value;
        return result;
    }
}
