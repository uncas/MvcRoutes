using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MvcRoutes
{
    public class WikiShortFormatter : IEndpointFormatter
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

        public void OutputGroup(string groupName)
        {
        }

        #endregion

        private static string GetParametersString(IEnumerable<ParameterInfo> parameters)
        {
            if (parameters == null)
                return string.Empty;
            return string.Join(", ", parameters.Select(p => p.Name));
        }
    }
}