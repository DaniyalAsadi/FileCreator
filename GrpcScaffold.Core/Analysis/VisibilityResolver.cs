// src/GrpcScaffold.Core/Analysis/VisibilityResolver.cs
using GrpcScaffold.Core.Analysis.Models;
using Microsoft.CodeAnalysis;

namespace GrpcScaffold.Core.Analysis;

public sealed class VisibilityResolver
{
    public EndpointVisibility Resolve(INamedTypeSymbol classSymbol, RouteInfo route)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            switch (attr.AttributeClass?.Name)
            {
                case "GrpcInternalAttribute": return EndpointVisibility.Internal;
                case "GrpcExternalAttribute": return EndpointVisibility.External;
            }
        }

        if (route.Group is not null)
        {
            if (route.Group.Contains("Internal", StringComparison.OrdinalIgnoreCase))
                return EndpointVisibility.Internal;
            if (route.Group.Contains("Public", StringComparison.OrdinalIgnoreCase) ||
                route.Group.Contains("External", StringComparison.OrdinalIgnoreCase))
                return EndpointVisibility.External;
        }

        if (route.Route.StartsWith("/internal/", StringComparison.OrdinalIgnoreCase))
            return EndpointVisibility.Internal;
        if (route.Route.StartsWith("/public/", StringComparison.OrdinalIgnoreCase) ||
            route.Route.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return EndpointVisibility.External;

        return EndpointVisibility.Unknown;
    }
}