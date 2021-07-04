


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
    public class CannotFindAnswerDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public CannotFindAnswerDialog(UserState userState)
            : base(nameof(CannotFindAnswerDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                WantsToAddToKnowledgeBaseStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> WantsToAddToKnowledgeBaseStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(),
                cancellationToken);

            userProfile.Question = stepContext.Context.Activity.Text;
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the user's response is received.

            var choices = new List<string> {"Optional: Tell me the answer"};
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
                MessageFactory.Text("Sorry, I couldn't find an answer to that.\n\nIf you require further assistance please contact the Fund Master project team at: xxx"),
                cancellationToken);


             await stepContext.PromptAsync(
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


            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}