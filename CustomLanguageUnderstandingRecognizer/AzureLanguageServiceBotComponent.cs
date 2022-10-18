using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AzureLanguageServiceRecognizers.LanguageUnderstanding;

namespace AzureLanguageServiceRecognizers
{
    public class AzureLanguageServiceBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<DeclarativeType>((sp) => new DeclarativeType<CluAdaptiveRecognizer>(CluAdaptiveRecognizer.Kind));
        }
    }
}
