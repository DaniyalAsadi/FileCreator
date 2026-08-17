// src/GrpcScaffold.Core/Analysis/Models/RouteInfo.cs
namespace GrpcScaffold.Core.Analysis.Models;

public sealed record RouteInfo(
    string HttpVerb,          // GET/POST/PUT/DELETE
    string Route,             // "/api/resources/{id}"
    string Group,             // Group<TGroup>() name, if any
    bool AllowAnonymous);
