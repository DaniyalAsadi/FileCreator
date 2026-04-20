using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestSpecificationGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName, RequestType type, ResponseType responseType)
    {
        StatementSyntax statements = type is RequestType.Query ? 
            ParseStatement("Query.AsNoTracking();") : 
            ParseStatement("Query");
        var ctor = ConstructorDeclaration($"{useCaseName}{type}Specification")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(statements));
        if (responseType == ResponseType.PagedList)
        {
            ctor = ctor.AddParameterListParameters(
                [
                 Parameter(Identifier("pagedRequest")).WithType(IdentifierName("PagedRequest"))
                ])
                .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(SeparatedList([
                        Argument(IdentifierName("pagedRequest"))
                    ]))));
        }


        SimpleBaseTypeSyntax items = responseType switch
        {
            ResponseType.Single => SimpleBaseType(ParseTypeName($"SingleResultSpecification<T,{useCaseName}{type}Response>")),
            ResponseType.IEnumerable => SimpleBaseType(ParseTypeName($"Specification<T,{useCaseName}{type}Response>")),
            ResponseType.PagedList => SimpleBaseType(ParseTypeName($"PagedListResultSpecification<T,{useCaseName}{type}Response>")),
            ResponseType.KeyValuePair => SimpleBaseType(ParseTypeName($"KeyValuePairResultSpecification<T, Guid, string>")),
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };


        var response =
            ClassDeclaration($"{useCaseName}{type}Specification")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(items)
                .AddMembers(ctor);

        return RoslynHelpers.CompilationUnit(ns, response,
            "Ardalis.Specification");
    }
}
