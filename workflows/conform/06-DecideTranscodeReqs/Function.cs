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
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.DecideTranscodeReqs
{
    public class Function
    {
        // Local Define
        private const string VIDEO_FORMAT = "AVC";
        private const string VIDEO_CODEC = "mp42";
        private const string VIDEO_CODEC_ISOM = "isom";
        private const int VIDEO_BITRATE_MB = 2;

        private static readonly int THRESHOLD_SECONDS = int.Parse(Environment.GetEnvironmentVariable("THESHOLD_SECONDS"));

        private double CalcSeconds(int hour, int minute, double seconds)
            => (hour * 60 * 60) + (minute * 60) + seconds;
 
        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());

            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

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
                Logger.Error("Failed to send notification: {0}", error);
            }

            var bme = await resourceManager.ResolveAsync<BMEssence>(@event["data"]["bmEssence"].ToString());

            var technicalMetadata = bme.Get<object>("technicalMetadata", false).ToMcmaJson();

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
            
            var totalSeconds =
                CalcSeconds(
                    hour.Success ? int.Parse(hour.Groups[1].Captures[0].Value) : 0,
                    min.Success ? int.Parse(min.Groups[1].Captures[0].Value) : 0,
                    double.Parse(sec.Groups[1].Captures[0].Value));

            Logger.Debug("[Total Seconds]: " + totalSeconds);

            return totalSeconds <= THRESHOLD_SECONDS ? "short" : "long";
        }
    }
}