using Infrastructure.Contracts.AIGeneration;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Implementations
{
    public static class ClaudeServiceExtensions
    {
        /// <summary>
        /// Registers IClaudeService and binds ClaudeOptions from configuration.
        /// Expects an "Claude" section in appsettings.json (or env vars).
        ///
        /// Usage:
        ///   builder.Services.AddClaudeService(builder.Configuration);
        /// </summary>
        public static IServiceCollection AddClaudeService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<ClaudeOptions>()
                .Bind(configuration.GetSection(ClaudeOptions.Section))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddHttpClient<IClaudeService, ClaudeService>();

            return services;
        }

        /// <summary>
        /// Registers IClaudeService with inline option overrides.
        ///
        /// Usage:
        ///   builder.Services.AddClaudeService(o => {
        ///       o.ApiKey    = "sk-ant-...";
        ///       o.MaxTokens = 2048;
        ///   });
        /// </summary>
        public static IServiceCollection AddClaudeService(
            this IServiceCollection services,
            Action<ClaudeOptions> configure)
        {
            services
                .AddOptions<ClaudeOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddHttpClient<IClaudeService, ClaudeService>();

            return services;
        }
    }
}
