using FileCreator.Core.Generators;
using FileCreator.Core.Helpers;
using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templating;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Templates.Generators;

public class EndpointRequestValidatorGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<EndpointRequestValidatorTemplateModel>(renderer)
{
    protected override string TemplateName => "endpoint-request-validator.sbn";
}