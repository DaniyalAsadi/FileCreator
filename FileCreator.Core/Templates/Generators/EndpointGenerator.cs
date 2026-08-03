// FileCreator.Core/Generators/EndpointGenerator.cs
using FileCreator.Core.Generators;
using FileCreator.Core.Templates.Factories;
using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templating;

namespace FileCreator.Core.Templates.Generators;

/// <summary>
/// Renders a FastEndpoints "Endpoint" class from an <see cref="EndpointTemplateModel"/>.
/// Compare this to the old SyntaxFactory-based EndpointGenerator: there is no branching
/// on RequestType/ResponseType here anymore — that decision-making already happened
/// once, in <see cref="EndpointTemplateModelFactory"/>, and its result is just data now.
/// </summary>
public sealed class EndpointGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<EndpointTemplateModel>(renderer)
{
    protected override string TemplateName => "endpoint.sbn";
}