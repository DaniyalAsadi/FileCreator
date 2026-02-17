using FileCreator.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Generators;

internal class MediatorRequestHandlerGenerator
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
                ResponseType.Single => $"Result<{useCaseName}Response>",
                ResponseType.IEnumerable => $"Result<IEnumerable<{useCaseName}Response>>",
                ResponseType.PagedList => $"Result<PagedList<{useCaseName}Response>>",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };

        }
        else
        {
            resultType = "Result";
        }
        string successStatement;
        if (hasResponse)
        {
            successStatement = responseType switch
            {
                ResponseType.Single => $"return Result.Success(new {useCaseName}Response());",
                ResponseType.IEnumerable => $"return Result.Success(Array.Empty<{useCaseName}Response>());",
                ResponseType.PagedList => $"return Result.Success(Array.Empty<{useCaseName}Response>().ToPagedList(request.PagedRequest.PageIndex,request.PagedRequest.PageSize));",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };

        }
        else
        {
            successStatement = "return Result.Success();";
        }

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

        var handler =
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

        return RoslynHelpers.CompilationUnit(ns, handler);
    }
}
