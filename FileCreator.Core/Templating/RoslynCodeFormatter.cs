// FileCreator.Core/Templating/RoslynCodeFormatter.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace FileCreator.Core.Templating;

/// <summary>
/// Scriban templates are hand-indented text, which is good enough for review but not
/// as crisp as `dotnet format`. Rather than fight indentation inside .sbn files, we
/// re-parse the rendered text and let Roslyn's Formatter normalize whitespace — the
/// same trick GrpcScaffold.Core's templates rely on implicitly via consistent {{~ ~}}
/// whitespace control. This is the ONLY place Roslyn syntax APIs are used in the
/// generation pipeline; they never construct code, only reformat already-correct code.
/// </summary>
public static class RoslynCodeFormatter
{
    public static string Format(string csharpSource)
    {
        var tree = CSharpSyntaxTree.ParseText(csharpSource);
        var root = tree.GetRoot();

        using var workspace = new AdhocWorkspace();
        var formatted = Formatter.Format(root, workspace);

        return formatted.ToFullString();
    }
}