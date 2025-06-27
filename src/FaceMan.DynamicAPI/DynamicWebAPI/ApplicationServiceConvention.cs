using FaceMan.DynamicWebAPI.Config;
using FaceMan.DynamicWebAPI.Extensions;
using FaceMan.PublicLibrary;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
namespace FaceMan.DynamicWebAPI
{
    /// <summary>
    /// 自定义应用程序模型约定，用于配置继承了 IApplicationService的接口或标记了 DynamicWebApiAttribute 特性接口的控制器。
    /// </summary>
    public class ApplicationServiceConvention : IApplicationModelConvention
    {
        private readonly string _routePrefix;
        private readonly List<HttpMethodConfigure> _httpMethods;
        private readonly string[] _commonPostfixes;

        public ApplicationServiceConvention(SwaggerConfigParam param)
        {
            _routePrefix = param.ApiRoutePrefix ?? throw new ArgumentNullException(nameof(param.ApiRoutePrefix));
            _httpMethods = param.HttpMethods ?? throw new ArgumentNullException(nameof(param.HttpMethods));
            _commonPostfixes = param.CommonPostfixes ?? new[] { "AppService", "ApplicationService", "Service" };
        }

        /// <summary>
        /// 应用约定
        /// </summary>
        /// <param name="application"></param>
        public void Apply(ApplicationModel application)
        {
            //循环每一个控制器信息
            foreach (var controller in application.Controllers)
            {
                var controllerType = controller.ControllerType.AsType();

                //是否继承IApplicationService接口，或标记了DynamicWebApiAttribute特性
                if (!typeof(IApplicationService).IsAssignableFrom(controllerType) && !controllerType.IsDefined(typeof(DynamicWebApiAttribute), false))
                {
                    continue;
                }

                var findPostfix = _commonPostfixes.FirstOrDefault(postfix => controller.ControllerName.EndsWith(postfix, StringComparison.OrdinalIgnoreCase));
                //去除路径中的指定的后缀
                if (findPostfix != null)
                {
                    controller.ControllerName = controller.ControllerName.RemovePostFix(findPostfix);
                }

                // 动态控制器命名约定：I{ServiceName} 接口
                var interfaceName = $"I{controller.ControllerType.Name}";

                // 在程序集中查找匹配的接口
                var serviceInterface = controller.ControllerType.Assembly
                    .GetTypes()
                    .FirstOrDefault(t =>
                        t.IsInterface &&
                        t.Name == interfaceName &&
                        typeof(IApplicationService).IsAssignableFrom(t));

                MethodInfo[] methods = [];
                //拿这个接口的方法定义
                if (serviceInterface != null)
                {
                    methods = serviceInterface.GetMethods();
                }

                //Actions就是接口的方法
                foreach (var item in controller.Actions)
                {
                    var action = methods.FirstOrDefault(t => t.Name == item.ActionName);
                    if (action != null)
                    {
                        ConfigureSelector(controller.ControllerName, item);
                    }
                }
            }
        }

        /// <summary>
        /// 配置选择器
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="action"></param>
        private void ConfigureSelector(string controllerName, ActionModel action)
        {
            var toRemove = new List<SelectorModel>();

            foreach (var selector in action.Selectors)
            {
                //判断是否有HttpMethodAttribute
                var httpAttributes = selector.ActionConstraints.Any();
                if (httpAttributes)
                {
                    string routePath = string.Concat(_routePrefix + "/", controllerName + "/", action.ActionName).Replace("//", "/");
                    var routeModel = new AttributeRouteModel(new RouteAttribute(routePath));
                    //如果没有设置路由，则添加路由
                    if (selector.AttributeRouteModel == null)
                    {
                        selector.AttributeRouteModel = routeModel;
                    }
                }
                else if (selector.AttributeRouteModel == null)
                {
                    toRemove.Add(selector);
                }
            }

            foreach (var item in toRemove)
            {
                action.Selectors.Remove(item);
            }

            if (!action.Selectors.Any())
            {
                action.Selectors.Add(CreateActionSelector(controllerName, action));
            }

        }

        /// <summary>
        /// 创建Action选择器
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private SelectorModel CreateActionSelector(string controllerName, ActionModel action)
        {
            var selectorModel = new SelectorModel();
            var actionName = action.ActionName;
            string httpMethod = string.Empty;

            //大写方法名
            var methodName = action.ActionMethod.Name.ToUpper();
            //遍历HttpMethodInfo配置，匹配方法名
            foreach (var item in _httpMethods)
            {
                foreach (var method in item.MethodVal)
                {
                    if (methodName.StartsWith(method.ToUpper()))
                    {
                        httpMethod = item.MethodKey;
                        goto end;
                    }

                }
            }
            //如果没有找到对应的HttpMethod，默认使用POST
            if (httpMethod == string.Empty)
            {
                httpMethod = "POST";
            }
        end:

            return ConfigureSelectorModel(selectorModel, action, controllerName, httpMethod);
        }

        /// <summary>
        /// 配置选择器模型
        /// </summary>
        /// <param name="selectorModel"></param>
        /// <param name="action"></param>
        /// <param name="controllerName"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        public SelectorModel ConfigureSelectorModel(SelectorModel selectorModel, ActionModel action, string controllerName, string httpMethod)
        {
            var routePath = string.Concat(_routePrefix + "/", controllerName + "/", action.ActionName).Replace("//", "/");
            //给此选择器添加路由
            selectorModel.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(routePath));
            //添加HttpMethod
            selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
            return selectorModel;
        }
    }
}
