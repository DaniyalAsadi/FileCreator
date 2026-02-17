using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Helpers;

public static class RoslynHelpers
{
    public static CompilationUnitSyntax CompilationUnit(string ns, MemberDeclarationSyntax member, params string[] usings)
    {
        var cu = SyntaxFactory.CompilationUnit();

        foreach (var u in usings)
            cu = cu.AddUsings(UsingDirective(ParseName(u)));

        cu = cu.AddMembers(
                 FileScopedNamespaceDeclaration(ParseName(ns))
                .AddMembers(member));

        return cu;
    }
    public static StatementSyntax AssignDiscard(ExpressionSyntax expression)
        => ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName("_"),
                expression));

    public static StatementSyntax Assign(string variable, ExpressionSyntax expression)
        => ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(variable),
                expression));
}
