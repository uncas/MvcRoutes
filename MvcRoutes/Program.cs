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

            IEndpointFormatter formatter = new WikiLongFormatter();

            formatter.OutputHeader();
            var endpoints = new List<Endpoint>();
            foreach (RouteBase route in RouteTable.Routes)
            {
                var rt = (Route) route;
                string methodsString = GetMethodsString(rt);
                IEnumerable<ParameterInfo> parameters = GetParameters(rt);
                ActionDocumentation actionDocumentation = GetActionDocumentation(rt);

                var endpoint = new Endpoint
                                   {
                                       Documentation = actionDocumentation,
                                       Methods = methodsString,
                                       Url = rt.Url,
                                       Parameters = parameters
                                   };
                endpoints.Add(endpoint);
            }

            IEnumerable<IGrouping<string, Endpoint>> controllers =
                endpoints.GroupBy(e => e.Documentation.ControllerName).OrderBy(g => g.Key);

            foreach (var controller in controllers)
            {
                string key = controller.Key;
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                string groupName = key.Replace("Controller", string.Empty);
                formatter.OutputGroup(groupName.SplitUpperCaseToString());
                OutputEndpoints(formatter, controller.ToList().OrderBy(e => e.Documentation.Name));
            }

            //OutputEndpoints(formatter, endpoints);
        }

        private static void OutputEndpoints(IEndpointFormatter formatter, IEnumerable<Endpoint> endpoints)
        {
            foreach (Endpoint endpoint in endpoints)
            {
                if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Name))
                    formatter.OutputEndpoint(endpoint);
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

        private static IEnumerable<ParameterInfo> GetParameters(Route rt)
        {
            MethodInfo actionMethodInfo;
            try
            {
                actionMethodInfo = GetMethodInfo(rt);
            }
            catch
            {
                return null;
            }

            if (actionMethodInfo == null)
                return null;

            return actionMethodInfo.GetParameters();
        }

        private static ActionDocumentation GetActionDocumentation(Route rt)
        {
            var result = new ActionDocumentation();
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

            result.Name = actionMethodInfo.Name;
            result.ControllerName = actionMethodInfo.DeclaringType.Name;
            result.Summary = ExtractNodeContent(xmlComments.Summary);
            result.Example = ExtractNodeContent(xmlComments.Example);
            result.Remarks = ExtractNodeContent(xmlComments.Remarks);
            result.Return = ExtractNodeContent(xmlComments.Return);

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
    }
}