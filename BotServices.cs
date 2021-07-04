


using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;

namespace FundMasterBot
{
    public class BotServices : IBotServices
    {
        public BotServices(IConfiguration configuration, OrchestratorRecognizer dispatcher)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            // If includeApiResults is set to true, the full response from the LUIS api (LuisResult)
            // will be made available in the properties collection of the RecognizerResult
            LuisFindFieldRecognizer = CreateLuisRecognizer(configuration, "LuisFindFieldAppId");
            LuisAddToKnowledgeBaseRecognizer = CreateLuisRecognizer(configuration, "LuisAddToKnowledgeBaseAppId");
            LuisGetFieldHelpRecognizer = CreateLuisRecognizer(configuration, "LuisGetFieldHelpAppId");
            LuisGenerateFundCodeRecognizer = CreateLuisRecognizer(configuration, "LuisGenerateFundCodeAppId");

            Dispatch = dispatcher;

            QnAMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["QnAKnowledgebaseId"],
                EndpointKey = configuration["QnAEndpointKey"],
                Host = GetHostname(configuration["QnAEndpointHostName"])
            });
        }

        public OrchestratorRecognizer Dispatch { get; private set; }
        
        public QnAMaker QnAMaker { get; private set; }

        public LuisRecognizer LuisFindFieldRecognizer { get; private set; }

        public LuisRecognizer LuisAddToKnowledgeBaseRecognizer { get; private set; }

        public LuisRecognizer LuisGetFieldHelpRecognizer { get; private set; }

        public LuisRecognizer LuisGenerateFundCodeRecognizer { get; private set; }





        private LuisRecognizer CreateLuisRecognizer(IConfiguration configuration, string appIdKey)
        {
            var luisApplication = new LuisApplication(
                configuration[appIdKey],
                configuration["LuisAPIKey"],
                configuration["LuisAPIHostName"]);

            // Set the recognizer options depending on which endpoint version you want to use.
            // More details can be found in https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-migration-api-v3
            var recognizerOptions = new LuisRecognizerOptionsV2(luisApplication)
            {
                IncludeAPIResults = true,
                PredictionOptions = new LuisPredictionOptions()
                {
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            };

            return new LuisRecognizer(recognizerOptions);
        }

        private static string GetHostname(string hostname)
        {
            if (!hostname.StartsWith("https://"))
            {
                hostname = string.Concat("https://", hostname);
            }

            if (!hostname.EndsWith("/qnamaker") && !hostname.Contains("/v5.0"))
            {
                hostname = string.Concat(hostname, "/qnamaker");
            }

            return hostname;
        }
    }
}
