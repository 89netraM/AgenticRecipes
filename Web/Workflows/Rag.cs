using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgenticRecipes.Web.Workflows;

public static class Rag
{
    public static void AddRagWorkflow(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent(
            "rag",
            (sp, name) =>
                new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: name,
                    instructions: """
                    You are a cooking expert, the user will ask you questions about recipes, if a recipe can be found
                    in the database it will be provided to you, if no recipe can be found apologize to the user and
                    end the turn without giving any advice.
                    """
                )
        );
        builder.Services.AddKeyedSingleton(
            "rag",
            (sp, _) =>
            {
                var ragExecutor = new RagExecutor();
                var ragAgent = sp.GetRequiredKeyedService<AIAgent>("rag");
                return new WorkflowBuilder(ragExecutor).AddEdge(ragExecutor, ragAgent).WithOutputFrom(ragAgent).Build();
            }
        );
    }
}

public class RagExecutor()
    : ReflectingExecutor<RagExecutor>("RagExecutor"),
        IMessageHandler<ChatMessage[]>,
        IMessageHandler<TurnToken>,
        IResettableExecutor
{
    public ValueTask HandleAsync(
        ChatMessage[] message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (message.LastOrDefault()?.Text.Contains("ragu", StringComparison.InvariantCultureIgnoreCase) is true)
        {
            return context.SendMessageAsync(
                message.Append(new(ChatRole.System, RaguRecipe)).ToArray(),
                cancellationToken
            );
        }

        return context.SendMessageAsync(
            message.Append(new(ChatRole.System, "No recipe provided")).ToArray(),
            cancellationToken
        );
    }

    private const string RaguRecipe = """
        # Classic beef ragù

        This slow-cooked beef ragù is perfect for batch-cooking and because it's cooked in the oven it's low effort. Serve three tablespoons of ragù to every 100g/3½oz serving of fresh pasta.

        ## Ingredients

        - [ ] 4 tbsp olive oil
        - [ ] 1kg/2lb 4oz chuck steak, cut into 2cm/¾in pieces
        - [ ] 1 brown onion, finely chopped
        - [ ] 1 garlic clove, crushed
        - [ ] 4 sticks celery, finely chopped
        - [ ] 1 tbsp finely chopped fresh rosemary
        - [ ] 350ml/12fl oz Chianti red wine
        - [ ] 600g/1lb 5oz tomato passata

        ### To serve

        * pappadelle pasta (approximately 100g/3½oz fresh or 50g/1¾oz dry pasta per person)
        * freshly grated Parmesan cheese, to serve
        * salt and freshly ground black pepper

        ## Method

        1. Preheat the oven to 180C/160C Fan/Gas 4.
        2. In a large, heavy-based, ovenproof saucepan or casserole, heat the olive oil over a medium heat. Season the beef, add to the pan and brown on all sides. Remove the beef and set aside.
        3. Add the onion, garlic, celery and rosemary to the pan and cook until soft. Return the meat to the pan, add the red wine and bring to the boil. Stir in the tomato passata and cover the surface of the ragù with a circle of baking paper. Bake for 2½ hours or until the meat is very tender.
        4. Remove the baking paper and break up the meat using a fork. Season with salt and pepper and set aside.
        5. To serve, cook the pappardelle in salted boiling water according to the packet instructions, or until al dente.
        6. In a sauté pan, mix 3 tablespoons of the ragù per portion of pasta with a tablespoon of the pasta water. Cook on a low heat and toss together until the sauce clings to the pasta.
        7. Serve with grated Parmesan cheese and freshly ground black pepper.
        """;

    public ValueTask HandleAsync(
        TurnToken message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        return context.SendMessageAsync(message, cancellationToken);
    }
}
