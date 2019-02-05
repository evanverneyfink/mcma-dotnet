using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.Conform.DecideTranscodeRequirements
{
    public class Function
    {
        // Local Define
        private const string VIDEO_FORMAT = "AVC";
        private const string VIDEO_CODEC = "mp42";
        private const string VIDEO_CODEC_ISOM = "isom";
        private const int VIDEO_BITRATE_MB = 2;

        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        private static readonly int THESHOLD_SECONDS = int.Parse(Environment.GetEnvironmentVariable(nameof(THESHOLD_SECONDS)));
        
        private McmaHttpClient McmaHttp { get; } = new McmaHttpClient();

        private async Task<BMEssence> GetBmEssenceAsync(string url)
        {
            var response = await McmaHttp.GetAsync(url);
            return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<BMEssence>();
        }

        private double CalcSeconds(int hour, int minute, double seconds)
            => (hour * 60 * 60) + (minute * 60) + seconds;
 
        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = new ResourceManager(SERVICE_REGISTRY_URL);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 45
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to send notification: {0}", error);
            }

            var bme = await GetBmEssenceAsync(@event["data"]["bmEssence"].ToString());

            var technicalMetadata = bme.Get<JToken>("technicalMetadata");

            var ebuCoreMain = technicalMetadata["ebucore:ebuCoreMain"];
            var coreMetadata = ebuCoreMain["ebucore:coreMetadata"][0];
            var containerFormat = coreMetadata["ebucore:format"][0]["ebucore:containerFormat"][0];
            var duration = coreMetadata["ebucore:format"][0]["ebucore:duration"][0];

            var video = new
            {
                Codec = containerFormat["ebucore:codec"][0]["ebucore:codecIdentifier"][0]["dc:identifier"][0]["#value"],
                BitRate = coreMetadata["ebucore:format"][0]["ebucore:videoFormat"][0]["ebucore:bitRate"][0]["#value"],
                Format = coreMetadata["ebucore:format"][0]["ebucore:videoFormat"][0]["@videoFormatName"],
                NormalPlayTime = duration["ebucore:normalPlayTime"][0]["#value"]
            };

            var codec = video.Codec.ToString();
            var format = video.Format.ToString();
            var bitRate = double.Parse(video.BitRate.ToString());
            var mbyte = (bitRate / 8) / (1024 * 1024);

            if ((codec == VIDEO_CODEC || codec == VIDEO_CODEC_ISOM) && format == VIDEO_FORMAT && mbyte <= VIDEO_BITRATE_MB)
                return "none";

            var normalPlayTime = video.NormalPlayTime.ToString();
            var hour = Regex.Match(normalPlayTime, "(\\d*)H");
            var min = Regex.Match(normalPlayTime, "(\\d*)M");
            var sec = Regex.Match(normalPlayTime, "(\\d*)S");
            var totalSeconds = CalcSeconds(hour.Success ? int.Parse(hour.Groups[1].Captures[0].Value) : 0, min.Success ? int.Parse(min.Groups[1].Captures[0].Value) : 0, double.Parse(sec.Groups[1].Captures[0].Value));

            Console.WriteLine("[Total Seconds]: " + totalSeconds);

            if (totalSeconds <= THESHOLD_SECONDS)
                return "short";
            else
                return "long";
        }
    }
}