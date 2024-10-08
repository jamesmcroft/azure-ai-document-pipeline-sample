using System.Text.Json;
using OpenAI.Chat;

namespace AIDocumentPipeline.Shared.Serialization.AzureOpenAI;

public class ChatCompletion<T>
{
    public string Id { get; private set; }

    public string Model { get; private set; }

    public string SystemFingerprint { get; private set; }

    public ChatTokenUsage Usage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ChatFinishReason FinishReason { get; private set; }

    public IReadOnlyList<ChatTokenLogProbabilityDetails> ContentTokenLogProbabilities { get; private set; }

    public IReadOnlyList<ChatTokenLogProbabilityDetails> RefusalTokenLogProbabilities { get; private set; }

    public ChatMessageRole Role { get; private set; }

    public ChatMessageContent Content { get; private set; }

    public IReadOnlyList<ChatToolCall> ToolCalls { get; private set; }

    public T? Parsed { get; private set; }

    public string Refusal { get; private set; }

    public static implicit operator ChatCompletion<T>(ChatCompletion completion)
    {
        return new ChatCompletion<T>
        {
            Id = completion.Id,
            Model = completion.Model,
            SystemFingerprint = completion.SystemFingerprint,
            Usage = completion.Usage,
            CreatedAt = completion.CreatedAt,
            FinishReason = completion.FinishReason,
            ContentTokenLogProbabilities = completion.ContentTokenLogProbabilities,
            RefusalTokenLogProbabilities = completion.RefusalTokenLogProbabilities,
            Role = completion.Role,
            Content = completion.Content,
            ToolCalls = completion.ToolCalls,
            Parsed = JsonSerializer.Deserialize<T?>(completion.Content[0].Text),
            Refusal = completion.Refusal
        };
    }
}
