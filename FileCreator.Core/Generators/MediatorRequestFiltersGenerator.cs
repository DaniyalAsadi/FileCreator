using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestFiltersGenerator
{
    public static CompilationUnitSyntax Generate(string ns, string useCaseName, RequestType type)
    {
        var response =
            ClassDeclaration($"{useCaseName}{type}Filter")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword));

        return RoslynHelpers.CompilationUnit(ns, response);
    }

}
