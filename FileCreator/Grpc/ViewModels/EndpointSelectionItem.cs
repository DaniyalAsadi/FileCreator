using FileCreator.Core;
using GrpcScaffold.Core.Analysis.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.ViewModels;

public sealed record EndpointSelectionItem
{
    public bool Selected { get; set; }

    public string GroupName { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string Route { get; init; } = default!;

    public HttpVerb HttpVerb { get; init; }

    public string RequestType { get; init; } = default!;

    public string ResponseType { get; init; } = default!;


    public static EndpointSelectionItem Map(EndpointModel endpoint)
    {
        return new EndpointSelectionItem()
        {
            Selected = false,
            GroupName = endpoint.Route.Group,
            Name = endpoint.EndpointClassName,
            Route = endpoint.Route.Route,
            HttpVerb = Enum.Parse<HttpVerb>(endpoint.Route.HttpVerb),
            RequestType = endpoint.Request?.Name ?? "",
            ResponseType = endpoint.Response?.Name ?? ""
        };
    }

}