using System;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgenticRecipes.Web.Workflows;

public static class AgenticRag
{
    public static void AddAgenticRag(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent(
            "recipe-researcher",
            (sp, name) =>
                new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: name,
                    instructions: """
                    You are a recipe expert. You call the search recipe tool to get base information about what the user
                    wants to know. Output the relevant parts of found recipes unedited.
                    """,
                    tools: [AIFunctionFactory.Create(SearchRecipe)]
                )
        );
        builder.AddAIAgent(
            "recipe-writer",
            (sp, name) =>
                new ChatClientAgent(
                    chatClient: sp.GetRequiredService<IChatClient>(),
                    name: name,
                    instructions: """
                    You are a cooking expert. You will take the users query, and any existing recipes, and write new
                    relevant recipes for the user. Keep recipes short and to the point. If you don't receive a full
                    recipe as input tell the user you could not complete their query, apologize. Do not guess anything,
                    don't tell the user anything you didn't find in the recipe.
                    """
                )
        );
        builder.Services.AddKeyedSingleton(
            "agentic-rag",
            (sp, _) =>
            {
                var recipeResearcher = sp.GetRequiredKeyedService<AIAgent>("recipe-researcher");
                var recipeWriter = sp.GetRequiredKeyedService<AIAgent>("recipe-writer");
                return AgentWorkflowBuilder.BuildSequential(recipeResearcher, recipeWriter);
            }
        );
    }

    [Description(
        "Search for recipes by title. Either returns the full text of a recipe, or a status code indicating that no recipe was found."
    )]
    private static string SearchRecipe(string recipeTitle)
    {
        if (recipeTitle.Contains("pancake", StringComparison.InvariantCultureIgnoreCase) is true)
        {
            return PancakeRecipe;
        }

        return "NO_RECIPE_FOUND";
    }

    private const string PancakeRecipe = """
        # Pancakes

        What's the secret to making the perfect pancakes? Trust Delia Smith to show you and enjoy Pancake Day without any flops. Delia's method uses milk and water for really thin, lacy edges. With added melted butter for flavour, these thin crêpe-style pancakes taste best with drizzled with lemon juice and sprinkled with crunchy, granulated sugar, but if you want to take things up a notch try our pancake topping ideas.

        If you're looking for fluffy American-style pancakes, try our classic American pancake recipe.

        Each pancake (without topping) provides 88kcal, 7.5g carbohydrates (of which 0.9g sugars), 5g fat (of which 2g saturates), 0.4g fibre and 0.2g salt.

        ## Ingredients

        ### For the pancake mixture

        - [ ] 110g/4oz plain flour, sifted
        - [ ] pinch of salt
        - [ ] 2 eggs
        - [ ] 200ml/7fl oz milk mixed with 75ml/3fl oz water
        - [ ] 50g/2oz butter

        ### To serve

        - [ ] caster sugar
        - [ ] lemon juice
        - [ ] lemon wedges

        ## Method

        1. Sift the flour and salt into a large mixing bowl with a sieve held high above the bowl so the flour gets an airing.
        2. Now make a well in the centre of the flour and break the eggs into it. Then begin whisking the eggs - any sort of whisk or even a fork will do - incorporating any bits of flour from around the edge of the bowl as you do so.
        3. Next gradually add small quantities of the milk and water mixture, still whisking (don't worry about any lumps as they will eventually disappear as you whisk).
        4. When all the liquid has been added, use a rubber spatula to scrape any elusive bits of flour from around the edge into the centre, then whisk once more until the batter is smooth, with the consistency of thin cream.
        5. Now melt the 50g/2oz of butter in a small saucepan or a bowl in the microwave.
        6. Spoon two tablespoons of the butter into the batter and whisk it in, then pour the rest into a bowl and use it to lubricate the pan, using a wodge of kitchen paper to smear it round before you make each pancake.
        7. Now get the pan really hot, then turn the heat down to medium and, to start with, do a test pancake to see if you're using the correct amount of batter. I find two tablespoons is about right for an 18cm/7in pan. It's also helpful if you spoon the batter into a ladle so it can be poured into the hot pan in one go.
        8. As soon as the batter hits the hot pan, tip it around from side to side to get the base evenly coated with batter. It should take only half a minute or so to cook; you can lift the edge with a palette knife to see if it's tinged gold as it should be.
        9. Flip the pancake over using a turner or palette knife - the other side will need a few seconds only - then simply slide it out of the pan onto a plate.
        10. Stack the pancakes as you make them between sheets of greaseproof paper (or non-stick baking paper) on a plate sat over simmering water. This will keep them warm while you make the rest.
        11. To serve, sprinkle each pancake with freshly squeezed lemon juice and caster sugar, fold in half, then in half again to form triangles, or else simply roll them up. Serve sprinkled with a little more sugar and lemon juice and extra sections of lemon.
        """;
}
