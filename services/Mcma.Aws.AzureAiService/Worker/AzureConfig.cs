namespace Mcma.Aws.AzureAiService.Worker
{
    internal class AzureConfig
    {
        public AzureConfig(AzureAiServiceWorkerRequest @event)
        {
            ApiUrl = @event.Request.StageVariables["AzureApiUrl"];
            Location = @event.Request.StageVariables["AzureLocation"];
            AccountID = @event.Request.StageVariables["AzureAccountID"];
            SubscriptionKey = @event.Request.StageVariables["AzureSubscriptionKey"];
        }

        public string ApiUrl { get; }

        public string Location { get; }

        public string AccountID { get; }

        public string SubscriptionKey { get; }
    }
}
