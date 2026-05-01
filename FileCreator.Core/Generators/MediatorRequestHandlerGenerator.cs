using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Core.Generators;

public class MediatorRequestHandlerGenerator
{
    public static CompilationUnitSyntax Generate(
        string ns,
        GroupName groupName,
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        string resultType;
        if (hasResponse)
        {
            resultType = responseType switch
            {
                ResponseType.Single => $"{useCaseName}{type}Response",
                ResponseType.IEnumerable => $"IEnumerable<{useCaseName}{type}Response>",
                ResponseType.KeyValuePair => $"IEnumerable<SelectItem>",
                ResponseType.PagedList => $"PagedList<{useCaseName}{type}Response>",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };
        }
        else
        {
            resultType = string.Empty;
        }
        ClassDeclarationSyntax handler;
        if (type == RequestType.Query)
        {
            var identifierName = responseType switch
            {
                ResponseType.Single => $"GetAsync",
                ResponseType.IEnumerable => "ListAsync",
                ResponseType.KeyValuePair => "ListAsync",
                ResponseType.PagedList => "ListAsync",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };
            TypeSyntax returnType =
                hasResponse ?
                ParseTypeName($"ValueTask<Result<{resultType}>>") :
                ParseTypeName($"ValueTask<Result>");
            var method =
                MethodDeclaration(returnType, "Handle")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("request"))
                            .WithType(ParseTypeName($"{useCaseName}{type}")),
                        Parameter(Identifier("cancellationToken"))
                            .WithType(ParseTypeName("CancellationToken")))
                     .WithExpressionBody(
                ArrowExpressionClause(
                    AwaitExpression(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("service"),
                                IdentifierName(identifierName)))
                        .AddArgumentListArguments(
                            Argument(IdentifierName("request")),
                            Argument(IdentifierName("cancellationToken"))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            SimpleBaseTypeSyntax items =
                hasResponse ?
                            SimpleBaseType(
                            ParseTypeName(type switch
                            {
                                RequestType.Command => $"ICommandHandler<{useCaseName}{type}, {resultType}>",
                                RequestType.Query => $"IQueryHandler<{useCaseName}{type}, {resultType}>",
                                _ => throw new NotImplementedException(),
                            })) :
                            SimpleBaseType(
                            ParseTypeName(type switch
                            {
                                RequestType.Command => $"ICommandHandler<{useCaseName}{type}>",
                                RequestType.Query => $"IQueryHandler<{useCaseName}{type}>",
                                _ => throw new NotImplementedException(),
                            }));
            handler =
                ClassDeclaration($"{useCaseName}{type}Handler")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                    .AddBaseListTypes(items)
                    .WithParameterList(ParameterList([
                        Parameter(Identifier("service")).WithType(IdentifierName($"I{useCaseName}Service"))
                        ]))
                    .AddMembers(method);
        }
        else
        {

            string successStatement = "throw new NotImplementedException();";

            TypeSyntax returnType =
                hasResponse ?
                ParseTypeName($"ValueTask<Result<{resultType}>>") :
                ParseTypeName($"ValueTask<Result>");
            var method =
                MethodDeclaration(
                        returnType, "Handle")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("request"))
                            .WithType(ParseTypeName($"{useCaseName}{type}")),
                        Parameter(Identifier("cancellationToken"))
                            .WithType(ParseTypeName("CancellationToken")))
                    .WithBody(Block(ParseStatement(successStatement)));

            SimpleBaseTypeSyntax items =
                hasResponse ?
                SimpleBaseType(
                ParseTypeName(type switch
                {
                    RequestType.Command => $"ICommandHandler<{useCaseName}{type}, {resultType}>",
                    RequestType.Query => $"IQueryHandler<{useCaseName}{type}, {resultType}>",
                    _ => throw new NotImplementedException(),
                })) :
                SimpleBaseType(
                ParseTypeName(type switch
                {
                    RequestType.Command => $"ICommandHandler<{useCaseName}{type}>",
                    RequestType.Query => $"IQueryHandler<{useCaseName}{type}>",
                    _ => throw new NotImplementedException(),
                }));

            handler =
                ClassDeclaration($"{useCaseName}{type}Handler")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                    .WithParameterList(ParameterList(
                        SeparatedList([
                            Parameter(
                                Identifier("repository")
                                )
                            .WithType(
                                IdentifierName($"I{groupName.Feature.TrimStart("The")}Repository")
                                )]
                            )
                        )
                    )
                    .AddBaseListTypes(items)
                    .AddMembers(method);

        }
        return RoslynHelpers.CompilationUnit(ns, handler);
    }
}
