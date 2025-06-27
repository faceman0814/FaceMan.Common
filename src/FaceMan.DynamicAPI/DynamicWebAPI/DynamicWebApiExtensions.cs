using FaceMan.DynamicWebAPI.Config;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System;

namespace FaceMan.DynamicWebAPI
{
    /// <summary>
    /// 动态WebAPI扩展类，用于在ASP.NET Core应用程序中添加动态WebAPI功能。
    /// </summary>
    public static class DynamicWebApiExtensions
    {
        /// <summary>
        /// 为IMvcBuilder添加动态WebAPI功能。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="param" cref="SwaggerConfigParam"></param>
        /// <returns></returns>
        public static IMvcBuilder AddDynamicWebApi(this IMvcBuilder builder, SwaggerConfigParam param)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // 配置应用程序部分管理器，添加自定义的控制器特性提供程序
            builder.ConfigureApplicationPartManager(applicationPartManager =>
            {
                applicationPartManager.FeatureProviders.Add(new ApplicationServiceControllerFeatureProvider());
                //applicationPartManager.FeatureProviders.Add(new ApplicationServiceControllerFeatureProvider());
                //applicationPartManager.FeatureProviders.Clear();
                //applicationPartManager.FeatureProviders.Add(new UnifiedControllerFeatureProvider());
            });

            // 配置MvcOptions，添加自定义的应用程序模型约定
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.Conventions.Add(new ApplicationServiceConvention(param));
            });

            return builder;
        }

        /// <summary>
        /// 为IMvcCoreBuilder添加动态WebAPI功能
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="param" cref="SwaggerConfigParam"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IMvcCoreBuilder AddDynamicWebApi(this IMvcCoreBuilder builder, SwaggerConfigParam param)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // 配置应用程序部分管理器，添加自定义的控制器特性提供程序
            builder.ConfigureApplicationPartManager(applicationPartManager =>
            {
                applicationPartManager.FeatureProviders.Add(new ApplicationServiceControllerFeatureProvider());
                //applicationPartManager.FeatureProviders.Clear();
                //applicationPartManager.FeatureProviders.Add(new UnifiedControllerFeatureProvider());
            });

            // 配置MvcOptions，添加自定义的应用程序模型约定
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.Conventions.Add(new ApplicationServiceConvention(param));
            });

            return builder;
        }
    }
}
