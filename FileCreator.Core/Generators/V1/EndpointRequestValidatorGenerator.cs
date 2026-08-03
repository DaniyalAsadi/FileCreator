using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators.V2;

public class EndpointRequestValidatorGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName)
    {
        var baseType = ParseTypeName($"Validator<{useCaseName}Request>");

        var ctor =
            ConstructorDeclaration($"{useCaseName}Validator")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(
                    // placeholder rule
                    ParseStatement("// RuleFor(x => x.SomeProperty).NotEmpty();")
                ));

        var validator =
            ClassDeclaration($"{useCaseName}Validator")
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(baseType))
                .AddMembers(ctor);

        return RoslynHelpers.CompilationUnit(ns, validator,
            "FluentValidation");
    }

}
