


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FundMasterBot.Dialog;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IQnAMakerClient = Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.IQnAMakerClient;

namespace FundMasterBot.Bot
{
    public class FundMasterBot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        private readonly ILogger<FundMasterBot<T>> _logger;
        private readonly IBotServices _botServices;
        public UserState UserState { get; }
        public ConversationState ConversationState { get; }
        private readonly IConfiguration _configuration;
        protected readonly RootDialog Dialog;


        public FundMasterBot(IBotServices botServices, IConfiguration configuration, ILogger<FundMasterBot<T>> logger,
            ConversationState conversationState, UserState userState, RootDialog dialog)
        {
            _logger = logger;
            _botServices = botServices;
            ConversationState = conversationState;
            UserState = userState;
            _configuration = configuration;
            Dialog = dialog;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            if (turnContext.Activity.Summary == "Unknown answer" || profile.FundCodeGenerationStarted ||
                profile.StartHelp || turnContext.Activity.Summary == "Help" ||
                turnContext.Activity.Summary == "SelectingHelpOption" || turnContext.Activity.Summary == "EndingHelp" ||
                turnContext.Activity.Summary == "HelpChoiceValueEntered")
            {
                await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
        }


        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext,
            CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Type == "event" && turnContext.Activity.Value.ToString() == "promptForFundCode")
            {
                var conversationStateAccessors =
                    ConversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
                var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(),
                    cancellationToken);

                var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
                var profile =
                    await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
                await PromptForFundCodeAsync(flow, profile, turnContext, cancellationToken);

                // Save changes.
                await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }

            if (turnContext.Activity.Type == "event" && turnContext.Activity.Name == "showFundCode")
            {
                var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
                var profile =
                    await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

                profile.FundCodeTypeCode = turnContext.Activity.Value.ToString().Split(":")[0];
                profile.GeneratedFundCode = turnContext.Activity.Value.ToString().Split(":")[1];

                // Save changes.
                await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }

            if (turnContext.Activity.Type == "event" && turnContext.Activity.Name == "fundCodeGenerationComplete")
            {
                var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));

                var profile =
                    await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

                profile.FundCodeGenerationStarted = false;
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            }

            if (turnContext.Activity.Type == "event" && turnContext.Activity.Name == "getRbacAllowed")
            {
                var fieldName = turnContext.Activity.Value.ToString().Split(":")[0];
                var allowed = turnContext.Activity.Value.ToString().Split(":")[1];


                var conversationStateAccessors =
                    ConversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
                var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(),
                    cancellationToken);


                flow.LastQuestionAsked = ConversationFlow.Question.HelpGenerateFundCode;

                if (fieldName == "FundCodeGenerator")
                {
                    if (allowed == "true")
                    {
                        turnContext.Activity.Name = "ProcessGenerateFundCodeAsync";

                        await Dialog.RunAsync(turnContext,
                            ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                        await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                    }
                    else
                    {
                        flow.LastQuestionAsked = ConversationFlow.Question.None;
                        await turnContext.SendActivityAsync(MessageFactory.Text(
                                "You do not have access to this functionality.\n\nPlease contact xxx for access."),
                            cancellationToken);
                    }
                }
            }


            if (turnContext.Activity.Type == "event" && turnContext.Activity.Name == "getUsername")
            {
                var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
                var profile =
                    await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

                profile.Name = turnContext.Activity.Value.ToString();

                profile.OnStartup = true;

                await turnContext.SendActivityAsync(MessageFactory.Text(
                        "Hello " + profile.Name + "!\n\nWelcome to the Fund Master bot. " +
                        "\n\n If you require any assistance in using this tool or have any additional queries please contact the Fund Master project team at: xxx"),
                    cancellationToken);

                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var conversationStateAccessors =
                ConversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(),
                cancellationToken);

            var conversationStateAccessorsDialogState =
                ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            var dialogSet = new DialogSet(conversationStateAccessorsDialogState);
            var rootDialog = new RootDialog(_botServices, UserState, ConversationState);
            dialogSet.Add(rootDialog);
            var dialogContext = await dialogSet.CreateContextAsync(turnContext, cancellationToken);

            var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);
            var input = turnContext.Activity.Text.Trim();

            if (turnContext.Activity.Text == "Optional: Tell me the answer" && profile.Question != null)
            {
                profile.WantsToAddToKnowledgeBase = "Yes";
                await ProcessAddToKnowledgeBaseAsync(turnContext, cancellationToken);
                return;
            }

            if (string.Equals(turnContext.Activity.Text, "help", StringComparison.InvariantCultureIgnoreCase))
            {
                profile.StartHelp = true;

                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
                return;
            }


            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.Name:
                    profile.Name = input;
                    flow.LastQuestionAsked = ConversationFlow.Question.None;
                    await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Thanks. Now taking you to the " + profile.TabToNavigateTo + "."),
                        cancellationToken);
                    await turnContext.SendActivityAsync(
                        new Activity
                        {
                            Name = "FindField",
                            Value = profile.FieldToNavigateTo + ":" + profile.UrlLocator + ":" + profile.Name,
                            Type = ActivityTypes.Event
                        }, cancellationToken);
                    return;
                case ConversationFlow.Question.Question:
                case ConversationFlow.Question.Answer:
                    await ProcessAddToKnowledgeBaseAsync(turnContext, cancellationToken);
                    return;
                case ConversationFlow.Question.HelpGetFieldOwner:
                    profile.StartHelp = false;
                    flow.LastQuestionAsked = ConversationFlow.Question.None;
                    await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                    turnContext.Activity.Text = "Who owns the " + turnContext.Activity.Text + " field";
                    await ProcessQnAAsync(turnContext, cancellationToken);
                    return;
                case ConversationFlow.Question.HelpFindField:
                    profile.StartHelp = false;
                    flow.LastQuestionAsked = ConversationFlow.Question.None;
                    await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                    turnContext.Activity.Text = "Where is the " + turnContext.Activity.Text + " field";
                    await ProcessFindFieldAsync(turnContext, cancellationToken);
                    return;
                case ConversationFlow.Question.HelpFindFieldMeaning:
                    profile.StartHelp = false;
                    flow.LastQuestionAsked = ConversationFlow.Question.None;
                    await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                    turnContext.Activity.Text = "What is the meaning of " + turnContext.Activity.Text;
                    await ProcessGetFieldHelpAsync(turnContext, cancellationToken);
                    return;
                case ConversationFlow.Question.HelpGenerateFundCode:
                    await Dialog.RunAsync(turnContext,
                        ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                    return;
            }

            if (profile.StartHelp && turnContext.Activity.Text != "I want to know who owns a field" &&
                turnContext.Activity.Text != "I want to find a field" &&
                turnContext.Activity.Text != "I want to know the meaning of a field" &&
                turnContext.Activity.Text != "I want to generate a Fund Code")
            {
                profile.StartHelp = false;
                await dialogContext.EndDialogAsync(null, cancellationToken);
                await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }


            if (turnContext.Activity.Text.Equals("I want to know who owns a field",
                StringComparison.InvariantCultureIgnoreCase))
            {
                flow.LastQuestionAsked = ConversationFlow.Question.HelpGetFieldOwner;
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
                return;
            }

            if (turnContext.Activity.Text.Equals("I want to find a field", StringComparison.InvariantCultureIgnoreCase))
            {
                flow.LastQuestionAsked = ConversationFlow.Question.HelpFindField;
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
                return;
            }

            if (turnContext.Activity.Text.Equals("I want to know the meaning of a field",
                StringComparison.InvariantCultureIgnoreCase))
            {
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
                flow.LastQuestionAsked = ConversationFlow.Question.HelpFindFieldMeaning;
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
                return;
            }

            if (turnContext.Activity.Text.Equals("I want to generate a Fund Code",
                StringComparison.InvariantCultureIgnoreCase))
            {
                await dialogContext.EndDialogAsync(null, cancellationToken);
                await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await ProcessGenerateFundCodeAsync(turnContext, cancellationToken);
                return;
            }


            var dc = new DialogContext(new DialogSet(), turnContext, new DialogState());
            // Top intent tells us which cognitive service to use.
            var allScores =
                await _botServices.Dispatch.RecognizeAsync(dc, (Activity) turnContext.Activity, cancellationToken);

            var topIntent = allScores.Intents.First().Key;

            // Next, we call the dispatcher with the top intent.
            await DispatchToTopIntentAsync(turnContext, topIntent, cancellationToken);
        }

        private async Task PromptForFundCodeAsync(ConversationFlow flow, UserProfile profile,
            ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.None:
                    await turnContext.SendActivityAsync(
                        "First, please enter a Fund Code so I can take you to the " + profile.TabToNavigateTo +
                        " and show you where the field is:", null, null, cancellationToken);
                    flow.LastQuestionAsked = ConversationFlow.Question.Name;
                    break;
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
                if (member.Id != turnContext.Activity.Recipient.Id)
                    await turnContext.SendActivityAsync(
                        new Activity
                        {
                            Name = "GetUsername",
                            Value = "",
                            Type = ActivityTypes.Event
                        }, cancellationToken);
        }

        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent,
            CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "GenerateFundCode":
                    await ProcessGenerateFundCodeAsync(turnContext, cancellationToken);
                    break;
                case "FindField":
                    await ProcessFindFieldAsync(turnContext, cancellationToken);
                    break;
                case "AddToKnowledgeBase":
                    await ProcessAddToKnowledgeBaseAsync(turnContext, cancellationToken);
                    break;
                case "GetFieldHelp":
                    await ProcessGetFieldHelpAsync(turnContext, cancellationToken);
                    break;
                case "QnAMaker":
                    await ProcessQnAAsync(turnContext, cancellationToken);
                    break;
                default:
                    _logger.LogInformation($"Dispatch unrecognized intent: {intent}.");
                    await ProcessQnAAsync(turnContext, cancellationToken);
                    break;
            }
        }

        public async Task ProcessGenerateFundCodeAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessGenerateFundCodeAsync");

            await turnContext.SendActivityAsync(
                new Activity
                {
                    Name = "GetRbacAllowed",
                    Value = "FundCodeGenerator",
                    Type = ActivityTypes.Event
                }, cancellationToken);
        }

        public async Task ProcessGetFieldHelpAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            // Retrieve LUIS result for HomeAutomation.
            var recognizerResult =
                await _botServices.LuisGetFieldHelpRecognizer.RecognizeAsync(turnContext, cancellationToken);
            var result = recognizerResult.Properties["luisResult"] as LuisResult;

            await turnContext.SendActivityAsync("Okay. Let me get that for you.", null, null,
                cancellationToken);

            await turnContext.SendActivityAsync(
                new Activity
                {
                    Name = "GetFieldHelp",
                    Value = result?.Entities.First().Entity,
                    Type = ActivityTypes.Event
                }, cancellationToken);
        }

        public async Task ProcessAddToKnowledgeBaseAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessAddToKnowledgeBaseAsync");
            var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            var conversationStateAccessors =
                ConversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(),
                cancellationToken);

            var input = turnContext.Activity.Text.Trim();

            if (profile.WantsToAddToKnowledgeBase != null && flow.LastQuestionAsked == ConversationFlow.Question.None)
                flow.LastQuestionAsked = ConversationFlow.Question.Question;

            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.None:
                    await turnContext.SendActivityAsync("Sure. Please give me a question to add:", null, null,
                        cancellationToken);

                    flow.LastQuestionAsked = ConversationFlow.Question.Question;
                    break;
                case ConversationFlow.Question.Question:
                    if (profile.WantsToAddToKnowledgeBase == null)
                    {
                        await turnContext.SendActivityAsync("Okay. And now give me an answer:", null, null,
                            cancellationToken);
                        profile.Question = input;
                        profile.WantsToAddToKnowledgeBase = null;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(
                            "Okay. So what is the answer for '" + profile.Question + "'?", null, null,
                            cancellationToken);
                    }

                    flow.LastQuestionAsked = ConversationFlow.Question.Answer;
                    break;
                case ConversationFlow.Question.Answer:
                    profile.Answer = input;
                    await turnContext.SendActivityAsync("Thanks. Let me add that for you. This may take a minute.",
                        null, null,
                        cancellationToken);
                    await turnContext.SendActivityAsync(new Activity {Type = ActivityTypes.Typing}, cancellationToken);

                    flow.LastQuestionAsked = ConversationFlow.Question.None;

                    var urls = new List<string>
                    {
                        "https://docs.microsoft.com/azure/cognitive-services/QnAMaker/troubleshooting"
                    };

                    var endpoint = _configuration["QnAEndpointAuthorHostName"];
                    var endpointKey = _configuration["QnAEndpointAuthorKey"];
                    var kbId = _configuration["QnAKnowledgebaseId"];

                    var client =
                        new QnAMakerClient(new ApiKeyServiceClientCredentials(endpointKey))
                            {Endpoint = endpoint};

                    var updateOp = await client.Knowledgebase.UpdateAsync(kbId,
                        new UpdateKbOperationDTO
                        {
                            // Create JSON of changes
                            Add = new UpdateKbOperationDTOAdd
                            {
                                QnaList = new List<QnADTO>
                                {
                                    new QnADTO
                                    {
                                        Questions = new List<string>
                                        {
                                            profile.Question
                                        },
                                        Answer = profile.Answer,
                                        Metadata = new List<MetadataDTO>
                                        {
                                            new MetadataDTO {Name = "AddedByUser", Value = "true"}
                                        }
                                    }
                                },
                                Urls = urls
                            },
                            Update = null,
                            Delete = null
                        }, cancellationToken);


                    // Loop while operation is success
                    await MonitorOperation(client, updateOp);

                    await client.Knowledgebase.PublishAsync(kbId, cancellationToken);

                    await turnContext.SendActivityAsync("Okay. I now know '" + profile.Question + "'.", null, null,
                        cancellationToken);

                    profile.Question = null;
                    profile.Answer = null;
                    profile.WantsToAddToKnowledgeBase = null;

                    break;
            }

            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static async Task MonitorOperation(IQnAMakerClient client, Operation operation)
        {
            // Loop while operation is success
            for (var i = 0;
                i < 20 && (operation.OperationState == OperationStateType.NotStarted ||
                           operation.OperationState == OperationStateType.Running);
                i++)
            {
                Console.WriteLine("Waiting for operation: {0} to complete.", operation.OperationId);
                await Task.Delay(5000);
                operation = await client.Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
                throw new Exception($"Operation {operation.OperationId} failed to completed.");
        }

        public async Task ProcessFindFieldAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessFindFieldAsync");
            var userStateAccessors = UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            // Retrieve LUIS result for HomeAutomation.
            var recognizerResult =
                await _botServices.LuisFindFieldRecognizer.RecognizeAsync(turnContext, cancellationToken);
            var result = recognizerResult.Properties["luisResult"] as LuisResult;

            turnContext.Activity.Text = "Find Field: " + result?.Entities.First().Entity;
            var results = await _botServices.QnAMaker.GetAnswersAsync(turnContext);

            if (results.Any())
            {
                var firstResult = results.First().Answer;
                profile.FieldToNavigateTo = result?.Entities.First().Entity;
                profile.TabToNavigateTo = firstResult.Split(":")[0];
                profile.UrlLocator = firstResult.Split(":")[1];
                await UserState.SaveChangesAsync(turnContext, false, cancellationToken);

                await turnContext.SendActivityAsync(
                    MessageFactory.Text("The " + result?.Entities.First().Entity + " field is located on the " +
                                        profile.TabToNavigateTo + ". I will highlight the field in yellow for you."),
                    cancellationToken);

                await turnContext.SendActivityAsync(
                    new Activity
                    {
                        Name = "FindField", Value = result?.Entities.First().Entity + ":" + profile.UrlLocator,
                        Type = ActivityTypes.Event
                    }, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text(
                        "Sorry, I couldn't find an answer to that.\n\nIf you require further assistance please contact the Fund Master project team at: xxx"),
                    cancellationToken);
            }
        }


        public async Task ProcessQnAAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessQnAAsync");


            if (turnContext.Activity.Text.Contains("meaning of"))
            {
                await ProcessGetFieldHelpAsync(turnContext, cancellationToken);
                return;
            }

            var results = await _botServices.QnAMaker.GetAnswersAsync(turnContext, new QnAMakerOptions
            {
                ScoreThreshold = 0.7f,
                Top = 3,
                QnAId = 0,
                RankerType = "Default",
                IsTest = false
            });
            if (results.Any())
            {
                if (results.First().Questions[0].Contains("Find Field"))
                {
                    await ProcessFindFieldAsync(turnContext, cancellationToken);
                    return;
                }

                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
            }
            else
            {
                turnContext.Activity.Summary = "Unknown answer";
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)),
                    cancellationToken);
            }
        }
    }
}