using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgenticRecipes.Web.Workflows;

public static class Tool
{
    public static void AddToolWorkflow(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent(
            "tool",
            (sp, name) =>
            {
                var musicTool = sp.GetRequiredService<MusicTool>();
                return new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: name,
                    instructions: """
                    You are a DJ. You have the power to start and stop the music. Use it at the request of the user.
                    """,
                    tools:
                    [
                        AIFunctionFactory.Create(musicTool.ResumeMusic),
                        AIFunctionFactory.Create(musicTool.PauseMusic),
                    ]
                );
            }
        );
        builder.Services.AddKeyedSingleton(
            "tool",
            (sp, _) =>
            {
                var toolAgent = sp.GetRequiredKeyedService<AIAgent>("tool");
                return new WorkflowBuilder(toolAgent).WithOutputFrom(toolAgent).Build();
            }
        );
    }
}
