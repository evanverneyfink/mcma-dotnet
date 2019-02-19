namespace Mcma.Core
{
    public class JobBase : McmaResource
    {
        public string Status { get; set; }

        public string StatusMessage { get; set; }

        public double? Progress { get; set; }

        public NotificationEndpoint NotificationEndpoint { get; set; }

        public JobParameterBag JobOutput { get; set; }
    }
}