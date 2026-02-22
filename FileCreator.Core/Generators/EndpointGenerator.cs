using FileCreator.Core;
using FileCreator.Core.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Generators;

public class EndpointGenerator
{
    public static CompilationUnitSyntax Generate(
    string ns,
    string useCaseNameSpace,
    string group,
    string useCaseName,
    RequestType type,
    HttpVerb httpVerb,
    bool hasRequest,
    bool hasResponse,
    ResponseType responseType)
    {
        string requestType = hasRequest ? $"{useCaseName}Request" : "EmptyRequest";
        string responseModelType;
        if (hasResponse)
        {
            responseModelType = responseType switch
            {
                ResponseType.Single => $"{useCaseName}{type}Response",
                ResponseType.IEnumerable => $"IEnumerable<{useCaseName}{type}Response>",
                ResponseType.KeyValuePair => $"IEnumerable<KeyValuePair<Guid,string>>",
                ResponseType.PagedList => $"PagedList<{useCaseName}{type}Response>",
                _ => throw new ArgumentOutOfRangeException(nameof(responseType)),
            };

        }
        else
        {
            responseModelType = "EmptyResponse";
        }
        var ctorParams = ParameterList(SeparatedList(
                    [
                    Parameter(Identifier("mediator"))
                .WithType(ParseTypeName($"IMediator"))
                ]));


        string baseType = hasRequest
            ? $"Endpoint<{requestType}>"
            : ("EndpointWithoutRequest");

        var classDecl =
            ClassDeclaration($"{useCaseName}Endpoint")
                .WithParameterList(ctorParams)
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(baseType)))
                .AddMembers(
                    GenerateConfigureMethod(group, useCaseName, hasResponse, responseModelType),
                    GenerateHandleMethod(useCaseName, type, httpVerb, hasRequest));

        return RoslynHelpers.CompilationUnit(ns, classDecl,
            useCaseNameSpace);
    }


    private static MethodDeclarationSyntax GenerateConfigureMethod(
        string group,
        string useCaseName,
        bool hasResponse,
        string responseType)
    {
        var statements = new[]
        {
        ParseStatement($"Tags(ApiRoutes.{group}.Tag);"),
        ParseStatement($"Specify(ApiRoutes.{group}.{useCaseName});"),
        CreateSummaryStatement(hasResponse,responseType)
        };

        var body = Block(statements);

        return MethodDeclaration(ParseTypeName("void"), "Configure")
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.OverrideKeyword))
            .WithBody(body);
    }
    static StatementSyntax CreateSummaryStatement(bool hasResponse, string responseType)
    {
        // Create the inner statements inside the lambda
        var statements = new List<StatementSyntax>
        {
            // s.Summary = "Creates a new communication entry.";
            ExpressionStatement(
        AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("s"),
                IdentifierName("Summary")),
            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(""))
        )
    ),

            // s.Description = "This endpoint allows for the creation of a new communication entry with a title, description, order, and state visibility.";
            ExpressionStatement(
        AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("s"),
                IdentifierName("Description")),
            LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal("")
            )
        )
    ),
            CreateResponseStatement(responseType, hasResponse)
        };

        // Build the lambda s => { ... }
        var lambda = SimpleLambdaExpression(
            Parameter(Identifier("s")),
            Block(statements)
        );

        // Build the outer Summary(...) invocation
        StatementSyntax summaryStatement = ExpressionStatement(
            InvocationExpression(IdentifierName("Summary"))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(lambda)
                    )
                )
            )
        );
        return summaryStatement;
    }
    private static ExpressionStatementSyntax CreateResponseStatement(string responseType, bool hasResponse)
    {
        if (hasResponse)
        {
            // s.Response<CreateCommunicationCommandResponse>(StatusCodes.Status201Created);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("s"),
                        GenericName(Identifier("Response"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(responseType)
                                    )
                                )
                            )
                    )
                )
            );
        }
        else
        {
            // s.Response();
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("s"),
                        IdentifierName("Response")
                    )
                )
            );
        }
    }

    private static MethodDeclarationSyntax GenerateHandleMethod(
    string useCaseName,
    RequestType type,
    HttpVerb httpVerb,
    bool hasRequest)
    {
        var parameters = new List<ParameterSyntax>();

        // req parameter (optional)
        if (hasRequest)
        {
            var parameter =
                Parameter(Identifier("req"))
                    .WithType(ParseTypeName($"{useCaseName}Request"));

            _ = ResolveBindingAttribute(httpVerb);

            //if (!string.IsNullOrEmpty(bindingAttr))
            //{
            //    parameter = parameter.AddAttributeLists(CreateAttribute(bindingAttr));
            //}

            parameters.Add(parameter);
        }


        // CancellationToken
        parameters.Add(
            Parameter(Identifier("ct"))
                .WithType(ParseTypeName("CancellationToken")));

        // var command/query = Request.MapToX(req);
        StatementSyntax mapStatement = hasRequest
            ? ParseStatement(
                $"var {type.ToString().ToLower()} = {useCaseName}Request.MapTo{type}(req);")
            : ParseStatement(
                $"var {type.ToString().ToLower()} = new {useCaseName}{type}();");

        // var result = await mediator.Send(...);
        var sendStatement =
            ParseStatement(
                $"var result = await mediator.Send({type.ToString().ToLower()}, ct);");

        // return ...
        StatementSyntax returnStatement = ParseStatement("return result.ToMinimalApiResult();");

        var body = Block(mapStatement, sendStatement, returnStatement);

        return MethodDeclaration(
                GenericName("Task")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(
                                ParseTypeName("IResult")))),
                "ExecuteAsync")
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.OverrideKeyword),
                Token(SyntaxKind.AsyncKeyword))
            .AddParameterListParameters([.. parameters])
            .WithBody(body);
    }
    private static string? ResolveBindingAttribute(HttpVerb verb)
    {
        return verb switch
        {
            HttpVerb.GET => "FromQuery",
            HttpVerb.POST => "FromBody",
            HttpVerb.PUT => "FromBody",
            HttpVerb.PATCH => "FromBody",
            HttpVerb.DELETE => "FromRoute",
            _ => null
        };
    }

    private static AttributeListSyntax CreateAttribute(string name)
    {
        return AttributeList(
            SingletonSeparatedList(
                Attribute(IdentifierName(name))));
    }

}
