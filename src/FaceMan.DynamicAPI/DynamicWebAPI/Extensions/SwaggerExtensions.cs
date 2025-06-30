using FaceMan.DynamicWebAPI.Config;
using FaceMan.DynamicWebAPI.Filters;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Filters;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
namespace FaceMan.DynamicWebAPI.Extensions
{
    public static class SwaggerExtensions
    {
        private static SwaggerConfigParam _configParam = new SwaggerConfigParam()
        {
            Title = "FaceMan API",
            Version = "v1",
            Description = "FaceMan API",
            ContactName = "FaceMan",
            ContactEmail = "face<EMAIL>",
            ContactUrl = "https://www.face-man.com",
            EnableXmlComments = true,
            ApiDocsPath = "ApiDocs",
            EnableLoginPage = true,
            LoginPagePath = "pages/swagger.html",
            EnableApiResultFilter = true,
            EnableSimpleToken = false
        };
        /// <summary>
        /// 配置Swagger
        /// </summary>
        public static void AddSwagger(this IServiceCollection services, SwaggerConfigParam param)
        {
            // 启用 API 端点探索的功能
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                //添加响应头信息。它可以帮助开发者查看 API 响应中包含的 HTTP 头信息，从而更好地理解 API 的行为。
                options.OperationFilter<AddResponseHeadersFilter>();

                //摘要中添加授权信息。它会在每个需要授权的操作旁边显示一个锁图标，提醒开发者该操作需要身份验证。
                options.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                //加安全需求信息。它会根据 API 的安全配置（如 OAuth2、JWT 等）自动生成相应的安全需求描述，帮助开发者了解哪些操作需要特定的安全配置。
                options.OperationFilter<SecurityRequirementsOperationFilter>();

                //包装响应体。它会将 API 的响应体包装在一个统一的格式中，通常包括状态码、消息和数据等字段，以便于前端处理和展示。
                options.OperationFilter<WrapResponseOperationFilter>();

                //使Post请求的Body参数在Swagger UI中以Json格式显示。
                options.OperationFilter<JsonBodyOperationFilter>();


                if (param.EnableSimpleToken)
                {
                    //添加自定义请求头
                    options.OperationFilter<SwaggerHttpHeaderFilter>();
                }

                //自定义Schema出入参名称，避免冲突
                options.CustomSchemaIds(CustomSchemaIdSelector);

                ////显示枚举值
                options.DescribeAllEnumsAsStrings();
                //添加自定义文档信息
                options.SwaggerDoc(param.Version, new OpenApiInfo
                {
                    Title = param.Title,
                    Version = param.Version,
                    Description = param.Description,
                    Contact = new OpenApiContact()
                    {
                        Name = param.ContactName,
                        Email = param.ContactEmail,
                        Url = new Uri(param.ContactUrl)
                    }
                });

                //启用Swagger文档的XML注释
                if (param.EnableXmlComments)
                {
                    //遍历所有xml并加载
                    var binXmlFiles =
                        new DirectoryInfo(Path.Join(param.WebRootPath, param.ApiDocsPath))
                            .GetFiles("*.xml", SearchOption.TopDirectoryOnly);
                    foreach (var filePath in binXmlFiles.Select(item => item.FullName))
                    {
                        options.IncludeXmlComments(filePath, true);
                    }
                }
            });
        }
        /// <summary>
        ///  自定义SchemaId
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        static string CustomSchemaIdSelector(Type modelType)
        {
            // 1. 特别处理 ApiResponse<T> 类型
            if (modelType.IsGenericType &&
                modelType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                var dataType = modelType.GetGenericArguments()[0];
                return $"{CustomSchemaIdSelector(dataType)}ApiResponse";
            }

            // 2. 处理其他泛型类型
            if (modelType.IsConstructedGenericType)
            {
                var prefix = modelType.GetGenericArguments()
                    .Select(genericArg => CustomSchemaIdSelector(genericArg))
                    .Aggregate((previous, current) => previous + current);

                return prefix + modelType.FullName.Split('`').First();
            }

            // 3. 处理非泛型类型
            return modelType.FullName.Replace("[]", "Array");

            //if (!modelType.IsConstructedGenericType) return modelType.FullName.Replace("[]", "Array");

            //var prefix = modelType.GetGenericArguments()
            //    .Select(genericArg => CustomSchemaIdSelector(genericArg))
            //    .Aggregate((previous, current) => previous + current);

            //return prefix + modelType.FullName.Split('`').First();
        }
        /// <summary>
        /// 启用Swagger
        /// </summary>
        public static void UseDynamicSwagger(this WebApplication app)
        {
            //开发环境或测试环境才开启文档。
            if (app.Environment.IsDevelopment())
            {
                // 启用异常页面
                app.UseDeveloperExceptionPage();
                // 启用Swagger
                app.UseSwagger();
                // 启用SwaggerUI
                app.UseSwaggerUI(options =>
                {
                    options.RoutePrefix = _configParam.RoutePrefix;
                    // Swagger文档的URL地址
                    options.SwaggerEndpoint(_configParam.SwaggerEndpoint, _configParam.Version);
                    // 展开深度
                    options.DefaultModelExpandDepth(_configParam.DefaultModelExpandDepth);
                    //开启深层链接
                    if (_configParam.EnableDeepLinking)
                    {
                        options.EnableDeepLinking();
                    }
                    // 文档展开方式
                    options.DocExpansion(_configParam.DocExpansion);
                    // 开启登录页
                    if (_configParam.EnableLoginPage)
                    {
                        options.IndexStream = () =>
                        {
                            var path = Path.Join(_configParam.WebRootPath, _configParam.LoginPagePath);
                            return new FileInfo(path).OpenRead();
                        };
                    }
                });

            }
        }

        public static void AddDynamicApi(this IServiceCollection services, string webRootPath, SwaggerConfigParam configParam = null)
        {
            var basePath = AppContext.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                           .SetBasePath(basePath)
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           .Build();
            //获取wwwroot路径
            if (configParam != null)
            {
                _configParam = configParam;
            }

            _configParam.WebRootPath = webRootPath;
            _configParam.HttpMethods = configuration.GetSection("HttpMethodInfo").Get<List<HttpMethodConfigure>>();
            services.AddMvcCore(x =>
            {
                if (_configParam.EnableApiResultFilter)
                {
                    //全局返回，异常处理，统一返回格式。
                    x.Filters.Add<GlobalActionFilterAttribute>();
                }
                //解析Post请求参数，将json反序列化赋值参数
                x.Filters.Add(new AutoFromBodyActionFilter());
            });

            services.AddControllers(options =>
            {
                options.ModelBinderProviders.Insert(0, new CustomModelBinderProvider());
            })
            .AddJsonOptions(options =>
            {
                //时间格式化响应
                options.JsonSerializerOptions.Converters.Add(new JsonOptionsDate(_configParam.DatetimeFormat));
                // 使用PascalCase属性名,动态API才能拿到值。
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                //禁止字符串被转义成Unicode
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            });

            services.AddMvc(options => { })
                    .AddRazorPagesOptions((options) => { })
                    //用于添加 Razor 视图的运行时编译功能
                    .AddRazorRuntimeCompilation()
                    .AddDynamicWebApi(_configParam);

            services.AddSwagger(_configParam);
        }
    }
}
