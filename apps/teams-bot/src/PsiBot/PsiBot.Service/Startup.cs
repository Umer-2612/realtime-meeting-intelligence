using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using PsiBot.Service.Settings;
using PsiBot.Services.Bot;
using PsiBot.Services.Logging;

namespace PsiBot.Services
{
    /// <summary>
    /// Configures dependency injection and the HTTP request pipeline for the bot service.
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Registers application services and bot dependencies.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("PsiBot", redirectToTrace: true));
            services.AddSingleton<InMemoryObserver, InMemoryObserver>();
            services.Configure<BotConfiguration>(Configuration.GetSection(nameof(BotConfiguration)));
            services.PostConfigure<BotConfiguration>(config => config.Initialize());
            services.AddSingleton<ITeamsCallLifecycleService, TeamsCallLifecycleService>(provider =>
            {
                var bot = new TeamsCallLifecycleService(
                    provider.GetRequiredService<IGraphLogger>(),
                    provider.GetRequiredService<IOptions<BotConfiguration>>());
                bot.Initialize();
                return bot;
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
