using FileCreator.Core.Generators;
using FileCreator.Core.Helpers;
using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templating;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace FileCreator.Core.Templates.Generators;

public class MediatorRequestHandlerGenerator(IScribanTemplateRenderer renderer)
    : ScribanCodeGenerator<MediatorRequestHandlerTemplateModel>(renderer)
{
    protected override string TemplateName => "mediator-request-handler.sbn";
}