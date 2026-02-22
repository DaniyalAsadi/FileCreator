using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestSpecificationGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName, RequestType type,ResponseType responseType)
    {
        var ctor = ConstructorDeclaration($"{useCaseName}{type}Specification")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(ParseStatement("_ = Query;")
                ));
        SimpleBaseTypeSyntax items = responseType switch
        {
            ResponseType.Single => SimpleBaseType(ParseTypeName("SingleResultSpecification")),
            ResponseType.IEnumerable => SimpleBaseType(ParseTypeName("Specification")),
            ResponseType.PagedList => SimpleBaseType(ParseTypeName("PagedListResultSpecification")),
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
