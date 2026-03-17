using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using Humanizer;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using FileCreator.Core;
using FileCreator.Core.Helpers;

namespace FileCreator.Core.Generators;

public class EndpointTestGenerator
{
    public static CompilationUnitSyntax Generate(
        string ns,
        string webNameSpace,
        GroupName groupName,
        string useCaseName,
        bool hasRequest,
        RequestType requestType,
        bool hasResponse,
        ResponseType responseType,
        HttpVerb httpVerb)
    {
        var ctorParams = ParameterList(SeparatedList([
            Parameter(Identifier("factory"))
            .WithType(ParseTypeName("CustomWebApplicationFactory<Program>"))
            ]));
        var argumentParam = ArgumentList(SeparatedList([
            Argument(IdentifierName("factory"))]));
        var classDecl =
            ClassDeclaration($"{useCaseName}Tests")
            .AddBaseListTypes(
                PrimaryConstructorBaseType(ParseTypeName("ApiTestBase"),
                argumentParam))
            .WithParameterList(ctorParams)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddMembers(GenerateTestMethod(
                groupName,
                useCaseName,
                hasRequest,
                requestType,
                hasResponse,
                responseType,
                httpVerb));

        return RoslynHelpers.CompilationUnit(ns, classDecl, webNameSpace);
    }

    // ---------- Test Method ----------

    private static MethodDeclarationSyntax GenerateTestMethod(
        GroupName groupName,
        string useCaseName,
        bool hasRequest,
        RequestType requestType,
        bool hasResponse,
        ResponseType responseType,
        HttpVerb httpVerb)
    {
        var methodName = BuildTestMethodName(groupName, useCaseName, httpVerb, hasResponse, responseType);

        // [Fact]
        var factAttr = AttributeList(
            SingletonSeparatedList(
                Attribute(IdentifierName("Fact"))));

        var statements = new List<StatementSyntax>();

        // var route = $"{ApiRoutes.Group.UseCase.RoutePattern}";
        statements.Add(
    LocalDeclarationStatement(
        VariableDeclaration(IdentifierName("var"))
        .AddVariables(
            VariableDeclarator("route")
            .WithInitializer(
                EqualsValueClause(
                    InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
                    .AddContents(
                        Interpolation(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("ApiRoutes"),
                                        IdentifierName(groupName.Resource.ToString())),
                                    IdentifierName(useCaseName)),
                                IdentifierName("RoutePattern")
                            )
                        )
                    )
                )
            )
        )
    )
);


        // var request = new XxxRequest();
        if (hasRequest)
        {
            statements.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator("request")
                        .WithInitializer(
                            EqualsValueClause(
                                ObjectCreationExpression(
                                    IdentifierName($"{useCaseName}Request"))
                                .WithArgumentList(ArgumentList()))))));
        }

        // await Client.PostBodyAsync<TReq,TRes>(...)
        statements.Add(CreateClientInvocation(useCaseName, requestType, responseType, httpVerb, hasRequest, hasResponse));

        // ArgumentNullException.ThrowIfNull(response);
        if (hasResponse)
        {
            statements.Add(
            ExpressionStatement(Chain
                .From("response")
                .Call("Should")
                .Call("NotBeNull")
                .Build()));
        }

        return MethodDeclaration(
                ParseTypeName("Task"),
                Identifier(methodName))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
            .AddAttributeLists(factAttr)
            .WithBody(Block(statements));
    }

    // ---------- Client Call ----------

    private static StatementSyntax CreateClientInvocation(
        string useCaseName,
        RequestType requestType,
        ResponseType responseType,
        HttpVerb verb,
        bool hasRequest,
        bool hasResponse)
    {
        var clientMethod = verb switch
        {
            HttpVerb.GET => hasResponse ? responseType switch
            {
                ResponseType.Single => "GetSingleAsync",
                ResponseType.IEnumerable => "GetEnumerableAsync",
                ResponseType.PagedList => "GetPagedListAsync",
                ResponseType.KeyValuePair => "GetEnumerableAsync",
                _ => throw new NotImplementedException(),
            } :
            "GetStatusAsync",
            HttpVerb.DELETE => "DeleteAsync",
            HttpVerb.POST => "PostBodyAsync",
            HttpVerb.PUT => "PutBodyAsync",
            HttpVerb.PATCH => "PatchBodyAsync",
            _ => "SendAsync"
        };

        var responseTypeString = hasResponse
            ? $"{useCaseName}{requestType}Response"
            : "HttpResponseMessage";

        var genericArgs = new List<TypeSyntax>();

        if (hasRequest && hasResponse)
            genericArgs.Add(IdentifierName($"{useCaseName}Request"));

        if (hasResponse)
            genericArgs.Add(IdentifierName(responseTypeString));

        SimpleNameSyntax methodNameSyntax;

        if (genericArgs.Any())
        {
            // Client.Method<T>()
            methodNameSyntax =
                GenericName(Identifier(clientMethod))
                .WithTypeArgumentList(TypeArgumentList(SeparatedList(genericArgs)));
        }
        else
        {
            // Client.Method()
            methodNameSyntax = IdentifierName(clientMethod);
        }


        var invocation =
            AwaitExpression(
        InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Client"),
                methodNameSyntax))
        .WithArgumentList(
            ArgumentList(
                hasRequest
                ? SeparatedList(
                [
                    Argument(IdentifierName("route")),
                    Argument(IdentifierName("request"))
                ])
                : SingletonSeparatedList(
                    Argument(IdentifierName("route"))))));
        if (hasResponse)
        {
            return LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
            .AddVariables(
                VariableDeclarator("response")
                .WithInitializer(EqualsValueClause(invocation))));
        }
        else
        {
            return ExpressionStatement(invocation);
        }
    }

    // ---------- Naming Convention ----------

    private static string BuildTestMethodName(
        GroupName group,
        string useCase,
        HttpVerb verb,
        bool hasResponse,
        ResponseType responseType)
    {
        var result = hasResponse ? responseType.ToString() : "NoContent";
        return $"{verb.ToString().Pascalize()}_{group.Resource}_{useCase}_Should_Return_{result}";
    }
}
