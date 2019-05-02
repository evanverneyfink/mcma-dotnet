namespace Mcma.Aws.AwsAiService.Worker
{
    internal class ProcessRekognitionResult
    {
        public string JobAssignmentId { get; set; }

        public RekognitionJobInfo JobInfo { get; set; }

        internal class RekognitionJobInfo
        {
            public string RekoJobId { get; set; }

            public string RekoJobType { get; set; }

            public string Status { get; set; }
        }
    }
}
