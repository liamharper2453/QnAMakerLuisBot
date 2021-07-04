


using System.IO;
using FundMasterBot.Bot;
using FundMasterBot.Dialog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FundMasterBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            OrchestratorConfig = configuration.GetSection("Orchestrator").Get<OrchestratorConfig>();
        }

        public OrchestratorConfig OrchestratorConfig { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            services.AddSingleton(InitializeOrchestrator());

            // Create the bot services (LUIS, QnA) as a singleton.
            services.AddSingleton<IBotServices, BotServices>();

                // The Dialog that will be run by the bot.
            services.AddSingleton<RootDialog>();
            // Create the bot as a transient.
            services.AddTransient<IBot, FundMasterBot<RootDialog>>();

            // Create the User state.
            services.AddSingleton<UserState>();

            // Create the Conversation state.
            services.AddSingleton<ConversationState>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }

        private OrchestratorRecognizer InitializeOrchestrator()
        {
            string modelFolder = Path.GetFullPath(OrchestratorConfig.ModelFolder);
            string snapshotFile = Path.GetFullPath(OrchestratorConfig.SnapshotFile);
            OrchestratorRecognizer orc = new OrchestratorRecognizer()
            {
                ModelFolder = modelFolder,
                SnapshotFile = snapshotFile
            };
            return orc;
        }
    }
}
