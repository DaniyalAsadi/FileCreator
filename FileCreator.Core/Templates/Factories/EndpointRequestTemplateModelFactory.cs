using FileCreator.Core.Templates.Models;
using FileCreator.Core.Templates.Models.Internals;

namespace FileCreator.Core.Templates.Factories;

public static class EndpointRequestTemplateModelFactory
{
    public static EndpointRequestTemplateModel Create(
        string ns,
        string useCaseNameSpace,
        string useCaseName,
        RequestType requestType,
        bool hasResponse,
        ResponseType responseType)
    {
        var isPagedQuery =
            requestType == RequestType.Query &&
            hasResponse &&
            responseType == ResponseType.PagedList;


        var properties = isPagedQuery
            ? new List<PropertyTemplateModel>
            {
                new()
                {
                    TypeName = "int",
                    Name = "PageIndex",
                    IsRequired = true
                },
                new()
                {
                    TypeName = "int",
                    Name = "PageSize",
                    IsRequired = true
                }
            }
            : [];


        var mapMethodName = $"MapTo{requestType}";


        return new EndpointRequestTemplateModel
        {
            Namespace = ns,

            UseCaseNamespace = useCaseNameSpace,

            Usings =
            [
                "SharedKernel"
            ],

            UseCaseName = useCaseName,


            RequestType = requestType,

            HasResponse = hasResponse,

            ResponseType = responseType,


            ClassName = $"{useCaseName}Request",


            TargetRequestTypeName =
                $"{useCaseName}{requestType}",


            MapMethodName = mapMethodName,


            HasPaging = isPagedQuery,


            FilterTypeName =
                $"{useCaseName}{requestType}Filter",


            Properties = properties,


            IsPagedQuery = isPagedQuery,


            MappingParameterName =
                isPagedQuery
                    ? "request"
                    : "req",


            MappingParameterTypeName =
                $"{useCaseName}Request"
        };
    }
}