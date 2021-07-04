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
    public class ChildHelpDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        public ChildHelpDialog(UserState userState)
            : base(nameof(ChildHelpDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectOptionAsync,
                PromptAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectOptionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choices = new List<string> { "I want to know who owns a field", "I want to find a field", "I want to know the meaning of a field", "I want to generate a Fund Code" };
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
                MessageFactory.Text("Here are some example questions that you can ask me."
                                    ),
                cancellationToken);

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("You can either click the buttons below or type your question directly. You do not need to enter exactly what is below. For example for 'I want to know who owns a field' you could instead ask 'Who owns the Fund Code field'. I should be able to answer your question even if you ask it a bit differently."),
                cancellationToken);

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("I also know various FAQ's regarding the FundMaster UI. For example: 'How do I access Fund Master'."),
                cancellationToken);

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("To see this information again you can type 'Help'."),
                cancellationToken);

            stepContext.Context.Activity.Summary = "SelectingHelpOption";

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

        private async Task<DialogTurnResult> PromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["helpChoice"] = ((FoundChoice)stepContext.Result).Value;

            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            userProfile.HelpChoice = stepContext.Values["helpChoice"].ToString();

            stepContext.Context.Activity.Summary = "EndingHelp";


            if (userProfile.HelpChoice == "I want to know who owns a field")
            {
                 await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Okay. Please enter a field name:") }, cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (userProfile.HelpChoice == "I want to find a field")
            {
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Okay. Please enter a field name:") }, cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (userProfile.HelpChoice == "I want to know the meaning of a field")
            {
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Okay. Please enter a field name:") }, cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (userProfile.HelpChoice == "I want to generate a Fund Code")
            {
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

        }
    }
}
