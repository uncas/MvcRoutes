using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;

namespace MvcRoutes
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Count() != 1)
            {
                Console.WriteLine("Please send the path to your dll as the first argument");
                return;
            }

            CallRegisterRoutes(args);

            Console.WriteLine("|| URL || HTTP Methods || Parameters || Comments ||");
            foreach (RouteBase route in RouteTable.Routes)
            {
                var rt = (Route) route;
                string methodsString = GetMethodsString(rt);
                string parametersString = GetParameters(rt);

                Console.WriteLine(
                    "| {0} | {1} | {2} | |",
                    rt.Url.Replace("{", "\\{"),
                    methodsString,
                    parametersString);
            }
        }

        private static void CallRegisterRoutes(string[] args)
        {
            ReflectionUtil.AssemblyName = args[0];
            const string className = "MvcApplication";
            const string methodName = "RegisterRoutes";

            ReflectionUtil.CallMethod(methodName, className);
        }

        private static string GetMethodsString(Route rt)
        {
            string methodsString = string.Empty;
            foreach (var constraint in rt.Constraints)
            {
                if (constraint.Key == "HttpVerbs")
                {
                    List<string> allowedMethods = ((HttpMethodConstraint) constraint.Value).AllowedMethods.ToList();
                    methodsString = string.Join(", ", allowedMethods);
                }
            }

            if (methodsString == string.Empty)
            {
                methodsString = GetMethodsFromAttributes(rt);
            }
            return methodsString;
        }

        private static Tuple<string, string> GetControllerAndActionNameFromRoute(Route rt)
        {
            string controllername = string.Empty;
            string actionName = string.Empty;
            const string controllerKey = "controller";
            const string actionKey = "Action";
            if (rt.Defaults.ContainsKey(controllerKey))
            {
                controllername = rt.Defaults[controllerKey].ToString();
                controllername += "Controller";
            }

            if (rt.Defaults.ContainsKey(actionKey))
                actionName = rt.Defaults[actionKey].ToString();

            return new Tuple<string, string>(controllername, actionName);
        }

        private static string GetMethodsFromAttributes(Route rt)
        {
            if (rt.Defaults == null)
                return string.Empty;

            Tuple<string, string> controllerAndActionNameFromRoute = GetControllerAndActionNameFromRoute(rt);
            string controllerName = controllerAndActionNameFromRoute.Item1;
            string actionName = controllerAndActionNameFromRoute.Item2;

            if (string.IsNullOrEmpty(actionName))
                return string.Empty;

            Type controllerType = ReflectionUtil.GetType(controllerName);

            if (controllerType == null)
            {
                Console.WriteLine("Controller not found even though is it marked as a controller for an action: " +
                                  controllerName);
                return string.Empty;
            }
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = controllerType.GetMethod(actionName);
            }
            catch (Exception)
            {
                return "Unable to resolve the method am not that smart yet";
            }

            if (actionMethodInfo == null)
                return string.Empty;
            Attribute[] customAttributes = Attribute.GetCustomAttributes(actionMethodInfo);

            return GetMethodsFromAttributes(customAttributes);
        }

        private static string GetParameters(Route rt)
        {
            if (rt.Defaults == null)
                return string.Empty;

            Tuple<string, string> controllerAndActionNameFromRoute = GetControllerAndActionNameFromRoute(rt);
            string controllerName = controllerAndActionNameFromRoute.Item1;
            string actionName = controllerAndActionNameFromRoute.Item2;

            if (string.IsNullOrEmpty(actionName))
                return string.Empty;

            Type controllerType = ReflectionUtil.GetType(controllerName);

            if (controllerType == null)
            {
                Console.WriteLine("Controller not found even though is it marked as a controller for an action: " +
                                  controllerName);
                return string.Empty;
            }
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = controllerType.GetMethod(actionName);
            }
            catch (Exception)
            {
                return "Unable to resolve the method am not that smart yet";
            }

            if (actionMethodInfo == null)
                return string.Empty;
            ParameterInfo[] parameters = actionMethodInfo.GetParameters();

            return string.Join(", ", parameters.Select(p => p.Name));
        }

        private static string GetMethodsFromAttributes(IEnumerable<Attribute> customAttributes)
        {
            var attributeToMethodName = new Dictionary<string, string>
                                            {
                                                {"HttpGetAttribute", "GET"},
                                                {"HttpPostAttribute", "POST"},
                                                {"HttpDeleteAttribute", "DELETE"},
                                                {"HttpPutAttribute", "PUT"},
                                            };
            var httpMethodList = new List<string>();
            foreach (Attribute customAttribute in customAttributes)
            {
                string attributeName = customAttribute.GetType().Name;
                if (attributeToMethodName.ContainsKey(attributeName))
                {
                    httpMethodList.Add(attributeToMethodName[attributeName]);
                }
                else if (attributeName == "AcceptVerbsAttribute")
                {
                    httpMethodList.AddRange(((AcceptVerbsAttribute) customAttribute).Verbs);
                }
            }
            return string.Join(",", httpMethodList);
        }
    }
}