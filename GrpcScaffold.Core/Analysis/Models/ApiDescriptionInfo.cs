// src/GrpcScaffold.Core/Analysis/Models/ApiDescriptionInfo.cs
namespace GrpcScaffold.Core.Analysis.Models;

public sealed record ApiDescriptionInfo(
    string HttpMethod,
    string Route,
    string Tag,
    string Summary,
    string? Description,
    string Security);