using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.Resources.NetStandard;

namespace FileCreator.Core.Rewriter;

public class EnumRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var enumName = node.Identifier.Text;

        // Process each enum member (field)
        var updatedMembers = node.Members.Select(member =>
        {
            var fieldName = member.Identifier.Text;
            var displayKey = $"Enum_{enumName}_{fieldName}";
            // Remove any existing DisplayAttribute
            var newMember = RemoveExistingDisplayAttribute(member);

            // Create a new DisplayAttribute with formatted enum name and field name
            var displayNameArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("Name"),
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(displayKey))));

            var resourceTypeArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("ResourceType"),
                SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName("LanguageManager")))
            );


            // Create the Display attribute using structured syntax
            var displayAttribute = SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName("Display")
                )
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                                    displayNameArgument,
                                    resourceTypeArgument
                        })
                    )
                );

            // Add the new attribute to the field
            newMember = newMember.AddAttributeLists(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(displayAttribute))
            );

            return newMember;
        })
            .ToList();

        // Replace the old members with updated ones
        return node.WithMembers(SyntaxFactory.SeparatedList(updatedMembers));
    }

    // Method to remove existing DisplayAttribute from the enum member
    private static EnumMemberDeclarationSyntax RemoveExistingDisplayAttribute(EnumMemberDeclarationSyntax member)
    {
        // Remove any existing DisplayAttribute
        var updatedAttributes = member.AttributeLists
            .Select(attributeList =>
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(
                        attributeList.Attributes.Where(attr =>
                            !(attr.Name.ToString() == "Display" || attr.Name.ToString().EndsWith(".Display"))
                        )
                    )
                )
            )
            .Where(list => list.Attributes.Any())
            .ToList();

        // Return the member with the filtered attributes
        return member.WithAttributeLists(SyntaxFactory.List(updatedAttributes));
    }
}
public sealed class ResxUpdater
{
    public void EnsureKeyExists(
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