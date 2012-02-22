using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace MvcRoutes
{
    public class WikiLongFormatter : IEndpointFormatter
    {
        #region IEndpointFormatter Members

        public void OutputHeader()
        {
            Console.WriteLine(@"{toc}

h1. Endpoints");
        }

        private static string WikiEscapeText(string input)
        {
            return input.Replace("{", "\\{");
        }

        public void OutputEndpoint(Endpoint endpoint)
        {
            string summary = endpoint.Documentation.Summary;
            string name = endpoint.Documentation.Name;
            Console.WriteLine(
                @"

h3. {0}

| URL | {1} |
| HTTP Methods | {2} |
| Summary | {3} |",
                FormatName(name),
                WikiEscapeText(endpoint.Url),
                endpoint.Methods,
                summary);

            if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Return))
                Console.WriteLine("| Returns | {0} |", endpoint.Documentation.Return);

            if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Example))
                Console.WriteLine("| Example | {0} |", endpoint.Documentation.Example);

            if (!string.IsNullOrWhiteSpace(endpoint.Documentation.Remarks))
                Console.WriteLine("{0}Remarks: {1}{0}", Environment.NewLine, WikiEscapeText(endpoint.Documentation.Remarks));

            Dictionary<string, string> parameterDocs = endpoint.Documentation.Params;

            if (parameterDocs.Any())
            {
                Console.WriteLine(@"
|| Parameter || Description ||");
                foreach (var parameter in parameterDocs)
                {
                    Console.WriteLine(
                        "| {0} | {1} |",
                        parameter.Key,
                        parameter.Value);
                }
            }
            else if (endpoint.Parameters != null && endpoint.Parameters.Any())
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

        public void OutputGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;
            Console.WriteLine(@"

h2. {0}
", groupName);
        }

        #endregion

        private static string FormatName(string name)
        {
            return name.SplitUpperCaseToString();
        }

        private static string GetActionDocumentation(ParameterInfo parameter)
        {
            string parameterName = parameter.Name;
            var xmlComments = new XmlComments(parameter.Member);

            return GetParameterDocumentation(xmlComments.Params, parameterName);
        }

        private static string GetParameterDocumentation(
            XmlNodeList nodes,
            string parameterName)
        {
            foreach (XmlNode node in nodes)
            {
                if (node.Attributes == null)
                    continue;
                XmlAttribute xmlAttribute = node.Attributes["name"];
                if (xmlAttribute == null)
                    continue;
                if (xmlAttribute.InnerText == parameterName)
                    return node.InnerText;
            }

            return string.Empty;
        }

        public static Dictionary<string, string> GetParameterDocumentations(
            XmlNodeList nodes)
        {
            var result = new Dictionary<string, string>();
            foreach (XmlNode node in nodes)
            {
                if (node.Attributes == null)
                    continue;
                XmlAttribute xmlAttribute = node.Attributes["name"];
                if (xmlAttribute == null)
                    continue;
                result.Add(xmlAttribute.InnerText, node.InnerText);
            }

            return result;
        }
    }
}