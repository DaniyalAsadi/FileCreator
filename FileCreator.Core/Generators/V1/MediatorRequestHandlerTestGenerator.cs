using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators.V2;

public class MediatorRequestHandlerTestGenerator
{
    public static CompilationUnitSyntax Generate(
        string ns,
        GroupName groupName,
        string useCaseNameSpace,
        string useCaseName,
        RequestType type,
        bool hasResponse,
        ResponseType responseType)
    {


        var mockField =
            type == RequestType.Query ?
            FieldDeclaration(VariableDeclaration(IdentifierName($"Mock<I{useCaseName}Service>"))
                .AddVariables(VariableDeclarator("_serviceMock")))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)) :
                FieldDeclaration(VariableDeclaration(IdentifierName($"Mock<I{groupName.Feature.TrimStart("The")}Repository>"))
                .AddVariables(VariableDeclarator("_repositoryMock")))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
        var handlerField = FieldDeclaration(VariableDeclaration(IdentifierName($"{useCaseName}{type}Handler"))
                .AddVariables(VariableDeclarator("_handler")))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));



        var newMockStatement = type == RequestType.Query
            ? ParseStatement(
                $"_serviceMock = new Mock<I{useCaseName}Service>();")
            : ParseStatement(
                $"_repositoryMock = new Mock<I{groupName.Feature.TrimStart("The")}Repository>();");
        var newHandlerStatement = type == RequestType.Query
            ? ParseStatement(
                $"_handler = new(_serviceMock.Object);")
            : ParseStatement(
                $"_handler = new(_repositoryMock.Object);");

        var ctor = ConstructorDeclaration($"{useCaseName}{type}HandlerTests")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBodyStatements(newMockStatement, newHandlerStatement);


        var classDecl =
            ClassDeclaration($"{useCaseName}{type}HandlerTests")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddMembers(mockField, handlerField, ctor, GenerateTestMethod(useCaseName, type, hasResponse, responseType));

        return RoslynHelpers.CompilationUnit(ns, classDecl, useCaseNameSpace);
    }

    // ---------------- Test Method ----------------



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
                                        SeparatedList(
                                        [
                                            Argument(IdentifierName("request")),
                                            Argument(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("CancellationToken"),
                                                    IdentifierName("None")))
                                        ])))))))),

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
        var responsePart = hasResponse ? responseType switch
        {
            ResponseType.Single => "Single",
            ResponseType.IEnumerable => "List",
            ResponseType.KeyValuePair => "KeyValuePair",
            ResponseType.PagedList => "PagedList",
            _ => throw new NotSupportedException()
        } : "NoResponse";

        return $"{useCaseName}{type}Handle_Should_Return_Success_{responsePart}";
    }
}
