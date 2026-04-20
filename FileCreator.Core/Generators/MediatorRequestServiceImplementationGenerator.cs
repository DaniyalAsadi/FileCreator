using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Core.Generators;

public class MediatorRequestServiceImplementationGenerator
{
    public static CompilationUnitSyntax Generate(
        string ns,
        string useCaseNamespace,
        string useCaseName,
        RequestType type,
        ResponseType responseType)
    {
        var resultType = responseType switch
        {
            ResponseType.Single => $"Task<{useCaseName}{type}Response?>",
            ResponseType.IEnumerable => $"Task<IEnumerable<{useCaseName}{type}Response>>",
            ResponseType.KeyValuePair => $"Task<IEnumerable<KeyValuePair<Guid,string>>>",
            ResponseType.PagedList => $"Task<PagedList<{useCaseName}{type}Response>>",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        var identifierName = responseType switch
        {
            ResponseType.Single => $"GetAsync",
            ResponseType.IEnumerable => "ListAsync",
            ResponseType.KeyValuePair => "ListAsync",
            ResponseType.PagedList => "ListAsync",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        ParameterListSyntax parameterList = responseType switch
        {
            ResponseType.Single or ResponseType.IEnumerable or ResponseType.KeyValuePair =>
            ParameterList(
                    [
                    Parameter(Identifier("cancellationToken")).WithType(ParseTypeName("CancellationToken"))
                    ]),
            ResponseType.PagedList =>
                   ParameterList(
                            [
                            Parameter(Identifier("filter")).WithType(ParseTypeName($"{useCaseName}{type}Filter")),
                    Parameter(Identifier("pagedRequest")).WithType(ParseTypeName("PagedRequest")),
                    Parameter(Identifier("cancellationToken")).WithType(ParseTypeName("CancellationToken"))
                            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        string successStatement;

        successStatement = responseType switch
        {
            ResponseType.Single => $"repository.SingleOrDefaultAsync(new {useCaseName}{type}Specification(),cancellationToken);",
            ResponseType.IEnumerable => $"Array.Empty<{useCaseName}{type}Response>();",
            ResponseType.KeyValuePair => $"Array.Empty<KeyValuePair<Guid,string>>();",
            ResponseType.PagedList => $"repository.ToPagedListAsync(new {useCaseName}{type}Specification(pagedRequest),cancellationToken);",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };


        var method = MethodDeclaration(ParseTypeName(resultType),
            Identifier(identifierName))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword))
            .WithExpressionBody(ArrowExpressionClause(ParseExpression(successStatement)))
            .WithParameterList(parameterList);

        var @class =
            ClassDeclaration($"{useCaseName}Service")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
                .AddMembers(method)
                .WithParameterList(ParameterList([
                    Parameter(Identifier("repository"))
                    ]))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName($"I{useCaseName}Service")));


        return RoslynHelpers.CompilationUnit(ns, @class,
            useCaseNamespace);
    }
}
