using FileCreator.Core.Generators;
using FileCreator.Core.Helpers;
using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templating;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FileCreator.Core.Templates.Generators;

public class EndpointTestGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<EndpointTestTemplateModel>(renderer)
{
    protected override string TemplateName => "endpoint-test.sbn";
}