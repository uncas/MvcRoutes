using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;

namespace MvcRoutes
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("Please send the path to your dll as the first argument");
                return;
            }

            CallRegisterRoutes(args);

            Console.WriteLine("|| URL || HTTP Methods || Parameters || Summary || Example ||");
            foreach (RouteBase route in RouteTable.Routes)
            {
                var rt = (Route) route;
                string methodsString = GetMethodsString(rt);
                string parametersString = GetParametersString(rt);
                Documentation documentation = GetDocumentation(rt);

                Console.WriteLine(
                    "| {0} | {1} | {2} | {3} | {4} |",
                    rt.Url.Replace("{", "\\{"),
                    methodsString,
                    parametersString,
                    documentation.Summary,
                    documentation.Example);
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

            if (string.IsNullOrWhiteSpace(methodsString))
                methodsString = GetMethodsFromAttributes(rt);

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
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = GetMethodInfo(rt);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            if (actionMethodInfo == null)
                return string.Empty;
            Attribute[] customAttributes = Attribute.GetCustomAttributes(actionMethodInfo);

            return GetMethodsFromAttributes(customAttributes);
        }

        private static MethodInfo GetMethodInfo(Route rt)
        {
            if (rt.Defaults == null)
                throw new Exception("No routes");

            Tuple<string, string> controllerAndActionNameFromRoute = GetControllerAndActionNameFromRoute(rt);
            string controllerName = controllerAndActionNameFromRoute.Item1;
            string actionName = controllerAndActionNameFromRoute.Item2;

            if (string.IsNullOrEmpty(actionName))
                throw new Exception("No action name");

            Type controllerType = ReflectionUtil.GetType(controllerName);

            if (controllerType == null)
            {
                throw new Exception("Controller not found even though is it marked as a controller for an action: " +
                                    controllerName);
            }

            try
            {
                return controllerType.GetMethod(actionName);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to resolve the method am not that smart yet", ex);
            }
        }

        private static string GetParametersString(Route rt)
        {
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = GetMethodInfo(rt);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            if (actionMethodInfo == null)
                return string.Empty;

            ParameterInfo[] parameters = actionMethodInfo.GetParameters();

            return string.Join(", ", parameters.Select(p => p.Name));
        }

        private static Documentation GetDocumentation(Route rt)
        {
            var result = new Documentation();
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = GetMethodInfo(rt);
            }
            catch
            {
                return result;
            }

            if (actionMethodInfo == null)
                return result;

            var xmlComments = new XmlComments(actionMethodInfo);

            result.Summary = ExtractNodeContent(xmlComments.Summary);
            result.Example = ExtractNodeContent(xmlComments.Example);

            return result;
        }

        private static string ExtractNodeContent(XmlNode summaryNode)
        {
            if (summaryNode == null || string.IsNullOrWhiteSpace(summaryNode.InnerText))
                return null;
            return summaryNode.InnerText.Trim();
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

        #region Nested type: Documentation

        private class Documentation
        {
            public string Example { get; set; }
            public string Summary { get; set; }
        }

        #endregion
    }
}