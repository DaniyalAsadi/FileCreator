using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class EndpointRequestValidatorTemplateModelFactory
{
    public static EndpointRequestValidatorTemplateModel Create(
        string ns,
        string useCaseName)
    {
        return new EndpointRequestValidatorTemplateModel
        {
            Namespace = ns,

            Usings =
            [
                "FluentValidation"
            ],

            ClassName = $"{useCaseName}Validator",

            RequestTypeName = $"{useCaseName}Request",

            BaseTypeName =
                $"Validator<{useCaseName}Request>",

            Rules =
            [
                "// RuleFor(x => x.SomeProperty).NotEmpty();"
            ]
        };
    }
}