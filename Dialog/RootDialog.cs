


using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Dialogs;

namespace FundMasterBot.Dialog
{
    /// <summary>
    /// This is an example root dialog. Replace this with your applications.
    /// </summary>
    public class RootDialog : ComponentDialog
    {
        /// <summary>
        /// QnA Maker initial dialog
        /// </summary>
        private const string InitialDialog = "initial-dialog";

        public readonly UserState UserState;
        public readonly ConversationState ConversationState;

        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;


        /// <summary>
        /// Initializes a new instance of the <see cref="RootDialog"/> class.
        /// </summary>
        /// <param name="services">Bot Services.</param>
        /// <param name="userState"></param>
        /// <param name="conversationState"></param>
        public RootDialog(IBotServices services, UserState userState, ConversationState conversationState)
        {
            UserState = userState;
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            ConversationState = conversationState;

            var dialogState = ConversationState.CreateProperty<DialogState>("dialogState");
            Dialogs = new DialogSet(dialogState);
            AddDialog(new QnAMakerBaseDialog(services));
            AddDialog(new FundCodeGeneratorDialog(userState, conversationState));
            AddDialog(new CannotFindAnswerDialog(userState));
            AddDialog(new ParentHelpDialog());
            AddDialog(new ChildHelpDialog(userState));


            AddDialog(new WaterfallDialog(InitialDialog)
               .AddStep(InitialStepAsync));

            // The initial child Dialog to run.
            InitialDialogId = InitialDialog;
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(),
                cancellationToken);

            if (stepContext.Context.Activity.Name == "ProcessGenerateFundCodeAsync")
            {
                return await stepContext.BeginDialogAsync(nameof(FundCodeGeneratorDialog), null, cancellationToken);

            }

            if (userProfile.OnStartup)
            {
                userProfile.OnStartup = false;
                await ConversationState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                await UserState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(ParentHelpDialog), null, cancellationToken);
            }


            if (userProfile.StartHelp)
            {
                await UserState.SaveChangesAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(ChildHelpDialog), null, cancellationToken);
            }

            if (stepContext.Context.Activity.Summary == "Unknown answer")
            {
                return await stepContext.BeginDialogAsync(nameof(CannotFindAnswerDialog), null, cancellationToken);

            }

           
            return await stepContext.BeginDialogAsync(nameof(QnAMakerDialog), null, cancellationToken);
        }

    }
}
