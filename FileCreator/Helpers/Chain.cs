using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace FileCreator.Helpers;

public sealed class Chain
{
    private ExpressionSyntax _current;

    private Chain(ExpressionSyntax root)
    {
        _current = root;
    }

    public static Chain From(string identifier)
        => new(IdentifierName(identifier));

    public static Chain From(ExpressionSyntax expression)
        => new(expression);

    public Chain Member(string name)
    {
        _current = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            _current,
            IdentifierName(name));

        return this;
    }

    public Chain Call(string methodName, params ExpressionSyntax[] args)
    {
        var member = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            _current,
            IdentifierName(methodName));

        _current = InvocationExpression(member)
            .WithArgumentList(
                ArgumentList(
                    SeparatedList(args.Select(Argument))));

        return this;
    }

    public Chain Index(ExpressionSyntax index)
    {
        _current = ElementAccessExpression(_current)
            .WithArgumentList(
                BracketedArgumentList(
                    SingletonSeparatedList(Argument(index))));

        return this;
    }

    public ExpressionSyntax Build() => _current;
}

