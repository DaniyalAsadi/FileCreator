using FileCreator.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Generators;

internal class EndpointRequestGenerator
{
    public static CompilationUnitSyntax Generate(
    string ns,
    string useCaseNameSpace,
    string useCaseName,
    RequestType type,
    bool hasResponse,
    ResponseType responseType)
    {
        var dto =
            ClassDeclaration($"{useCaseName}Request")
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName("IRequestEndpoints")));

        // ---- Conditional Paging Properties ----
        if (type == RequestType.Query &&
            hasResponse &&
            responseType == ResponseType.PagedList)
        {
            dto = dto.AddMembers(
                CreateRequiredIntProperty("PageIndex"),
                CreateRequiredIntProperty("PageSize"));
        }

        MethodDeclarationSyntax mapMethod =
            GenerateMapMethod(useCaseName, type, hasResponse, responseType);

        dto = dto.AddMembers(mapMethod);

        return RoslynHelpers.CompilationUnit(ns, dto,
            useCaseNameSpace,
            "ECommerce.SharedKernel");
    }

    private static MethodDeclarationSyntax GenerateMapMethod(
    string useCaseName,
    RequestType type,
    bool hasResponse,
    ResponseType responseType)
    {
        var method = MethodDeclaration(
                ParseTypeName($"{useCaseName}{type}"),
                $"MapTo{type}")
            .AddModifiers(
                Token(SyntaxKind.InternalKeyword),
                Token(SyntaxKind.StaticKeyword));

        // ------------------------------------------------------------
        // CASE 1 : Paged Query  (SPECIAL SHAPE)
        // ------------------------------------------------------------
        if (type == RequestType.Query &&
            hasResponse &&
            responseType == ResponseType.PagedList)
        {
            // parameter → ListRequest request
            method = method.AddParameterListParameters(
                Parameter(Identifier("request"))
                    .WithType(ParseTypeName("ListRequest")));

            // new {UseCase}Filter() { }
            var filterObject =
                ObjectCreationExpression(ParseTypeName($"{useCaseName}Filter"))
                .WithArgumentList(ArgumentList())
                .WithInitializer(
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression));

            // new PagedRequest(request.PageIndex, request.PageSize)
            var pagedRequestObject =
                ObjectCreationExpression(ParseTypeName("PagedRequest"))
                .WithArgumentList(
                    ArgumentList(SeparatedList(
                    [
                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("request"),
                        IdentifierName("PageIndex"))),

                    Argument(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("request"),
                        IdentifierName("PageSize")))
                    ])));

            // new Query(filter, pagedRequest)
            var newQuery =
                ObjectCreationExpression(ParseTypeName($"{useCaseName}{type}"))
                .WithArgumentList(
                    ArgumentList(SeparatedList(
                    [
                    Argument(filterObject),
                    Argument(pagedRequestObject)
                    ])));

            method = method.WithExpressionBody(
                    ArrowExpressionClause(newQuery))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            return method;
        }

        // ------------------------------------------------------------
        // CASE 2 : Default Mapping
        // ------------------------------------------------------------
        method = method.AddParameterListParameters(
            Parameter(Identifier("req"))
                .WithType(ParseTypeName($"{useCaseName}Request")));

        var defaultCtor =
            ObjectCreationExpression(ParseTypeName($"{useCaseName}{type}"))
            .WithArgumentList(ArgumentList());

        method = method.WithExpressionBody(
                ArrowExpressionClause(defaultCtor))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        return method;
    }
    private static PropertyDeclarationSyntax CreateRequiredIntProperty(string name)
    {
        return PropertyDeclaration(
                PredefinedType(Token(SyntaxKind.IntKeyword)),
                Identifier(name))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.RequiredKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
    }


}
