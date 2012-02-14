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
                ActionDocumentation actionDocumentation = GetActionDocumentation(rt);

                var endpoint = new Endpoint
                                   {
                                       Documentation = actionDocumentation,
                                       Methods = methodsString,
                                       Url = rt.Url,
                                       Parameters = parameters
                                   };
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

            return result;
        }

        private static string GetActionDocumentation(ParameterInfo parameter)
        {
            var xmlComments = new XmlComments(parameter.Member);

            XmlNodeList nodes = xmlComments.Params;
            foreach (XmlNode node in nodes)
            {
                if (node.Attributes == null)
                    continue;
                XmlAttribute xmlAttribute = node.Attributes["name"];
                if (xmlAttribute == null)
                    continue;
                if (xmlAttribute.InnerText == parameter.Name)
                    return node.InnerText;
            }

            return string.Empty;
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

        #region Nested type: ActionDocumentation

        private class ActionDocumentation
        {
            public string ControllerName { get; set; }
            public string Example { get; set; }
            public string Name { get; set; }
            public string Remarks { get; set; }
            public string Summary { get; set; }
        }

        #endregion

        #region Nested type: Endpoint

        private class Endpoint
        {
            public string Url { get; set; }
            public string Methods { get; set; }
            public IEnumerable<ParameterInfo> Parameters { get; set; }
            public ActionDocumentation Documentation { get; set; }
        }

        #endregion

        #region Nested type: IEndpointFormatter

        private interface IEndpointFormatter
        {
            void OutputHeader();
            void OutputEndpoint(Endpoint endpoint);
        }

        #endregion

        #region Nested type: WikiLongFormatter

        private class WikiLongFormatter : IEndpointFormatter
        {
            #region IEndpointFormatter Members

            public void OutputHeader()
            {
                Console.WriteLine(@"{toc}

h2. Endpoints");
            }

            public void OutputEndpoint(Endpoint endpoint)
            {
                string parametersString = GetParametersString(endpoint.Parameters);
                string summary = endpoint.Documentation.Summary;
                string name = endpoint.Documentation.Name;
                Console.WriteLine(
                    @"

h3. {0}

| URL | {1} |
| HTTP Methods | {2} |
| Summary | {3} |",
                    FormatName(name),
                    endpoint.Url.Replace("{", "\\{"),
                    endpoint.Methods,
                    summary);

                //if (!string.IsNullOrWhiteSpace(parametersString))
                //    Console.WriteLine("| Parameters | {0} |", parametersString);

                if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Example))
                    Console.WriteLine("| Example | {0} |", endpoint.Documentation.Example);

                if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Remarks))
                    Console.WriteLine("Remarks: {1}{0}", Environment.NewLine, endpoint.Documentation.Remarks);

                if (endpoint.Parameters != null && endpoint.Parameters.Any())
                {
                    Console.WriteLine(@"
|| Parameter || Description ||");
                    foreach (ParameterInfo parameter in endpoint.Parameters)
                    {
                        Console.WriteLine(
                            "| {0} | {1} |",
                            parameter.Name,
                            GetActionDocumentation(parameter));
                    }
                }
            }

            #endregion

            private static string FormatName(string name)
            {
                return name.SplitUpperCaseToString();
            }
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