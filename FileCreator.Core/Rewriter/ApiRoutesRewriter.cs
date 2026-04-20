using FileCreator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Rewriter;

public sealed class ApiRoutesRewriter(
    string projectName,
    string groupName,
    string usecaseName,
    HttpVerb httpVerb,
    string route) : CSharpSyntaxRewriter
{
    // ------------------------------------------------------------
    // بازنویسی کلاس ApiRoutes
    // ------------------------------------------------------------
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.Identifier.Text != "ApiRoutes")
            return base.VisitClassDeclaration(node);

        var projectMembers = node.Members.ToList();

        var projectClass = projectMembers
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == projectName) ?? throw new NoMatchFoundException();

        var groupMembers = projectClass.Members.ToList();

        var groupClass = groupMembers
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == groupName);

        if (groupClass == null)
        {
            groupClass = CreateGroupClass();
            groupMembers.Add(groupClass);
        }
        else
        {
            groupClass = UpdateGroupClass(groupClass);
            var index = groupMembers.FindIndex(m =>
                m is ClassDeclarationSyntax c &&
                c.Identifier.Text == groupName);

            groupMembers[index] = groupClass;
        }

        // 🔴 این خط مهم است
        groupMembers = NormalizeAllGroups(groupMembers);

        return node.WithMembers(List(groupMembers));
    }
    // ------------------------------------------------------------
    // ساخت کلاس گروه جدید
    // ------------------------------------------------------------
    private ClassDeclarationSyntax CreateGroupClass()
    {
        return ClassDeclaration(groupName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .WithMembers(List<MemberDeclarationSyntax>(
            [
                CreateTagField(groupName),
                CreateApiDescriptionField(usecaseName, httpVerb, route)
            ]));
    }

    // ------------------------------------------------------------
    // Update کلاس گروه موجود
    // ------------------------------------------------------------
    private ClassDeclarationSyntax UpdateGroupClass(ClassDeclarationSyntax node)
    {
        var members = node.Members.ToList();

        // اطمینان از وجود Tag
        var tagField = members.OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f => f.Declaration.Variables.First().Identifier.Text == "Tag");

        if (tagField == null)
        {
            tagField = CreateTagField(groupName);
            members.Insert(0, tagField);
        }

        // حذف Endpoint موجود (اگر بود)
        var existing = members.OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f => f.Declaration.Variables.First().Identifier.Text == usecaseName);

        if (existing != null)
            members.Remove(existing);

        // اضافه کردن Endpoint جدید
        members.Add(CreateApiDescriptionField(usecaseName, httpVerb, route));

        // مرتب‌سازی Endpointها بدون Tag
        var ordered = members
            .Where(m => m != tagField)
            .OrderBy(GetSortKey)
            .ThenBy(GetSortKey2, StringComparer.Ordinal)
            .ToList();

        members = [];
        members.Add(tagField);
        members.AddRange(ordered);

        return node.WithMembers(List(members));
    }

    // ------------------------------------------------------------
    // ساخت Tag Field
    // ------------------------------------------------------------
    private static FieldDeclarationSyntax CreateTagField(string group)
    {
        return FieldDeclaration(
                VariableDeclaration(ParseTypeName("string"))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator("Tag")
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(group)))))))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword));
    }

    // ------------------------------------------------------------
    // ساخت Endpoint Field
    // ------------------------------------------------------------
    private static FieldDeclarationSyntax CreateApiDescriptionField(string name, HttpVerb verb, string route)
    {
        var httpVerb = verb.ToString().ToLower().Pascalize();
        var invocation =
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Api"),
                    IdentifierName(httpVerb)))
            .WithArgumentList(
                ArgumentList(SeparatedList(
                [
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(""))).WithNameColon(NameColon(IdentifierName("name"))),
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(""))).WithNameColon(NameColon(IdentifierName("displayName"))),
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(route))).WithNameColon(NameColon(IdentifierName("route"))),
                    Argument(IdentifierName("Tag")).WithNameColon(NameColon(IdentifierName("tag"))),
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(""))).WithNameColon(NameColon(IdentifierName("summary"))),
                    Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,IdentifierName("EndpointSecurityStore"),IdentifierName("Anonymous"))).WithNameColon(NameColon(IdentifierName("security"))),
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,Literal(""))).WithNameColon(NameColon(IdentifierName("description"))),
                ])));

        var equalsClause =
            EqualsValueClause(invocation);

        return FieldDeclaration(
                VariableDeclaration(ParseTypeName("ApiDescription"))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(name)
                        .WithInitializer(equalsClause))))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.StaticKeyword),
                Token(SyntaxKind.ReadOnlyKeyword))
            .WithLeadingTrivia(
                TriviaList(EndOfLine(Environment.NewLine)))
            .WithTrailingTrivia(
                TriviaList(EndOfLine(Environment.NewLine)));
    }

    // ------------------------------------------------------------
    // مرتب‌سازی Endpoint بر اساس HTTP Verb
    // ------------------------------------------------------------
    private static int GetSortKey(MemberDeclarationSyntax member)
    {
        if (member is not FieldDeclarationSyntax field)
            return int.MaxValue;

        var variables = field.Declaration.Variables;
        var declator = variables.FirstOrDefault();
        if (declator == null)
            return int.MaxValue;
        var equalsValueClauseSyntax = declator.Initializer;
        if (equalsValueClauseSyntax?.Value is not InvocationExpressionSyntax invocationExpressionSyntax)
            return int.MinValue;
        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax simpleMemberAccessExpressionSyntax)
            return int.MaxValue;
        var httpVerb = simpleMemberAccessExpressionSyntax.Name.Identifier.Text;
        int order = httpVerb.ToUpper() switch
        {
            "GET" => 0,
            "POST" => 1,
            "PUT" => 2,
            "PATCH" => 3,
            "DELETE" => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(httpVerb), httpVerb, "Specified argument was out of the range of valid values.")
        };

        return order;
    }
    private static string GetSortKey2(MemberDeclarationSyntax member)
    {
        if (member is not FieldDeclarationSyntax field)
            return "\uFFFF"; // push to end

        return field.Declaration.Variables
            .FirstOrDefault()?.Identifier.Text
            ?? "\uFFFF";
    }


    private static List<MemberDeclarationSyntax> NormalizeAllGroups(
    List<MemberDeclarationSyntax> members)
    {
        var groups = members
            .OfType<ClassDeclarationSyntax>()
            .OrderBy(c => c.Identifier.Text, StringComparer.Ordinal)
            .Select(NormalizeGroupMembers)
            .Cast<MemberDeclarationSyntax>()
            .ToList();

        var others = members
            .Where(m => m is not ClassDeclarationSyntax)
            .ToList();

        var result = new List<MemberDeclarationSyntax>();
        result.AddRange(groups);
        result.AddRange(others);

        return result;
    }
    private static ClassDeclarationSyntax NormalizeGroupMembers(
    ClassDeclarationSyntax group)
    {
        var members = group.Members.ToList();

        var tag = members
            .OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f =>
                f.Declaration.Variables.First().Identifier.Text == "Tag");

        var endpoints = members
            .OfType<FieldDeclarationSyntax>()
            .Where(f =>
                f.Declaration.Variables.First().Identifier.Text != "Tag")
            .OrderBy(GetSortKey)
            .ThenBy(GetSortKey2, StringComparer.Ordinal)
            .Cast<MemberDeclarationSyntax>()
            .ToList();

        var newMembers = new List<MemberDeclarationSyntax>();

        if (tag != null)
            newMembers.Add(tag);

        newMembers.AddRange(endpoints);

        return group.WithMembers(List(newMembers));
    }
}
public static class ApiRoutesUpdater
{
    public static void Update(
        string filePath,
        string projectName,
        string groupName,
        string usecasename,
        HttpVerb verb,
        string route)
    {
        return;
        var source = File.ReadAllText(filePath);

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var rewriter = new ApiRoutesRewriter(projectName, groupName, usecasename, verb, route);
        var newRoot = rewriter.Visit(root);

        var workspace = new AdhocWorkspace();
        var formatted = Microsoft.CodeAnalysis.Formatting.Formatter.Format(newRoot, workspace);

        File.WriteAllText(filePath, formatted.ToFullString());
    }
}
