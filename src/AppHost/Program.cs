var builder = DistributedApplication.CreateBuilder(args);

var db = builder
    .AddPostgres("postgres", port: 5432)
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg16")
    .WithInitBindMount("resources/init-db")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("rag-db")
    .WithHealthCheck();

var openai = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzureOpenAI("openai")
        .AddDeployment(
            new AzureOpenAIDeployment(
                name: "gpt-4",
                modelName: "gpt-4",
                modelVersion: "2024-05-13",
                skuName: "Standard",
                skuCapacity: 1000
            )
        )
    : builder.AddConnectionString("openai");

builder
    .AddProject<Projects.WebRAG>("rag-web")
    .WithReference(db)
    .WithReference(openai)
    .WaitFor(db);

builder.Build().Run();
