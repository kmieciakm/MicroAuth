using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Web;

public class AddSwaggerHeadersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var headers = context
            .MethodInfo
            .GetCustomAttributes(false)
            .Where(attr => attr.GetType() == typeof(SwaggerHeaderParameterAttribute))
            .Select(attr => (SwaggerHeaderParameterAttribute) attr);

        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        foreach (var header in headers)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = header.Name,
                In = ParameterLocation.Header,
                Description = header.Description,
                Required = header.Required
            });
        }
    }
}

public class SwaggerHeaderParameterAttribute : Attribute
{
    public SwaggerHeaderParameterAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public string Description { get; set; } = "";
    public bool Required { get; set; }
}