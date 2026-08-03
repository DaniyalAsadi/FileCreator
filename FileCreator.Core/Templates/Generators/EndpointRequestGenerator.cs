using FileCreator.Core.Generators;
using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templating;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Templates.Generators;

internal class EndpointRequestGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<EndpointRequestTemplateModel>(renderer)
{
    protected override string TemplateName => "endpoint-request.sbn";
}
