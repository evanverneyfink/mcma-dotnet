namespace Mcma.Aws.Authentication
{
    public class AwsV4AuthContext
    {
        public string AccessKey { get; set; }

        public string SecretKey { get; set; }

        public string SessionToken { get; set; }

        public string Region { get; set; }
    }
}