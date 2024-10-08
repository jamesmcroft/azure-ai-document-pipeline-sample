using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JsonSchemaMapper;
using OpenAI.Chat;

namespace AIDocumentPipeline.Shared.Serialization.AzureOpenAI;

public static class AzureOpenAISerializationExtensions
{
    private static readonly JsonSerializerOptions s_outputSerializerOptions = CreateStructuredOutputsSerializerOptions();

    private static Func<JsonSchemaGenerationContext, JsonNode, JsonNode> s_structuredOutputsTransform = (_, node) =>
    {
        if (node is JsonObject rootObject)
        {
            ProcessJsonObject(rootObject);
        }

        return node;

        static void ProcessJsonObject(JsonObject jsonObject)
        {
            if (jsonObject["type"]?.ToString().Contains("object") != true)
            {
                return;
            }

            // Ensures that object types include the "additionalProperties" field, set to false.
            if (!jsonObject.ContainsKey("additionalProperties"))
            {
                jsonObject.Add("additionalProperties", false);
            }

            var required = new JsonArray();
            var properties = jsonObject["properties"] as JsonObject;
            foreach (var property in properties!)
            {
                required.Add(property.Key);

                if (property.Value is JsonObject nestedObject)
                {
                    // Process nested objects to ensure schema validity.
                    ProcessJsonObject(nestedObject);
                }
            }

            // Ensures that object types include the "required" field containing all the property keys.
            if (!jsonObject.ContainsKey("required"))
            {
                jsonObject.Add("required", required);
            }
        }
    };


    private static readonly JsonSchemaMapperConfiguration s_outputSchemaMapperConfig = new()
    {
        IncludeSchemaVersion = false,
        IncludeTypeInEnums = true,
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = s_structuredOutputsTransform
    };

    public static async Task<ChatCompletion<T?>> CompleteChatAsync<T>(
        this ChatClient chatClient,
        List<ChatMessage> messages,
        ChatCompletionOptions? options = default,
        CancellationToken cancellationToken = default)
    {
        var result = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        return result.Value;
    }

    public static ChatResponseFormat CreateJsonSchemaFormat(this Type outputType, string jsonSchemaFormatName, string? jsonSchemaFormatDescription = null, bool? jsonSchemaIsStrict = null)
    {
        var jsonSchema = s_outputSerializerOptions.GetJsonSchema(outputType, s_outputSchemaMapperConfig);
        if (jsonSchema is not JsonObject jObj)
        {
            jObj = jsonSchema.GetValue<bool>()
                ? new JsonObject()
                : new JsonObject { ["not"] = true };
        }

        return ChatResponseFormat.CreateJsonSchemaFormat(jsonSchemaFormatName, BinaryData.FromString(jObj.ToJsonString(s_outputSerializerOptions)), jsonSchemaFormatDescription, jsonSchemaIsStrict);
    }

    private static JsonSerializerOptions CreateStructuredOutputsSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            Converters = { new JsonStringEnumConverter() },
        };
        options.MakeReadOnly();
        return options;
    }
}
