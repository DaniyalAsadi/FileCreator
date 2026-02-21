using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestServiceGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName,RequestType type, ResponseType responseType)
    {
        var resultType = responseType switch
        {
            ResponseType.Single => $"Task<{useCaseName}{type}Response?>",
            ResponseType.IEnumerable => $"Task<IEnumerable<{useCaseName}{type}Response>>",
            ResponseType.PagedList => $"Task<PagedList<{useCaseName}{type}Response>>",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        var identifierName = responseType switch
        {
            ResponseType.Single => $"GetAsync",
            ResponseType.IEnumerable => "ListAsync",
            ResponseType.PagedList => "ListAsync",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        ParameterListSyntax parameterList;
        if (responseType is ResponseType.Single or ResponseType.IEnumerable)
        {
            parameterList = ParameterList(
                [
                Parameter(Identifier("cancellationToken")).WithType(ParseTypeName("CancellationToken"))
                ]);


        }
        else
        {
            parameterList = ParameterList(
                    [
                    Parameter(Identifier("filter")).WithType(ParseTypeName($"{useCaseName}{type}Filter")),
                    Parameter(Identifier("pagedRequest")).WithType(ParseTypeName("PagedRequest")),
                    Parameter(Identifier("cancellationToken")).WithType(ParseTypeName("CancellationToken"))
                    ]);
        }


        var method = MethodDeclaration(ParseTypeName(resultType),
            Identifier(identifierName))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword))
            .WithParameterList(parameterList)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));


        var @interface =
            InterfaceDeclaration($"I{useCaseName}Service")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
                .AddMembers(method);


        return RoslynHelpers.CompilationUnit(ns, @interface);
    }
}