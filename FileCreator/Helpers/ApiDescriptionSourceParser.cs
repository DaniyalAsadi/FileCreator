using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Helpers;

public static class ApiDescriptionSourceParser
{
    public static IReadOnlyList<ImportedEndpoint> Parse(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var results = new List<ImportedEndpoint>();
        var className = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single();

        var fields = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>();


        foreach (var field in fields)
        {
            if (!field.Declaration.Type.ToString().Contains("ApiDescription"))
                continue;

            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Initializer?.Value is not ObjectCreationExpressionSyntax obj)
                    continue;

                var args = obj.ArgumentList!.Arguments;

                var verbExpression = args[0].ToString();   // Http.GET
                var routeExpression = args[1].ToString();  // "auth/register"

                var verb = ParseVerb(verbExpression);
                var route = routeExpression.Trim('"');

                results.Add(new ImportedEndpoint
                {
                    GroupName  = className.Identifier.Text,
                    Name = variable.Identifier.Text,
                    Route = route,
                    Verb = verb,

                    RequestType = verb == HttpVerb.GET
                        ? RequestType.Query
                        : RequestType.Command,

                    HasRequest = verb != HttpVerb.GET,
                    HasResponse = verb == HttpVerb.GET,
                    ResponseType = ResponseType.Single
                });
            }
        }

        return results;
    }

    private static HttpVerb ParseVerb(string text)
    {
        if (text.EndsWith("GET")) return HttpVerb.GET;
        if (text.EndsWith("POST")) return HttpVerb.POST;
        if (text.EndsWith("PUT")) return HttpVerb.PUT;
        if (text.EndsWith("DELETE")) return HttpVerb.DELETE;

        throw new NotSupportedException($"Unknown verb: {text}");
    }
}
