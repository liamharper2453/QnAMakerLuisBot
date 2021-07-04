namespace FundMasterBot
{
    public class ConversationFlow
    {
        // Identifies the last question asked.
        public enum Question
        {
            Name,
            Age,
            Date,
            Question,
            Answer,
            None,
            HelpGetFieldOwner,
            HelpFindField,
            HelpFindFieldMeaning,
            HelpGenerateFundCode
        }

        // The last question asked.
        public Question LastQuestionAsked { get; set; } = Question.None;
    }
}