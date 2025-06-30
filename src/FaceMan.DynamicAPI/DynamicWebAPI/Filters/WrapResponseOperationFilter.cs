using FaceMan.DynamicWebAPI.Extensions;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Threading.Tasks;
namespace FaceMan.DynamicWebAPI.Filters
{
    public class WrapResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var responseType = context.MethodInfo.ReturnType;

            if (responseType == typeof(Task))
            {
                // 处理返回类型为Task的情况（即异步void）
                operation.Responses.Clear();
                operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "Success - No return data"
                });
                return;
            }

            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                responseType = responseType.GetGenericArguments()[0]; // 取Task<T>的T类型
            }

            if (responseType != typeof(void))
            {
                var wrappedType = typeof(ApiResponse<>).MakeGenericType(responseType);
                operation.Responses.Clear(); // 清除原始生成的响应类型

                operation.Responses.Add("200", new OpenApiResponse
                {
                    Description = "Success",
                    Content = { ["application/json"] = new OpenApiMediaType
                    { Schema = context.SchemaGenerator.GenerateSchema(wrappedType, context.SchemaRepository) } }
                });
            }
            else
            {
                // 处理返回类型为void的情况
                operation.Responses.Clear();
                operation.Responses.Add("204", new OpenApiResponse
                {
                    Description = "No Content - Method does not return any data"
                });
            }
        }
    }
}