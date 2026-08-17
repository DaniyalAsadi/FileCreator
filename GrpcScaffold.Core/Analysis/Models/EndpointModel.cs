// src/GrpcScaffold.Core/Analysis/Models/EndpointModel.cs
namespace GrpcScaffold.Core.Analysis.Models;

public enum EndpointVisibility { Internal, External, Unknown }

public sealed record EndpointModel(
    string EndpointGroupName,
    string EndpointClassName,      // "ApiResourceListEndpoint"
    string EndpointNamespace,      // "MyApp.Api.Endpoints.ApiResource"
    string ServiceName,            // "ApiResourceService" (derived from Group or class name)
    string RpcName,                // "List" (derived from route/verb/class name)
    ContractInfo? Request,
    ContractInfo? Response,
    ContractInfo MediatorMessage,
    bool MediatorMessageIsCommand,   // Command vs Query (naming convention detection)
    RouteInfo Route,
    EndpointVisibility Visibility,
    string SourceFilePath);