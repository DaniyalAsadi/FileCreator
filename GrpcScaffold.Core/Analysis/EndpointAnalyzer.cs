// src/GrpcScaffold.Core/Analysis/EndpointAnalyzer.cs
using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.Generation;
using GrpcScaffold.Core.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace GrpcScaffold.Core.Analysis;
/*
DeleteCurrency: could not infer a response type - skipping. Consider using Endpoint<TRequest, TResponse> or an explicit return type.
*/
public sealed class EndpointAnalyzer(AnalysisContext context, VisibilityResolver visibilityResolver, MediatorSendResolver sendResolver)
{
    private const string EndpointBaseMetadataName1 = "SharedKernel.Tools.EndpointWithoutRequest";
    private const string EndpointBaseMetadataName2 = "SharedKernel.Tools.Endpoint<TRequest>";

    /// <summary>
    /// Discovers every Endpoint&lt;TRequest[,TResponse]&gt; subclass in the given compilation.
    /// </summary>
    public async Task<ImmutableArray<EndpointModel>> DiscoverAsync(
        Compilation compilation,
        CancellationToken ct = default)
    {
        var results = ImmutableArray.CreateBuilder<EndpointModel>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            ct.ThrowIfCancellationRequested();
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(ct);

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(classDecl, ct) is not INamedTypeSymbol classSymbol)
                    continue;

                
                if (!TryGetEndpointRequestType(classSymbol, out var requestType))
                    continue;

                if (!TryGetEndpointResponseType(
                    classDecl,
                    semanticModel,
                    ct,
                    out var responseType))
                {
                    continue;
                }

                var model = BuildModel(
                    classDecl,
                    classSymbol,
                    requestType,
                    responseType,
                    ct);
                if (model is not null)
                    results.Add(model);
            }
        }

        return results.ToImmutable();
    }

    private static bool TryGetEndpointResponseType(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        CancellationToken ct,
        out ITypeSymbol? responseType)
    {
        responseType = null;

        var configure = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "Configure");

        if (configure is null)
            return false;

        foreach (var invocation in configure.DescendantNodes()
                     .OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol method)
                continue;

            if (method.Name != "Response")
                continue;

            // Response<T>()
            if (method.IsGenericMethod)
            {
                responseType = method.TypeArguments[0];
                return true;
            }

            // Response()
            responseType = null;
            return true;
        }

        return false;
    }

    private static bool TryGetEndpointRequestType(
        INamedTypeSymbol classSymbol,
        out ITypeSymbol? requestType)
    {
        for (var baseType = classSymbol.BaseType;
             baseType is not null;
             baseType = baseType.BaseType)
        {
            var def = baseType.OriginalDefinition.ToDisplayString();
            if (def == EndpointBaseMetadataName2 &&
                baseType.TypeArguments.Length == 1)
            {
                requestType = baseType.TypeArguments[0];
                return true;
            }

            if (def == EndpointBaseMetadataName1)
            {
                requestType = null;
                return true;
            }
        }

        requestType = null;
        return false;
    }

    private EndpointModel? BuildModel(
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol classSymbol,
        ITypeSymbol? requestType,
        ITypeSymbol? responseType,
        CancellationToken ct)
    {
        var semanticModel =
            context.EntryCompilation.GetSemanticModel(classDecl.SyntaxTree);

        if (!ApiDescriptionResolver.TryResolve(classDecl, context, ct, out var api))
        {
            return null;
        }

        var route = new RouteInfo(
            HttpVerb: api.HttpMethod,
            Route: api.Route,
            Group: api.Tag,
            AllowAnonymous: api.Security == "EndpointSecurityStore.Anonymous");
        var sendInfo = sendResolver.ResolveMediatorSend(classDecl, semanticModel, ct);
        if (sendInfo is null)
        {
            Console.Error.WriteLine(
                $"[warn] {classSymbol.Name}: could not resolve an IMediator.Send(...) call — skipping.");
            return null;
        }


        

        var serviceName = DeriveServiceName(classSymbol.Name, route.Group);
        var rpcName = DeriveRpcName(classSymbol.Name, serviceName, route.HttpVerb);

        return new EndpointModel(
            EndpointGroupName:api.Tag,
            EndpointClassName: classSymbol.Name,
            EndpointNamespace: classSymbol.ContainingNamespace.ToDisplayString(),
            ServiceName: serviceName,
            RpcName: rpcName,
            Request: CreateContract(requestType),
            Response: CreateContract(responseType),
            MediatorMessage: CreateContract(sendInfo.MessageType)!,
            MediatorMessageIsCommand: sendInfo.MessageType.Name.EndsWith("Command", StringComparison.Ordinal),
            Route: route,
            Visibility: visibilityResolver.Resolve(classSymbol, route),
            SourceFilePath: classDecl.SyntaxTree.FilePath);
    }

    private static string DeriveServiceName(
        string endpointClassName,
        string? group)
    {
        if (group is not null) return $"{group}Service";

        // "ApiResourceListEndpoint" -> "ApiResourceService"
        var trimmed = endpointClassName.EndsWith("Endpoint") ? endpointClassName[..^"Endpoint".Length] : endpointClassName;

        // Strip a trailing verb-ish suffix (List/Get/Create/Update/Delete) to find the resource root.
        foreach (var verbSuffix in new[] { "List", "GetById", "Get", "Create", "Update", "Delete" })
        {
            if (trimmed.EndsWith(verbSuffix) && trimmed.Length > verbSuffix.Length)
                return trimmed[..^verbSuffix.Length] + "Service";
        }
        return trimmed + "Service";
    }

    private static string DeriveRpcName(
        string endpointClassName,
        string serviceName,
        string httpVerb)
    {
        var trimmed = endpointClassName.EndsWith("Endpoint") ? endpointClassName[..^"Endpoint".Length] : endpointClassName;
        var resourceRoot = serviceName[..^"Service".Length];

        var rpc = trimmed.StartsWith(resourceRoot) ? trimmed[resourceRoot.Length..] : trimmed;
        if (string.IsNullOrEmpty(rpc))
        {
            rpc = httpVerb switch
            {
                "GET" => "Get",
                "POST" => "Create",
                "PUT" => "Update",
                "DELETE" => "Delete",
                _ => "Execute"
            };
        }
        return rpc;
    }
    private static ContractInfo? CreateContract(ITypeSymbol? type)
    {
        if (type is null)
            return null;

        return CreateContract(type,
            new Dictionary<ITypeSymbol, ContractInfo>(SymbolEqualityComparer.Default));
    }

    private static ContractInfo CreateContract(
    ITypeSymbol type,
    IDictionary<ITypeSymbol, ContractInfo> visited)
    {
        if (visited.TryGetValue(type, out var existing))
            return existing;

        List<ConstructorInfo> constructorInfos = GetConstructors(type).ToList();
        var contract = new ContractInfo
        {
            ClrType = type,
            Name = type.Name,
            Namespace = type.ContainingNamespace.ToDisplayString(),
            Fields = ProtoTypeMapper.ExtractFields(type),
            Constructors = constructorInfos,
            PreferredConstructor = constructorInfos.FirstOrDefault(c => c.IsPreferred)
        };

        visited[type] = contract;

        var dependencies = new List<ContractInfo>();

        foreach (var field in contract.Fields)
        {
            Collect(field.Reference, dependencies, visited);
        }

        contract = contract with
        {
            Dependencies = dependencies
        };

        visited[type] = contract;

        return contract;
    }
    private static void Collect(
    ProtoTypeReference reference,
    ICollection<ContractInfo> dependencies,
    IDictionary<ITypeSymbol, ContractInfo> visited)
    {
        if (reference.ElementType is not null)
        {
            Collect(reference.ElementType, dependencies, visited);
        }

        foreach (var arg in reference.GenericArguments)
        {
            Collect(arg, dependencies, visited);
        }

        if (!reference.IsMessage)
            return;

        if (visited.TryGetValue(reference.ClrType, out var existing))
        {
            dependencies.Add(existing);
            return;
        }

        dependencies.Add(CreateContract(reference.ClrType, visited));
    }
    private static IReadOnlyList<ConstructorInfo> GetConstructors(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return [];

        var ctors = namedType.InstanceConstructors
            .Where(c =>
                !c.IsImplicitlyDeclared &&
                c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();

        if (ctors.Count == 0)
            return [];

        var preferred = ctors[0];
        return [.. ctors.Select(c => new ConstructorInfo
        {
            Name = c.Name,

            IsPublic = true,

            IsParameterless = c.Parameters.Length == 0,

            IsPreferred = SymbolEqualityComparer.Default.Equals(c, preferred),

            Parameters = [.. c.Parameters
        .Select(p => new ConstructorParameterInfo
        {
            Name = p.Name,

            TypeName = p.Type.ToDisplayString(),

            Type = p.Type,

            SourceFieldName = p.Name,

            IsOptional = p.IsOptional,

            HasDefaultValue = p.HasExplicitDefaultValue,

            DefaultValue = p.HasExplicitDefaultValue
                ? p.ExplicitDefaultValue
                : null,

            IsNullable = NullableAnnotation.Annotated == p.NullableAnnotation,

            IsParams = p.IsParams,

            RefKind = p.RefKind
        })]
        })];
    }
}