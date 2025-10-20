using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgenticRecipes.Web;

public sealed class MicrowaveTool(HttpClient httpClient)
{
    [Description(
        "Sets the microwave to run for `seconds` seconds (>= 0) at `power`% (0 - 100). Cannot be used if the microwave is already running."
    )]
    public async Task<string> StartMicrowave([Range(0, int.MaxValue)] int seconds, [Range(0, 100)] int power)
    {
        var response = await httpClient.PutAsJsonAsync(
            "state",
            new(Minutes: seconds / 60, Seconds: seconds % 60, power, IsRunning: true),
            MicrowaveJsonSerializerContext.Default.MicrowaveEditableState
        );
        if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            return "Cannot start microwave when already running. Stop first.";
        }
        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
        {
            var errorProperties = await response.Content.ReadFromJsonAsync(
                MicrowaveJsonSerializerContext.Default.StringArray
            );
            var errorArguments = errorProperties?.Select(p => p is "minutes" ? "seconds" : p).ToHashSet() ?? [];
            return $"Something is wrong with the input: {string.Join(", ", errorArguments)}.";
        }
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            return $"Unexpected error: {response.StatusCode}.";
        }
        var state = await response.Content.ReadFromJsonAsync(MicrowaveJsonSerializerContext.Default.MicrowaveState);
        if (state is null)
        {
            return "The microwave started but could not report it's state.";
        }
        return $"The microwave will run for {state.Minutes * 60 + state.Seconds} seconds at {state.Power}% power.";
    }

    [Description("Immediately stops any running microwave operations.")]
    public async Task<string> StopMicrowave()
    {
        var response = await httpClient.PutAsJsonAsync(
            "state",
            new(IsRunning: false),
            MicrowaveJsonSerializerContext.Default.MicrowaveEditableState
        );
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            return $"Unexpected error: {response.StatusCode}.";
        }
        var state = await response.Content.ReadFromJsonAsync(MicrowaveJsonSerializerContext.Default.MicrowaveState);
        if (state is null)
        {
            return "The microwave has been stopped but it's state could not be reported.";
        }
        return "The microwave is stopped.";
    }

    [Description("Gets whether the microwave is on and how many seconds remain, or if it's off.")]
    public async Task<string> GetMicrowaveState()
    {
        var state = await httpClient.GetFromJsonAsync("state", MicrowaveJsonSerializerContext.Default.MicrowaveState);
        if (state is null)
        {
            return "Could not read the state of the microwave.";
        }
        if (state.IsRunning)
        {
            return $"The microwave is running at {state.Power}% power, {state.RemainingTimeInSeconds} seconds remaining.";
        }
        return "The microwave is off.";
    }
}

[JsonSerializable(typeof(MicrowaveState))]
[JsonSerializable(typeof(MicrowaveEditableState))]
[JsonSerializable(typeof(string[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class MicrowaveJsonSerializerContext : JsonSerializerContext;

public sealed record MicrowaveState(int Minutes, int Seconds, int Power, bool IsRunning, int RemainingTimeInSeconds);

public sealed record MicrowaveEditableState(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Minutes = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Seconds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Power = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsRunning = null
);
