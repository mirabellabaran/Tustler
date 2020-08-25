using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerAWSLib.Utilities
{
    public static class TranscriptParser
    {
        // expecting an AWS TranscribeService transcript document with the following properties of interest
        private static readonly byte[] resultsProperty = Encoding.UTF8.GetBytes("results");
        private static readonly byte[] transcriptsProperty = Encoding.UTF8.GetBytes("transcripts");
        private static readonly byte[] transcriptProperty = Encoding.UTF8.GetBytes("transcript");

        public static async Task<string> ParseTranscriptData(ReadOnlyMemory<byte> transcriptData, NotificationsList notifications)
        {
            AWSResult<string> result = await ParseTranscriptDataAsync(transcriptData);

            if (result.IsError)
            {
                notifications.HandleError(result);
                return null;
            }
            else
            {
                return result.Result;
            }
        }

        private static async Task<AWSResult<string>> ParseTranscriptDataAsync(ReadOnlyMemory<byte> transcriptData)
        {
            var options = new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            return await Task.Run(() =>
            {
                try
                {
                    var result = ParseTranscriptData(transcriptData.Span, options);
                    return new AWSResult<string>(result, null);
                }
                catch (JsonException ex)
                {
                    return new AWSResult<string>(null, new AWSException("TranscriptParser", "Error parsing the transcript document", ex));
                }
            });
        }

        private static string ParseTranscriptData(ReadOnlySpan<byte> data, JsonReaderOptions options)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, options);

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.ValueTextEquals(resultsProperty))
                        {
                            reader.Read();
                            switch (reader.TokenType)
                            {
                                case JsonTokenType.StartObject:
                                    reader.Read();
                                    switch (reader.TokenType)
                                    {
                                        case JsonTokenType.PropertyName:
                                            if (reader.ValueTextEquals(transcriptsProperty))
                                            {
                                                reader.Read();
                                                switch (reader.TokenType)
                                                {
                                                    case JsonTokenType.StartArray:
                                                        reader.Read();
                                                        switch (reader.TokenType)
                                                        {
                                                            case JsonTokenType.StartObject:
                                                                reader.Read();
                                                                switch (reader.TokenType)
                                                                {
                                                                    case JsonTokenType.PropertyName:
                                                                        if (reader.ValueTextEquals(transcriptProperty))
                                                                        {
                                                                            reader.Read();
                                                                            if (reader.TokenType == JsonTokenType.String)
                                                                                return reader.GetString();
                                                                        }
                                                                        else
                                                                            throw new JsonException("Expected a 'transcript' property as first child of 'transcripts' array.");
                                                                        break;
                                                                    default:
                                                                        throw new JsonException("Expected a 'transcript' property as first child of 'transcripts' array.");

                                                                }
                                                                break;
                                                            default:
                                                                throw new JsonException("Expected a new object as first value of 'transcripts' array.");
                                                        }
                                                        break;
                                                    default:
                                                        throw new JsonException("Expected a 'transcripts' array.");
                                                }
                                            }
                                            else
                                                throw new JsonException("Expected a 'transcripts' property as first child of 'results'.");
                                            break;
                                        default:
                                            throw new JsonException("Expected a 'transcripts' property as first child of 'results'.");
                                    }
                                    break;
                                default:
                                    throw new JsonException("Expected a 'results' object.");
                            }
                        }
                        break;
                }
            }

            return null;
        }
    }
}
