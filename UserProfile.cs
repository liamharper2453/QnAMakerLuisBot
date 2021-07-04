namespace FundMasterBot
{
    public class UserProfile
    {
        public string Name { get; set; }
        public string FieldToNavigateTo { get; set; }
        public string TabToNavigateTo { get; set; }

        public string UrlLocator { get; set; }

        public string Question { get; set; }
        public string Answer { get; set; }
        public string WantsToAddToKnowledgeBase { get; set; }

        public string FundName { get; set; }
        public string FundReasonForGeneration { get; set; }

        public string FundCodeType { get; set; }

        public string GeneratedFundCode { get; set; }

        public string FundCodeTypeCode { get; set; }

        public bool FundCodeGenerationStarted { get; set; }

        public bool OnStartup { get; set; }

        public bool StartHelp { get; set; }

        public string HelpChoice { get; set; }

        public string HelpChoiceValue { get; set; }
    }
}