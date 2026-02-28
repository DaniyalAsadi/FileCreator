using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Walker;

public sealed class EnumKeyCollector : CSharpSyntaxWalker
{
    private readonly HashSet<string> _keys = new();

    public IReadOnlyCollection<string> Keys => _keys;

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var enumName = node.Identifier.Text;

        foreach (var member in node.Members)
        {
            var fieldName = member.Identifier.Text;
            var key = $"Enum_{enumName}_{fieldName}";
            _keys.Add(key);
        }

        base.VisitEnumDeclaration(node);
    }
}