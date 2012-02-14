using System.Collections.Generic;

namespace MvcRoutes
{
    public class ActionDocumentation
    {
        public string ControllerName { get; set; }
        public string Example { get; set; }
        public string Name { get; set; }
        public string Remarks { get; set; }
        public string Return { get; set; }
        public string Summary { get; set; }
        public Dictionary<string, string> Params { get; set; }
    }
}