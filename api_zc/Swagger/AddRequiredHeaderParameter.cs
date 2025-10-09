using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Adds a required header parameter to the OpenAPI operation.
/// </summary>
public class AddRequiredHeaderParameter : IOperationFilter
{
    /// <summary>
    /// Adds a required header parameter to the OpenAPI operation.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>();

        var controllerName = context.MethodInfo.DeclaringType?.Name;
        var actionName = context.MethodInfo.Name;

        // 你可以根據 controllerName 或 actionName 進行條件判斷
        operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
        {
            Name = "Database",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Required = true,
            Schema = new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "string"
            },
            Description = "This is a attribute in Accura MES connection xml"
        });

        operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
        {
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Required = true,
            Schema = new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "string"
            },
            Description = "Token"
        });

        // 只針對指定的 API (例如 Controller 名稱或 Action 名稱) 加上 Header
        if (actionName == "NestedStructureDataQueryTemplate")
        {
            operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
            {
                Name = "i18n",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Required = false,
                Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "string"
                },
                Description = "EX. \"zh-TW\""
            });
        }
    }
}