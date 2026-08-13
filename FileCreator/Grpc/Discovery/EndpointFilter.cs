using FileCreator.Grpc.ViewModels;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace FileCreator.Grpc.Discovery;

public sealed class EndpointFilter
{
    public ImmutableArray<EndpointModel> Apply(
        ImmutableArray<EndpointModel> endpoints,
        GrpcGenerationOptions options)
    {
        if (options.GenerateAll) return endpoints;

        IEnumerable<EndpointModel> query = endpoints;

        if (options.InternalOnly)
            query = query.Where(e => e.Visibility == EndpointVisibility.Internal);

        if (!string.IsNullOrWhiteSpace(options.EndpointFilter))
            query = query.Where(e => Matches(options.EndpointFilter!, e));

        return query.ToImmutableArray();
    }

    public static bool Matches(string pattern, EndpointModel endpoint) =>
        GlobMatch(pattern, endpoint.EndpointClassName);

    public static bool GlobMatch(string pattern, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase);
    }
}
