namespace MvcRoutes
{
    public interface IEndpointFormatter
    {
        void OutputHeader();
        void OutputEndpoint(Endpoint endpoint);
        void OutputGroup(string groupName);
    }
}