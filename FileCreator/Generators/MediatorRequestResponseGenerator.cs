using FileCreator.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Generators;

internal class MediatorRequestResponseGenerator
{

    public static CompilationUnitSyntax Generate(string ns, string useCaseName, RequestType type)
    {
        var response =
            ClassDeclaration($"{useCaseName}{type}Response")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword));

        return RoslynHelpers.CompilationUnit(ns, response);
    }

}
