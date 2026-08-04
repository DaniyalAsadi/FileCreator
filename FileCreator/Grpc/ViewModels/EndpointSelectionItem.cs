using FileCreator.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Grpc.ViewModels;

public sealed class EndpointSelectionItem
{
    public bool Selected { get; set; }

    public string Name { get; init; } = default!;

    public string Route { get; init; } = default!;

    public HttpVerb HttpVerb { get; init; }

    public string RequestType { get; init; } = default!;

    public string ResponseType { get; init; } = default!;
}