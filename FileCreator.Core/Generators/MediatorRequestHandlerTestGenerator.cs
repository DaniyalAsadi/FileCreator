using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class MediatorRequestHandlerTestGenerator
{
    public static CompilationUnitSyntax Generate(
        string ns,
        string useCaseNameSpace,
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        var classDecl =
            ClassDeclaration($"{useCaseName}{type}HandlerTests")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddMembers(GenerateHandlerField(useCaseName, type), GenerateTestMethod(useCaseName, type, hasResponse, responseType));

        return RoslynHelpers.CompilationUnit(ns, classDecl, useCaseNameSpace);
    }

    // ---------------- Test Method ----------------

    private static FieldDeclarationSyntax GenerateHandlerField(
        string useCaseName,
        RequestType type)
    {
        return FieldDeclaration(VariableDeclaration(IdentifierName($"{useCaseName}{type}Handler"))
                .AddVariables(
                    VariableDeclarator("_handler")
                    .WithInitializer(
                        EqualsValueClause(
                            ImplicitObjectCreationExpression()
                            .WithArgumentList(ArgumentList())))))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));

    }

    private static MethodDeclarationSyntax GenerateTestMethod(
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        var methodName = BuildMethodName(useCaseName, type, hasResponse, responseType);

        // [Fact]
        var factAttr =
            AttributeList(
                SingletonSeparatedList(
                    Attribute(IdentifierName("Fact"))));

        var statements = new List<StatementSyntax>
        {
            // var request = new XxxCommand();
            LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                .AddVariables(
                    VariableDeclarator("request")
                    .WithInitializer(
                        EqualsValueClause(
                            ObjectCreationExpression(
                                IdentifierName($"{useCaseName}{type}"))
                            .WithArgumentList(ArgumentList()))))),

            // var result = await handler.Handle(request, CancellationToken.None);
            LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                .AddVariables(
                    VariableDeclarator("result")
                    .WithInitializer(
                        EqualsValueClause(
                            AwaitExpression(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("_handler"),
                                        IdentifierName("Handle")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList(new[]
                                        {
                                            Argument(IdentifierName("request")),
                                            Argument(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("CancellationToken"),
                                                    IdentifierName("None")))
                                        })))))))),

            ExpressionStatement(Chain
                .From("result")
                .Call("Should")
                .Call("NotBeNull")
                .Build()),
            ExpressionStatement(Chain
                .From("result")
                .Member("IsSuccess")
                .Call("Should")
                .Call("BeTrue")
                .Build())
        };

        return MethodDeclaration(
            ParseTypeName("Task"),
                Identifier(methodName))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
            .AddAttributeLists(factAttr)
            .WithBody(Block(statements));
    }

    // ---------------- Naming Convention ----------------

    private static string BuildMethodName(
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {
        var responsePart = hasResponse ? responseType.ToString() : "NoResponse";

        return $"{useCaseName}{type}Handle_Should_Return_Success_{responsePart}";
    }
}
