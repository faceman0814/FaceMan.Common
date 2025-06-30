using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Linq;

namespace FaceMan.DynamicWebAPI.Filters
{
    public class JsonBodyOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // 确认是否是POST方法并且操作定义中还没有RequestBody
            if (!apiDescription.HttpMethod.Equals("Get", StringComparison.OrdinalIgnoreCase) && operation.RequestBody == null)
            {
                var anyFromBody = apiDescription.ParameterDescriptions.Any(pd => pd.Source.Id.Equals("Body"));
                var controllerParameterInfo = apiDescription.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>();

                // 查找可能作为RequestBody传入的复杂类型参数
                var complexTypeParam = controllerParameterInfo.FirstOrDefault(p =>
                    p.ParameterInfo.ParameterType.IsClass &&
                    p.ParameterInfo.ParameterType != typeof(string) &&
                    !anyFromBody
                );

                if (complexTypeParam != null)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(complexTypeParam.ParameterInfo.ParameterType, context.SchemaRepository);
                    operation.Parameters.Clear();
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Content =
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = schema
                        }
                    }
                    };

                    // 删除已转换为RequestBody的参数
                    //operation.Parameters.Remove(p => p.Name == complexTypeParam.ParameterInfo.Name);
                }
            }


        }
    }
}
