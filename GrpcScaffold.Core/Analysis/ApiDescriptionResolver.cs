using GrpcScaffold.Core.Analysis.Models;
using GrpcScaffold.Core.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace GrpcScaffold.Core.Analysis;


internal static partial class ApiDescriptionResolver
{
    public static bool TryResolve(
    ClassDeclarationSyntax endpointClass,
    AnalysisContext context,
    CancellationToken cancellationToken,
    [NotNullWhen(true)]out ApiDescriptionInfo? info)
    {
        info = default!;


        var endpointSemanticModel =
            context.EntryCompilation.GetSemanticModel(endpointClass.SyntaxTree);

        var configure = endpointClass.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.ValueText == "Configure");

        if (configure is null)
            return false;

        if (!TryFindSpecifyInvocation(
                configure,
                context.EntryCompilation,
                cancellationToken,
                out var specifyInvocation))
            return false;

        if (!TryGetApiDescriptionMember(
                specifyInvocation,
                endpointSemanticModel,
                cancellationToken,
                out var memberSymbol))
            return false;

        if (!TryGetApiDescriptionFactoryInvocation(
                memberSymbol,
                cancellationToken,
                out var apiInvocation))
            return false;

        var targetCompilation = context.FindCompilation(memberSymbol.ContainingAssembly);

        if (targetCompilation is null)
            return false;

        return TryCreateDescription(
            apiInvocation,
            targetCompilation,
            cancellationToken,
            out info);
    }

    private static bool TryFindSpecifyInvocation(
    MethodDeclarationSyntax configureMethod,
    Compilation compilation,
    CancellationToken cancellationToken,
    out InvocationExpressionSyntax invocation)
    {
        var semanticModel =
            compilation.GetSemanticModel(configureMethod.SyntaxTree);

        foreach (var node in configureMethod
                     .DescendantNodes()
                     .OfType<InvocationExpressionSyntax>())
        {
            var symbol =
                semanticModel.GetSymbolInfo(node, cancellationToken).Symbol
                as IMethodSymbol;

            if (symbol == null)
                continue;

            if (symbol.Name != "Specify")
                continue;

            invocation = node;
            return true;
        }

        invocation = null!;
        return false;
    }
    private static bool TryGetApiDescriptionMember(
        InvocationExpressionSyntax specifyInvocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ISymbol? memberSymbol)
    {
        memberSymbol = null;

        var argument =
            specifyInvocation.ArgumentList.Arguments.FirstOrDefault();

        if (argument == null)
            return false;

        memberSymbol =
            semanticModel.GetSymbolInfo(argument.Expression, cancellationToken)
                .Symbol;

        return memberSymbol != null;
    }

    private static bool TryGetApiDescriptionFactoryInvocation(
        ISymbol symbol,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out InvocationExpressionSyntax? invocation)
    {
        invocation = null!;

        SyntaxReference? syntaxReference = symbol switch
        {
            IFieldSymbol f => f.DeclaringSyntaxReferences.FirstOrDefault(),
            IPropertySymbol p => p.DeclaringSyntaxReferences.FirstOrDefault(),
            _ => null
        };

        if (syntaxReference == null)
            return false;

        var syntax = syntaxReference.GetSyntax(cancellationToken);

        EqualsValueClauseSyntax? initializer = syntax switch
        {
            VariableDeclaratorSyntax v => v.Initializer,
            PropertyDeclarationSyntax p => p.Initializer,
            _ => null
        };

        if (initializer == null)
            return false;

        invocation = initializer.Value as InvocationExpressionSyntax;

        return invocation != null;
    }

    private static bool TryCreateDescription(
    InvocationExpressionSyntax apiInvocation,
    Compilation compilation,
    CancellationToken cancellationToken,
    [NotNullWhen(true)] out ApiDescriptionInfo? info)
    {
        info = null;

        if (apiInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Api.Get / Api.Post / Api.Put ...
        var httpMethod = memberAccess.Name.Identifier.ValueText.ToUpperInvariant();

        string? route = null;
        string? tag = null;
        string? summary = null;
        string? description = null;
        string? security = null;

        foreach (var argument in apiInvocation.ArgumentList.Arguments)
        {
            if (argument.NameColon is null)
                continue;

            var name = argument.NameColon.Name.Identifier.ValueText;

            switch (name)
            {
                case "route":
                    route = ResolveExpression(argument.Expression, compilation, cancellationToken);
                    break;

                case "tag":
                    tag = ResolveExpression(argument.Expression, compilation, cancellationToken);
                    break;

                case "summary":
                    summary = ResolveExpression(argument.Expression, compilation, cancellationToken);
                    break;

                case "description":
                    description = ResolveExpression(argument.Expression, compilation, cancellationToken);
                    break;

                case "security":
                    security = ResolveExpression(argument.Expression, compilation, cancellationToken);
                    break;
            }
        }

        if (route is null ||
            tag is null ||
            summary is null ||
            security is null)
        {
            info = null;
            return false;
        }

        info = new ApiDescriptionInfo(
            HttpMethod: httpMethod,
            Route: route,
            Tag: tag,
            Summary: summary,
            Description: description,
            Security: security);

        return true;
    }
    private static string? ResolveExpression(
    ExpressionSyntax expression,
        Compilation compilation,
    CancellationToken cancellationToken)
    {
        var semanticModel = compilation.GetSemanticModel(expression.SyntaxTree);

        if (expression.SyntaxTree != semanticModel.SyntaxTree)
        {
            semanticModel =
                semanticModel.Compilation.GetSemanticModel(expression.SyntaxTree);
        }

        var constant =
            semanticModel.GetConstantValue(expression, cancellationToken);

        if (constant.HasValue)
            return constant.Value?.ToString();

        var symbol =
            semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;

        return symbol switch
        {
            IFieldSymbol f when f.HasConstantValue
                => f.ConstantValue?.ToString(),

            IFieldSymbol f
                => f.Name,

            IPropertySymbol p
                => p.Name,

            _ => expression.ToString()
        };
    }
}