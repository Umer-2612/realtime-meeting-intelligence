using PsiBot.Service.Settings;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Security.Authentication;

namespace PsiBot.Services
{
    /// <summary>
    /// ASP.NET Core entry point responsible for building and running the bot host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry point that boots the web host.
        /// </summary>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Configures the web host builder with Kestrel and custom TLS settings.
        /// </summary>
        /// <param name="args">Command line arguments supplied to the process.</param>
        /// <returns>Configured web host builder.</returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel((ctx, opt) =>
                {
                    var config = new BotConfiguration();
                    ctx.Configuration.GetSection(nameof(BotConfiguration)).Bind(config);
                    config.Initialize();
                    opt.Configure()
                        .Endpoint("HTTPS", listenOptions =>
                        {
                            listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
                        });
                    opt.ListenAnyIP(config.CallSignalingPort, o => o.UseHttps());
                    opt.ListenAnyIP(config.CallSignalingPort + 1);
                });
    }
}
