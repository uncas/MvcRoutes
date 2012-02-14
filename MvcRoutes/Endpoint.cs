using System.Collections.Generic;
using System.Reflection;

namespace MvcRoutes
{
    public class Endpoint
    {
        public string Url { get; set; }
        public string Methods { get; set; }
        public IEnumerable<ParameterInfo> Parameters { get; set; }
        public ActionDocumentation Documentation { get; set; }
    }
}