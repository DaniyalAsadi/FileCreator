using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;

public static class MediatorRequestResponseTemplateModelFactory
{
    public static MediatorRequestResponseTemplateModel Create(
        string ns,
        string useCaseName,
        RequestType requestType)
    {
        return new MediatorRequestResponseTemplateModel
        {
            Namespace = ns,

            Usings = [],

            UseCaseName = useCaseName,

            RequestType = requestType,

            ClassName = $"{useCaseName}{requestType}Response"
        };
    }
}