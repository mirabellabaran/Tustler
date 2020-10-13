using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerAWSLib.Utilities
{
    public class WordTiming
    {
        public enum WordType
        {
            Pronunciation,
            Punctuation
        }

        public enum CacheKey
        {
            StartTimeKey = 0,
            ContentKey = 1,
            WordTypeKey = 2
        }

        private static readonly Dictionary<CacheKey, string> cache = new Dictionary<CacheKey, string>(3);

        private WordTiming(double startTime, string content, WordType wordType)
        {
            StartTime = startTime;
            Content = content;
            Type = wordType;
        }

        public double StartTime { get; }
        public string Content { get; }
        public WordType Type { get; }

        public static void Push(CacheKey key, string value)
        {
            cache.Add(key, value);
        }

        private static double GetStartTime(WordType wordType)
        {
            double startTime = 0.0;

            if (wordType == WordType.Pronunciation)
            {
                if (!Double.TryParse(cache[CacheKey.StartTimeKey], out startTime))
                {
                    throw new ArgumentException($"Unexpected start time value {cache[CacheKey.StartTimeKey]} on JSON items array object");
                }
            }

            return startTime;
        }

        public static WordTiming AddTiming()
        {
            var content = cache[CacheKey.ContentKey];
            var wordType = cache[CacheKey.WordTypeKey] switch
            {
                "pronunciation" => WordType.Pronunciation,
                "punctuation" => WordType.Punctuation,
                _ => throw new ArgumentException($"Unexpected type value {cache[CacheKey.WordTypeKey]} on JSON items array object")
            };
            var startTime = GetStartTime(wordType);

            cache.Clear();

            return new WordTiming(startTime, content, wordType);
        }
    }

    public static class TranscriptParser
    {
        // expecting an AWS TranscribeService transcript document with the following properties of interest
        private static readonly byte[] resultsProperty = Encoding.UTF8.GetBytes("results");
        private static readonly byte[] transcriptsProperty = Encoding.UTF8.GetBytes("transcripts");
        private static readonly byte[] transcriptProperty = Encoding.UTF8.GetBytes("transcript");
        private static readonly byte[] itemsProperty = Encoding.UTF8.GetBytes("items");
        private static readonly byte[] startTimeProperty = Encoding.UTF8.GetBytes("start_time");
        private static readonly byte[] alternativesProperty = Encoding.UTF8.GetBytes("alternatives");
        private static readonly byte[] contentProperty = Encoding.UTF8.GetBytes("content");
        private static readonly byte[] typeProperty = Encoding.UTF8.GetBytes("type");

        public static async Task<string> ParseTranscriptData(byte[] transcriptData, NotificationsList notifications)
        {
            AWSResult<string> result = await ParseTranscriptDataAsync(new ReadOnlyMemory<byte>(transcriptData));

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

        public static async Task<IEnumerable<WordTiming>> ParseWordTimingData(byte[] transcriptData, NotificationsList notifications)
        {
            AWSResult<IEnumerable<WordTiming>> result = await ParseWordTimingDataAsync(new ReadOnlyMemory<byte>(transcriptData));

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

        private static async Task<AWSResult<IEnumerable<WordTiming>>> ParseWordTimingDataAsync(ReadOnlyMemory<byte> transcriptData)
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
                    var result = ParseWordTimings(transcriptData.Span, options);
                    return new AWSResult<IEnumerable<WordTiming>>(result, null);
                }
                catch (JsonException ex)
                {
                    return new AWSResult<IEnumerable<WordTiming>>(null, new AWSException("TranscriptParser", "Error parsing the transcript document", ex));
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

        private static IEnumerable<WordTiming> ParseWordTimings(ReadOnlySpan<byte> data, JsonReaderOptions options)
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
                                                if (reader.TrySkip())
                                                {
                                                    reader.Read();  // read over EndArray token
                                                    if (reader.ValueTextEquals(itemsProperty))
                                                    {
                                                        reader.Read();
                                                        return reader.TokenType switch
                                                        {
                                                            JsonTokenType.StartArray => ExtractWordTimingData(reader),
                                                            _ => throw new JsonException("Expected an 'items' array."),
                                                        };
                                                    }
                                                    else
                                                        throw new JsonException("Expected an 'items' property as second child of 'results'.");
                                                }
                                            }
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

        private static IEnumerable<WordTiming> ExtractWordTimingData(Utf8JsonReader reader)
        {
            var results = new List<WordTiming>();

            var completed = false;
            while (!completed && reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        var moveToNext = false;
                        while (!moveToNext && reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                case JsonTokenType.PropertyName:
                                    if (reader.ValueTextEquals(startTimeProperty))
                                    {
                                        reader.Read();
                                        if (reader.TokenType == JsonTokenType.String)
                                            WordTiming.Push(WordTiming.CacheKey.StartTimeKey, reader.GetString());
                                    }
                                    else if (reader.ValueTextEquals(alternativesProperty))
                                    {
                                        reader.Read();
                                        switch (reader.TokenType)
                                        {
                                            case JsonTokenType.StartArray:
                                                reader.Read();
                                                if (reader.TokenType == JsonTokenType.StartObject)
                                                {
                                                    reader.Read();
                                                    if (reader.TrySkip())   // skip the confidence property
                                                    {
                                                        reader.Read();
                                                        if (reader.ValueTextEquals(contentProperty))
                                                        {
                                                            reader.Read();
                                                            if (reader.TokenType == JsonTokenType.String)
                                                                WordTiming.Push(WordTiming.CacheKey.ContentKey, reader.GetString());
                                                        }
                                                    }
                                                }
                                                break;
                                            default:
                                                throw new JsonException("Expected an 'alternatives' array.");
                                        }
                                    }
                                    else if (reader.ValueTextEquals(typeProperty))
                                    {
                                        reader.Read();
                                        if (reader.TokenType == JsonTokenType.String)
                                            WordTiming.Push(WordTiming.CacheKey.WordTypeKey, reader.GetString());
                                    }
                                    else
                                    {
                                        reader.Skip();  // skip the end_time property
                                    }
                                    break;
                                case JsonTokenType.EndObject:
                                    if (reader.CurrentDepth == 3)
                                    {
                                        results.Add(WordTiming.AddTiming());
                                        moveToNext = true;      // move to the next JSON object in the items array
                                    }
                                    break;
                                case JsonTokenType.EndArray:
                                    break;
                                default:
                                    throw new JsonException("Expected 'start_time, end_time, alternatives or type' properties within an 'items' array object.");
                            }
                        }
                        break;
                    case JsonTokenType.EndArray:
                        completed = true;
                        break;
                    default:
                        throw new JsonException("Expected a new object as first value of 'transcripts' array.");
                }
            }

            return results;
        }
    }
}
