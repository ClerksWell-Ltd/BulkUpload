using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BulkUpload.V17
{
    internal class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            options.SwaggerDoc(
                "bulk-upload",
                new OpenApiInfo
                {
                    Title = "Bulk Upload",
                    Version = "Latest",
                    Description = "Contains api endpoints for bulk upload of content and media"
                });

            // Add operation filter to handle file uploads
            options.OperationFilter<FileUploadOperationFilter>();
        }
    }

    /// <summary>
    /// Operation filter to handle IFormFile parameters in Swagger
    /// </summary>
    internal class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
                .ToList();

            if (!formFileParameters.Any())
                return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = formFileParameters.ToDictionary(
                                p => p.Name,
                                p => (IOpenApiSchema)new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "binary"
                                }
                            ),
                            Required = formFileParameters
                                .Where(p => p.IsRequired)
                                .Select(p => p.Name)
                                .ToHashSet()
                        }
                    }
                }
            };

            // Remove the [FromForm] parameters from the parameter list since they're now in RequestBody
            if (operation.Parameters != null)
            {
                var parametersToRemove = operation.Parameters
                    .Where(p => formFileParameters.Any(fp => fp.Name == p.Name))
                    .ToList();

                foreach (var parameter in parametersToRemove)
                {
                    operation.Parameters.Remove(parameter);
                }
            }
        }
    }
}
