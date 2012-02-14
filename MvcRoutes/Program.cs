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
            foreach (RouteBase route in RouteTable.Routes)
            {
                var rt = (Route) route;
                string methodsString = GetMethodsString(rt);
                IEnumerable<ParameterInfo> parameters = GetParameters(rt);
                MethodDocumentation methodDocumentation = GetDocumentation(rt);

                var endpoint = new Endpoint
                                   {
                                       Documentation = methodDocumentation,
                                       Methods = methodsString,
                                       Url = rt.Url,
                                       Parameters = parameters
                                   };

                formatter.OutputEndpoint(endpoint);
            }
        }

        private static void OutputWikiHeader()
        {
            Console.WriteLine("|| URL || HTTP Methods || Parameters || Summary || Example ||");
        }

        private static void OutputEndpointInWikiFormat(Endpoint endpoint)
        {
            string parametersString = GetParametersString(endpoint.Parameters);
            Console.WriteLine(
                "| {0} | {1} | {2} | {3} | {4} |",
                endpoint.Url.Replace("{", "\\{"),
                endpoint.Methods,
                parametersString,
                endpoint.Documentation.Summary,
                endpoint.Documentation.Example);
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

        private static string GetParametersString(IEnumerable<ParameterInfo> parameters)
        {
            if (parameters == null)
                return string.Empty;
            return string.Join(", ", parameters.Select(p => p.Name));
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

        private static MethodDocumentation GetDocumentation(Route rt)
        {
            var result = new MethodDocumentation();
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

        #region Nested type: Endpoint

        private class Endpoint
        {
            public string Url { get; set; }
            public string Methods { get; set; }
            public IEnumerable<ParameterInfo> Parameters { get; set; }
            public MethodDocumentation Documentation { get; set; }
        }

        #endregion

        #region Nested type: IEndpointFormatter

        private interface IEndpointFormatter
        {
            void OutputHeader();
            void OutputEndpoint(Endpoint endpoint);
        }

        #endregion

        #region Nested type: MethodDocumentation

        private class MethodDocumentation
        {
            public string Example { get; set; }
            public string Summary { get; set; }
        }

        #endregion

        #region Nested type: WikiLongFormatter

        private class WikiLongFormatter : IEndpointFormatter
        {
            #region IEndpointFormatter Members

            public void OutputHeader()
            {
                Console.WriteLine("h2. Endpoints");
            }

            public void OutputEndpoint(Endpoint endpoint)
            {
                string parametersString = GetParametersString(endpoint.Parameters);
                string summary = endpoint.Documentation.Summary;
                Console.WriteLine(
                    @"
h3. {0}

| URL | {1} |
| HTTP Methods | {2} |
| Parameters | {3} |
| Summary | {4} |
| Example | {5} |
",
                    summary,
                    endpoint.Url.Replace("{", "\\{"),
                    endpoint.Methods,
                    parametersString,
                    summary,
                    endpoint.Documentation.Example);
            }

            #endregion
        }

        #endregion

        #region Nested type: WikiShortFormatter

        private class WikiShortFormatter : IEndpointFormatter
        {
            #region IEndpointFormatter Members

            public void OutputHeader()
            {
                Console.WriteLine("|| URL || HTTP Methods || Parameters || Summary || Example ||");
            }

            public void OutputEndpoint(Endpoint endpoint)
            {
                string parametersString = GetParametersString(endpoint.Parameters);
                Console.WriteLine(
                    "| {0} | {1} | {2} | {3} | {4} |",
                    endpoint.Url.Replace("{", "\\{"),
                    endpoint.Methods,
                    parametersString,
                    endpoint.Documentation.Summary,
                    endpoint.Documentation.Example);
            }

            #endregion
        }

        #endregion
    }
}