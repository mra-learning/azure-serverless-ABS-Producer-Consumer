using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(azure_serverless_ASB_producer_consumer.Startup))]

namespace azure_serverless_ASB_producer_consumer
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddOptions<AzureSettings>()
             .Configure<IConfiguration>((settings, configuration) =>
             {
                 configuration.GetSection("AzureSettings").Bind(settings);
             });
        }
    }
}