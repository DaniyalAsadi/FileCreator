
using FileCreator.Core.Templates.Models;

namespace FileCreator.Core.Templates.Factories;
public static class MediatorRequestFiltersTemplateModelFactory
{
    public static MediatorRequestFiltersTemplateModel Create(
        string ns,
        string useCaseName,
        RequestType requestType)
    {
        return new MediatorRequestFiltersTemplateModel
        {
            Namespace = ns,

            Usings = [],

            UseCaseName = useCaseName,

            RequestType = requestType,

            ClassName = $"{useCaseName}{requestType}Filter"
        };
    }
}