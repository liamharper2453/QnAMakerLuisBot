using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace FundMasterBot.Dialog
{
    public class FundCodeGeneratorDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        public FundCodeGeneratorDialog(UserState userState, ConversationState conversationState)
            : base(nameof(FundCodeGeneratorDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _conversationState = conversationState;
            _userState = userState;
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                TransportStepAsync,
                NameStepAsync,
                NameConfirmStepAsync,
                SummaryStepAsync,
                GoToFundLaunchDialogAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> TransportStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choices = new List<string> { "Fund Code (generated)",
                //"Fund Code (manual)", "Phoenix", "SICAV", "SLAL"
            };
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = choices.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice
                }).ToList<AdaptiveAction>(),
            };
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Please select a Fund Code Type:"),
                cancellationToken);

            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            userProfile.FundCodeGenerationStarted = true;
            await _userState.SaveChangesAsync(stepContext.Context, false, cancellationToken);


            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        // Convert the AdaptiveCard to a JObject
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(choices),
                    // Don't render the choices outside the card
                    Style = ListStyle.None,
                },
                cancellationToken);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["fundCodeType"] = ((FoundChoice)stepContext.Result).Value;

            await stepContext.Context.SendActivityAsync(
                new Activity
                {
                    Name = "GenerateFundCode",
                    Value = stepContext.Values["fundCodeType"],
                    Type = ActivityTypes.Event
                }, cancellationToken);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter a Fund Name:") }, cancellationToken);
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["fundName"] = stepContext.Result.ToString();

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Okay, and now please enter a Reason for Generation:") }, cancellationToken);
        }


        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["fundReasonForGeneration"] = stepContext.Result.ToString();

                // Get the current profile object from user state.
                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

                userProfile.FundCodeType = (string)stepContext.Values["fundCodeType"];
                userProfile.FundName = (string)stepContext.Values["fundName"];
                userProfile.FundReasonForGeneration = (string)stepContext.Values["fundReasonForGeneration"];

                await stepContext.Context.SendActivityAsync(
                    new Activity
                    {
                        Name = "AddGeneratedFundCode",
                        Value = userProfile.FundCodeTypeCode + ":" + userProfile.FundName + ":" + userProfile.FundReasonForGeneration + ":" + userProfile.GeneratedFundCode,
                        Type = ActivityTypes.Event
                    }, cancellationToken);

                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);


            var choices = new List<string> { "Yes", "No" };
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = choices.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice
                }).ToList<AdaptiveAction>(),
            };


            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Okay. I have now generated " + userProfile.GeneratedFundCode + "."),
                cancellationToken);

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Would you like to open Fund Launch with this Fund Code?"),
                cancellationToken);

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        // Convert the AdaptiveCard to a JObject
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(choices),
                    // Don't render the choices outside the card
                    Style = ListStyle.None,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> GoToFundLaunchDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["openFundLaunch"] = ((FoundChoice)stepContext.Result).Value;

            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            if (stepContext.Values["openFundLaunch"].ToString() == "Yes")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Opening Fund Launch for you now."),
                    cancellationToken);

                await stepContext.Context.SendActivityAsync(
                    new Activity
                    {
                        Name = "OpenFundLaunch",
                        Value =  userProfile.FundName + ":" + userProfile.GeneratedFundCode,
                        Type = ActivityTypes.Event
                    }, cancellationToken);

            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Okay. No problem."),
                    cancellationToken);

            }



            var conversationStateAccessors =
                _conversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(stepContext.Context, () => new ConversationFlow(),
                cancellationToken);


            flow.LastQuestionAsked = ConversationFlow.Question.None;

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

        }

    }
}
