using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgenticRecipes.Web.Workflows;

public static class Plain
{
    public static void AddPlainWorkflow(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent(
            "plain",
            (sp, name) => new ChatClientAgent(chatClient: sp.GetRequiredService<IChatClient>(), name: name)
        );
        builder.Services.AddKeyedSingleton(
            "plain",
            (sp, _) =>
            {
                var plainAgent = sp.GetRequiredKeyedService<AIAgent>("plain");
                return new WorkflowBuilder(plainAgent).WithOutputFrom(plainAgent).Build();
            }
        );
    }
}
