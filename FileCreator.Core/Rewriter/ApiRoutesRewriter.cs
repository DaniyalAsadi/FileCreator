using FileCreator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Rewriter;

public sealed class ApiRoutesRewriter(
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
        // فقط کلاس ریشه ApiRoutes
        if (node.Identifier.Text != "ApiRoutes")
            return base.VisitClassDeclaration(node);

        var members = node.Members.ToList();

        // پیدا کردن کلاس گروه (مثلاً Communications)
        var groupClass = members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == groupName);

        if (groupClass == null)
        {
            // اگر کلاس گروه وجود نداشت → بساز
            groupClass = CreateGroupClass();
            members.Add(groupClass);
        }
        else
        {
            // اگر بود → Update
            groupClass = UpdateGroupClass(groupClass);
            var idx = members.FindIndex(m => m == members.OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == groupName));
            members[idx] = groupClass;
        }
        var classMembers = members.OfType<ClassDeclarationSyntax>()
                          .OrderBy(c => c.Identifier.Text) // Sort alphabetically
                          .ToList();

        // نگه‌داشتن بقیه اعضا (اگر باشد)
        var otherMembers = members.Where(m => m is not ClassDeclarationSyntax).ToList();

        // ترکیب دوباره
        members = [];
        members.AddRange(classMembers);
        members.AddRange(otherMembers);

        return node.WithMembers(List(members));

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
        return FieldDeclaration(
            VariableDeclaration(ParseTypeName("ApiDescription"))
            .WithVariables(
                SingletonSeparatedList(
                    VariableDeclarator(name)
                    .WithInitializer(
                        EqualsValueClause(
                            ImplicitObjectCreationExpression()
                            .WithArgumentList(
                                ArgumentList(SeparatedList(
                                [
                                    Argument(ParseExpression($"Http.{verb.ToString().ToUpper()}")),
                                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(route)))
                                ]))))))))
            .AddModifiers(Token(SyntaxKind.PublicKeyword),
                          Token(SyntaxKind.StaticKeyword),
                          Token(SyntaxKind.ReadOnlyKeyword));
    }

    // ------------------------------------------------------------
    // مرتب‌سازی Endpoint بر اساس HTTP Verb
    // ------------------------------------------------------------
    private static int GetSortKey(MemberDeclarationSyntax member)
    {
        if (member is not FieldDeclarationSyntax field)
            return int.MaxValue;

        var text = field.ToString();

        if (text.Contains("Http.GET")) return 0;
        if (text.Contains("Http.POST")) return 1;
        if (text.Contains("Http.PUT")) return 2;
        if (text.Contains("Http.PATCH")) return 3;
        if (text.Contains("Http.DELETE")) return 4;

        return 10;
    }
}

public static class ApiRoutesUpdater
{
    public static void Update(
        string filePath,
        string groupName,
        string usecasename,
        HttpVerb verb,
        string route)
    {
        var source = File.ReadAllText(filePath);

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var rewriter = new ApiRoutesRewriter(groupName, usecasename, verb, route);
        var newRoot = rewriter.Visit(root);

        var workspace = new AdhocWorkspace();
        var formatted = Microsoft.CodeAnalysis.Formatting.Formatter.Format(newRoot, workspace);

        File.WriteAllText(filePath, formatted.ToFullString());
    }
}
