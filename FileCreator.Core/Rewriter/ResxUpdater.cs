using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Resources.NetStandard;
using Humanizer;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Core.Rewriter;

public sealed class ResxUpdater
{
    public static void EnsureKeyExists(
        string resxPath,
        string key,
        string defaultValue = "")
    {
        if (!File.Exists(resxPath))
            throw new FileNotFoundException("Resx file not found.", resxPath);

        var entries = new Dictionary<string, string>();

        using (var reader = new ResXResourceReader(resxPath))
        {
            foreach (DictionaryEntry entry in reader)
            {
                entries[(string)entry.Key] = entry.Value?.ToString() ?? "";
            }
        }

        if (entries.ContainsKey(key))
            return; // already exists (idempotent)

        entries[key] = defaultValue;
        using var writer = new ResXResourceWriter(resxPath);

        foreach (var kv in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            writer.AddResource(kv.Key, kv.Value);
        }

        writer.Generate();
    }
}


public class ResxDesignRewriter(string key) : CSharpSyntaxRewriter
{

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node == null) return null;
        if (node.Identifier.Text != "LanguageManager")
            return base.VisitClassDeclaration(node);

        string documentationComment = @$"
        /// <summary>
        ///   Looks up a localized string similar to {key}.
        /// </summary>
        ";


        var comment = ParseLeadingTrivia(documentationComment);
        //var travia = Trivia(comment);
        var publicKeyword = Token(SyntaxKind.PublicKeyword)
            .WithLeadingTrivia(comment);
        var staticKeyword = Token(SyntaxKind.StaticKeyword);
        var returnStatement = ReturnStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("ResourceManager"),
                                        IdentifierName("GetString")))
                                    .WithArgumentList(
                                        ArgumentList(SeparatedList(
                                            [
                                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(key))),
                                                Argument(IdentifierName("resourceCulture"))
                                            ])
                                        )
                                    )
                                );
        var property =
            PropertyDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)), key)
            .AddModifiers(publicKeyword, staticKeyword)
            .AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .AddBodyStatements(returnStatement));

        node = node.AddMembers(property);

        return node;
    }
}