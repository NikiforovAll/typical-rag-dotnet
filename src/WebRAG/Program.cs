using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Service.AspNetCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WebRAG;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddLogging(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "hh:mm:ss";
    })
);

builder.AddNpgsqlDataSource("rag-db");

builder.ConfigureAiOptions();
var aiOptions = builder.GetAiOptions();

builder
    .Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        deploymentName: aiOptions.Deployment,
        endpoint: aiOptions.Endpoint,
        apiKey: aiOptions.ApiKey
    );

builder.Services.AddKernelMemory<MemoryServerless>(memoryBuilder =>
{
    memoryBuilder
        .WithPostgresMemoryDb(
            new PostgresConfig()
            {
                ConnectionString = builder.Configuration.GetConnectionString("rag-db")!
            }
        )
        .WithAzureOpenAITextGeneration(
            new AzureOpenAIConfig
            {
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = aiOptions.ApiKey,
                APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
                Deployment = aiOptions.Deployment,
                Endpoint = aiOptions.Endpoint,
            }
        )
        .WithAzureOpenAITextEmbeddingGeneration(
            new AzureOpenAIConfig
            {
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = aiOptions.ApiKey,
                APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
                Deployment = "text-embedding-ada-002",
                Endpoint = aiOptions.Endpoint,
            }
        );
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.AddKernelMemoryEndpoints(apiPrefix: "/rag");

app.MapGet(
    "/rag/my-query",
    async ([FromQuery] string q, Kernel kernel) =>
    {
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings =
            new() { Temperature = 0.0, MaxTokens = 100, };

        var result = await chatCompletionService.GetChatMessageContentAsync(
            q,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel
        );

        return new { result };
    }
);

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
