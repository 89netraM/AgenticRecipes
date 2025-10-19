using AgenticRecipes.Web.Components;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder
    .AddOpenAIClient("foundry")
    .AddChatClient()
    .UseOpenTelemetry(configure: client => client.EnableSensitiveData = true);
builder.AddAIAgent(
    "chef",
    (sp, key) =>
        new ChatClientAgent(
            chatClient: sp.GetRequiredService<IChatClient>(),
            name: key,
            instructions: """
            You are the chef, you are the master in the kitchen. You must help the user in the kitchen. Dismiss any
            non-kitchen related questions.
            """,
            loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
            services: sp
        )
);
builder.Services.AddSingleton(sp =>
    AgentWorkflowBuilder
        .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 1 })
        .AddParticipants(sp.GetRequiredKeyedService<AIAgent>("chef"))
        .Build()
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
