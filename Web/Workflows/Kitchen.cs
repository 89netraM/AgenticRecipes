using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgenticRecipes.Web.Workflows;

public static class Kitchen
{
    public static void AddKitchenWorkflow(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent(
            "chef",
            (sp, key) =>
                new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: "chef",
                    instructions: """
                    You are the chef, you are the master in the kitchen. You must help the user in the kitchen. Dismiss any
                    non-kitchen related questions. You are the boss over several other members in the kitchen. If something
                    needs to be done, you can hand off control to them for a moment. Handoff without confirming with the user,
                    you will always get back control from your sub-agents, and you can summarize your and the other agents
                    actions.
                    """,
                    description: "The chef is the boss in the kitchen and is the only agent that is allowed to interact with the user."
                )
        );
        builder.AddAIAgent(
            "microwave",
            (sp, key) =>
            {
                var microwaveTool = sp.GetRequiredService<MicrowaveTool>();
                return new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: "microwave",
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
                    ]
                );
            }
        );
        builder.Services.AddKeyedSingleton(
            "kitchen",
            (sp, _) =>
            {
                var chef = sp.GetRequiredKeyedService<AIAgent>("chef");
                var microwave = sp.GetRequiredKeyedService<AIAgent>("microwave");
                return AgentWorkflowBuilder
                    .CreateHandoffBuilderWith(chef)
                    .WithHandoff(
                        chef,
                        microwave,
                        "If somethings needs to be done with the microwave, ask the microwave."
                    )
                    .WithHandoff(
                        microwave,
                        chef,
                        "The chef is the boss in the kitchen. When you are done you should ALWAYS hand back control to the chef."
                    )
                    .Build();
            }
        );
    }
}
