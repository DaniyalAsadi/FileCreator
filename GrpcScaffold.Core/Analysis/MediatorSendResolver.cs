// src/GrpcScaffold.Core/Analysis/MediatorSendResolver.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GrpcScaffold.Core.Analysis;

public sealed record MediatorSendInfo(ITypeSymbol MessageType, ITypeSymbol? InferredResponseType);

public sealed class MediatorSendResolver
{
    public MediatorSendInfo? ResolveMediatorSend(
        ClassDeclarationSyntax classDecl, SemanticModel model, CancellationToken ct)
    {
        var handleMethod = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text is "ExecuteAsync" or "HandleAsync");

        if (handleMethod?.Body is null) return null;

        foreach (var invocation in handleMethod.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Send" } memberAccess)
                continue;

            var symbolInfo = model.GetSymbolInfo(invocation, ct);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol
                                ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            // Confirm this is IMediator.Send / ISender.Send, not an unrelated ".Send(...)"
            var receiverType = model.GetTypeInfo(memberAccess.Expression, ct).Type;
            var isMediatorLike = receiverType?.AllInterfaces
                .Concat(new[] { receiverType })
                .Any(t => t?.Name is "IMediator" or "ISender") ?? false;

            if (!isMediatorLike) continue;

            var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (firstArg is null) continue;

            var messageType = ResolveExpressionType(firstArg, model, ct);
            if (messageType is null) continue;

            // Infer TResponse from IRequest<TResponse> the message implements, if present.
            var responseType = messageType.AllInterfaces
                .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 1)
                ?.TypeArguments[0];

            return new MediatorSendInfo(messageType, responseType);
        }

        return null;
    }

    /// <summary>
    /// Resolves the CLR type of the expression passed to Send(...), following
    /// object-creation, extension-method mapping (`request.ToQuery()`), and
    /// local variable declarations back to their assignment.
    /// </summary>
    private static ITypeSymbol? ResolveExpressionType(ExpressionSyntax expr, SemanticModel model, CancellationToken ct)
    {
        // Case 1: new SomeQuery(...) / new SomeQuery { ... }
        if (expr is ObjectCreationExpressionSyntax objCreation)
            return model.GetTypeInfo(objCreation, ct).Type;

        // Case 2: request.ToQuery() / mapper.Map<SomeQuery>(request)
        if (expr is InvocationExpressionSyntax mappingCall)
            return model.GetTypeInfo(mappingCall, ct).Type;

        // Case 3: identifier referring to a local (var query = ...;)
        if (expr is IdentifierNameSyntax identifier)
        {
            var symbol = model.GetSymbolInfo(identifier, ct).Symbol;
            if (symbol is ILocalSymbol local)
                return local.Type;
        }

        // Fallback: just ask the semantic model for the static type of the expression.
        return model.GetTypeInfo(expr, ct).Type;
    }
}