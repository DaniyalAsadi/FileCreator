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
                ResponseType.Single => $"Result<{useCaseName}{type}Response>",
                ResponseType.IEnumerable => $"Result<IEnumerable<{useCaseName}{type}Response>>",
                ResponseType.KeyValuePair => $"Result<IEnumerable<KeyValuePair<Guid,string>>>",
                ResponseType.PagedList => $"Result<PagedList<{useCaseName}{type}Response>>",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };
        }
        else
        {
            resultType = "Result";
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
            var method =
                MethodDeclaration(
                        ParseTypeName($"ValueTask<{resultType}>"), "Handle")
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
                            Argument(IdentifierName("cancellationToken"))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            handler =
                ClassDeclaration($"{useCaseName}{type}Handler")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                    .AddBaseListTypes(
                        SimpleBaseType(
                            ParseTypeName(type switch
                            {
                                RequestType.Command => $"ICommandHandler<{useCaseName}{type}, {resultType}>",
                                RequestType.Query => $"IQueryHandler<{useCaseName}{type}, {resultType}>",
                                _ => throw new NotImplementedException(),
                            })))
                    .AddMembers(method);
        }
        else
        {

            string successStatement = "throw new NotImplementedException();";

            var method =
                MethodDeclaration(
                        ParseTypeName($"ValueTask<{resultType}>"), "Handle")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.AsyncKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("request"))
                            .WithType(ParseTypeName($"{useCaseName}{type}")),
                        Parameter(Identifier("cancellationToken"))
                            .WithType(ParseTypeName("CancellationToken")))
                    .WithBody(Block(ParseStatement(successStatement)));

            handler =
                ClassDeclaration($"{useCaseName}{type}Handler")
                    .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                    .AddBaseListTypes(
                        SimpleBaseType(
                            ParseTypeName(type switch
                            {
                                RequestType.Command => $"ICommandHandler<{useCaseName}{type}, {resultType}>",
                                RequestType.Query => $"IQueryHandler<{useCaseName}{type}, {resultType}>",
                                _ => throw new NotImplementedException(),
                            })))
                    .AddMembers(method);

        }
        return RoslynHelpers.CompilationUnit(ns, handler);
    }
}
