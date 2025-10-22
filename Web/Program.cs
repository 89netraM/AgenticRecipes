using System;
using System.Data.Common;
using AgenticRecipes.Web;
using AgenticRecipes.Web.Components;
using AgenticRecipes.Web.Workflows;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAPI.Web;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMermaidJS();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<MicrowaveTool>((client) => client.BaseAddress = new("http://microwave"));

builder
    .Services.AddTransient(sp => new SpotifyClient(
        new DbConnectionStringBuilder()
        {
            ConnectionString =
                sp.GetRequiredService<IConfiguration>().GetConnectionString("Spotify") ?? throw new ArgumentException(
                    "No Spotify token provided"
                ),
        }["Key"] as string
            ?? throw new ArgumentException("No Key in Spotify connection string")
    ))
    .AddTransient<MusicTool>();

builder
    .AddOpenAIClient("openai")
    .AddChatClient()
    .UseOpenTelemetry(configure: client => client.EnableSensitiveData = true);

builder.AddPlainWorkflow();

builder.AddRagWorkflow();

builder.AddToolWorkflow();

builder.AddKitchenWorkflow();

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
