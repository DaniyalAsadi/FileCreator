using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestSpecificationGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName, RequestType type)
    {
        var ctor = ConstructorDeclaration($"{useCaseName}{type}Specification")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(ParseStatement("_ = Query;")
                ));


        var response =
            ClassDeclaration($"{useCaseName}{type}Specification")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName("Specification")))
                .AddMembers(ctor);

        return RoslynHelpers.CompilationUnit(ns, response);
    }
}
