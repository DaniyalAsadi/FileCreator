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
    public static CompilationUnitSyntax Generate(string ns, string useCaseName,RequestType type, ResponseType responseType)
    {
        var resultType = responseType switch
        {
            ResponseType.Single => $"{useCaseName}{type}Response?",
            ResponseType.IEnumerable => $"IEnumerable<{useCaseName}{type}Response>",
            ResponseType.PagedList => $"PagedList<{useCaseName}{type}Response>",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        var identifierName = responseType switch
        {
            ResponseType.Single => $"GetAsync",
            ResponseType.IEnumerable => "ListAsync",
            ResponseType.PagedList => "ListAsync",
            _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
        };
        
        var method = MethodDeclaration(ParseTypeName(resultType),
            Identifier(identifierName))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword))
            .WithBody(Block());

        var @class =
            ClassDeclaration($"I{useCaseName}Service")
                .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
                .AddMembers(method);


        return RoslynHelpers.CompilationUnit(ns, @class);
    }
}
