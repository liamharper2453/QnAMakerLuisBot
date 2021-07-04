


using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.AI.QnA;

namespace FundMasterBot
{
    public interface IBotServices
    {
        LuisRecognizer LuisFindFieldRecognizer { get; }

         LuisRecognizer LuisAddToKnowledgeBaseRecognizer { get;  }
         LuisRecognizer LuisGetFieldHelpRecognizer { get; }
        OrchestratorRecognizer Dispatch { get; }
        
        QnAMaker QnAMaker { get; }
    }
}
