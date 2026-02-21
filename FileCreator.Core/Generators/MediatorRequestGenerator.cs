using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace FileCreator.Core.Generators;

public class MediatorRequestGenerator
{
    public static CompilationUnitSyntax Generate(
    string ns,
    string useCaseName,
    RequestType type,
    bool hasResponse,
    ResponseType responseType)
    {
        // ---------------- Result Type ----------------
        string resultType;
        if (hasResponse)
        {
            resultType = responseType switch
            {
                ResponseType.Single => $"Result<{useCaseName}{type}Response>",
                ResponseType.IEnumerable => $"Result<IEnumerable<{useCaseName}{type}Response>>",
                ResponseType.PagedList => $"Result<PagedList<{useCaseName}{type}Response>>",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };
        }
        else
        {
            resultType = "Result";
        }

        // ---------------- Base Interface ----------------
        var baseType = SimpleBaseType(
            ParseTypeName(type switch
            {
                RequestType.Command => $"ICommand<{resultType}>",
                RequestType.Query => $"IQuery<{resultType}>",
                _ => throw new NotImplementedException(),
            }));

        // ---------------- Class ----------------
        var classDecl = ClassDeclaration($"{useCaseName}{type}")
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.SealedKeyword))
            .AddBaseListTypes(baseType);

        // ------------------------------------------------------------
        // Add Primary Constructor ONLY for Query + PagedList
        // ------------------------------------------------------------
        if (type == RequestType.Query && responseType == ResponseType.PagedList)
        {
            // (GetRoleListQueryFilter filter, PagedRequest pagedRequest)
            var ctorParams = ParameterList(SeparatedList(
            [
            Parameter(Identifier("filter"))
                .WithType(ParseTypeName($"{useCaseName}{type}Filter")),

            Parameter(Identifier("pagedRequest"))
                .WithType(ParseTypeName("PagedRequest"))
        ]));

            classDecl = classDecl.WithParameterList(ctorParams);

            // ---------------- Properties ----------------

            // public {UseCaseName}Filter Filter { get; } = filter;
            var filterProp =
                PropertyDeclaration(
                        ParseTypeName($"{useCaseName}{type}Filter"),
                        Identifier("Filter"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                    .WithInitializer(
                        EqualsValueClause(IdentifierName("filter")))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            // public PagedRequest PagedRequest { get; } = pagedRequest;
            var pagedProp =
                PropertyDeclaration(
                        ParseTypeName("PagedRequest"),
                        Identifier("PagedRequest"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                    .WithInitializer(
                        EqualsValueClause(IdentifierName("pagedRequest")))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            classDecl = classDecl.AddMembers(filterProp, pagedProp);
        }

        return RoslynHelpers.CompilationUnit(ns, classDecl);
    }

}
