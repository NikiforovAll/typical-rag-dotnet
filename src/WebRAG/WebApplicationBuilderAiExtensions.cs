using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace WebRAG;

public static class WebApplicationBuilderAiExtensions
{
    public static AiOptions GetAiOptions(this WebApplicationBuilder builder)
    {
        using var sp = builder.Services.BuildServiceProvider();

        return sp.GetRequiredService<IOptions<AiOptions>>().Value;
    }

    public static void ConfigureAiOptions(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddOptions<AiOptions>()
            .Configure(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("openai")!;

                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (!part.Contains('='))
                    {
                        continue;
                    }

                    var keyValue = part.Split('=');
                    var key = keyValue[0];
                    var value = keyValue[1];

                    switch (key)
                    {
                        case "Endpoint":
                            options.Endpoint = value;
                            break;
                        case "Key":
                            options.ApiKey = value;
                            break;
                        case "Deployment":
                            options.Deployment = value;
                            break;
                        default:
                            // Handle unrecognized key or value
                            break;
                    }
                }
            })
            .ValidateOnStart()
            .ValidateDataAnnotations();
    }
}

public class AiOptions
{
    [Required]
    [Url]
    public string Endpoint { get; set; } = default!;

    [Required]
    public string ApiKey { get; set; } = default!;

    [Required]
    public string Deployment { get; set; } = default!;
}
