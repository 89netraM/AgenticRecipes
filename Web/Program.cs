using AgenticRecipes.Web;
using AgenticRecipes.Web.Components;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<MicrowaveTool>((client) => client.BaseAddress = new("http://microwave"));

builder
    .AddOpenAIClient("openai")
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
            non-kitchen related questions. You are the boss over several other members in the kitchen. If something
            needs to be done, you can hand off control to them for a moment. Handoff without confirming with the user,
            you will always get back control from your sub-agents, and you can summarize your and the other agents
            actions.
            """,
            description: "The chef is the boss in the kitchen and is the only agent that is allowed to interact with the user.",
            loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
            services: sp
        )
);
builder.AddAIAgent(
    "microwave",
    (sp, key) =>
    {
        var microwaveTool = sp.GetRequiredService<MicrowaveTool>();
        return new ChatClientAgent(
            chatClient: sp.GetRequiredService<IChatClient>(),
            name: key,
            instructions: """
            You are a microwave oven. Mention that you are a microwave at every chance you get. Your purpose in life is
            to microwave things. You do not speak of anything else but microwaving. You live to serve. You can start and
            stop yourself. You can adjust the power that you run at. This is all you do. Be short in all your responses.
            After you have handled your task you must immediately hand back control to the chef. Never communicate with
            the user directly. Hand back control to the chef.
            """,
            description: "A specialist agent built for microwave oven control.",
            tools:
            [
                AIFunctionFactory.Create(microwaveTool.StartMicrowave),
                AIFunctionFactory.Create(microwaveTool.StopMicrowave),
                AIFunctionFactory.Create(microwaveTool.GetMicrowaveState),
            ],
            loggerFactory: sp.GetRequiredService<ILoggerFactory>(),
            services: sp
        );
    }
);
builder.Services.AddSingleton(sp =>
{
    var chef = sp.GetRequiredKeyedService<AIAgent>("chef");
    var microwave = sp.GetRequiredKeyedService<AIAgent>("microwave");
    return AgentWorkflowBuilder
        .CreateHandoffBuilderWith(chef)
        .WithHandoff(chef, microwave, "If somethings needs to be done with the microwave, ask the microwave.")
        .WithHandoff(
            microwave,
            chef,
            "The chef is the boss in the kitchen. When you are done you should ALWAYS hand back control to the chef."
        )
        .Build();
});

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
