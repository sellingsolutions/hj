namespace HubspotConnector.CrossCuttingConcerns
{
    public class HsAppSettings
    {
        public string Environment { get; set; }
        public string ApiKey { get; set; }
        public string DefaultPipeline { get; set; }
        public string DefaultDealStage { get; set; }
    }
}