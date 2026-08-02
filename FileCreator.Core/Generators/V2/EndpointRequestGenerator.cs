using FileCreator.Core.Models;
using FileCreator.Core.Templating;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Core.Generators.V2;

internal class EndpointRequestGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<EndpointTemplateModel>(renderer)
{
    protected override string TemplateName => "EndpointRequest.sbn";
}
