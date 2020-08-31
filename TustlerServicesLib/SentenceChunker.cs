using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TustlerServicesLib
{
    public class SentenceChunker
    {
        public const long MinSleepMilliseconds = 10;
        public const long MaxSleepMilliseconds = 5000;
        public const int MaxRetries = 10;

        //internal int chunkSize;     // maximum of 5000 UTF-8 characters per chunk, broken on sentence boundaries

        internal Dictionary<int, string> SourceChunks { get; set; }
        internal Dictionary<int, (bool Complete, string Value)> TranslatedChunks { get; set; }

        /// <summary>
        /// Break the specified text into chunks, ensuring breaks occur on sentence boundaries
        /// </summary>
        /// <param name="text">The text to break into chunks</param>
        /// <remarks>Text data whose length is exactly chunkSize will be chunked at the previous sentence end boundary, producing two chunks</remarks>
        public SentenceChunker(string text, int chunkSize = 5000)
        {
            var nChunks = (text.Length / chunkSize) + 1;

            // the following regex finds sentences ending in a period, an exclamation or a question mark followed by a capital letter (with optional intervening whitespace)
            // while ignoring SOME quote embedded strings
            // example: When he came to the house, he shouted \"Hey, Anybody there?\", and then opened the door and went in. First sentence.Second sentence! Third sentence? Yes.
            // note that the following version, with a standalone sentence inside the quote, breaks the quoted phrase: ...he shouted \"Hey! Anybody there?\", and then ...
            // inspired by the discussions at https://stackoverflow.com/questions/4957226/split-text-into-sentences-in-c-sharp
            MatchCollection matches = Regex.Matches(text, @"(?<=[\.!\?])\s*(?=[A-Z])");

            // each end-of-sentence match is the index of a potential chunk terminator (break point)
            var matchSeq = (matches as IEnumerable<Match>);
            var matchIndices = new Queue<int>(matchSeq.Select(m => m.Success ? m.Index : -1));
            matchIndices.Enqueue(text.Length);      // add the end of the string as a potential chunk terminator

            var aggregator = new Aggregator(chunkSize, nChunks, matchIndices);
            text.Aggregate(aggregator, (agg, c) => {
                agg.Add(c);
                return agg;
            });

            aggregator.Flush();
            SourceChunks = aggregator.Result;
            TranslatedChunks = new Dictionary<int, (bool Complete, string Value)>(SourceChunks.Select(kvp => new KeyValuePair<int, (bool Complete, string Value)>(kvp.Key, (false, null))));
        }

        /// <summary>
        /// Create a sentence chunker from the specified sentences (e.g. a sequence of subtitles that need translation)
        /// </summary>
        /// <param name="sentences">A collection of sentences, to be translated separately</param>
        public SentenceChunker(IEnumerable<string> sentences)
        {
            SourceChunks = new Dictionary<int, string>(sentences.Where(s => !string.IsNullOrEmpty(s)).Select((s, i) => new KeyValuePair<int, string>(i, s)));
            TranslatedChunks = new Dictionary<int, (bool Complete, string Value)>(SourceChunks.Select(kvp => new KeyValuePair<int, (bool Complete, string Value)>(kvp.Key, (false, null))));
        }

        private SentenceChunker(Dictionary<int, string> sourceChunks, Dictionary<int, (bool Complete, string Value)> translatedChunks)
        {
            SourceChunks = sourceChunks;
            TranslatedChunks = translatedChunks;
        }

        public int NumChunks
        {
            get
            {
                return SourceChunks.Count;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> Chunks
        {
            get
            {
                return SourceChunks.AsEnumerable();
            }
        }

        public bool IsChunkTranslated(int index)
        {
            return TranslatedChunks[index].Complete;
        }

        public bool IsJobComplete
        {
            get
            {
                return TranslatedChunks.All(kvp => kvp.Value.Complete);
            }
        }

        public string CompletedTranslation
        {
            get
            {
                if (IsJobComplete)
                    return string.Join(" ", TranslatedChunks.Select(kvp => kvp.Value.Value));
                else
                    return null;
            }
        }

        public string[] AllSentences
        {
            get
            {
                if (IsJobComplete)
                    return TranslatedChunks.Select(kvp => kvp.Value.Value).ToArray();
                else
                    return null;
            }
        }

        // compute an exponential backoff delay when a Rate exceeded message is received
        public static long GetDelay(int retryNum, long minSleepMilliseconds, long maxSleepMilliseconds)
        {
            retryNum = Math.Max(0, retryNum);
            long currentSleepMillis = (long)(minSleepMilliseconds * Math.Pow(2, retryNum));
            return Math.Min(currentSleepMillis, maxSleepMilliseconds);
        }

        public async Task ProcessChunks(Func<int, string, Task<(bool IsErrorState, bool RecoverableError)>> translator)
        {
            var retries = 0;
            var delay = 0L;
            foreach (var kvp in Chunks)
            {
                if (!IsChunkTranslated(kvp.Key))
                {
                    int maxRetries = MaxRetries;
                    while (maxRetries-- > 0)
                    {
                        if (delay > 0L)
                        {
                            await Task.Delay((int)delay).ConfigureAwait(false);
                        }
                        var (isErrorState, recoverableError) = await translator(kvp.Key, kvp.Value);
                        if (isErrorState)
                        {
                            if (recoverableError)
                            {
                                // exponential backoff
                                delay = GetDelay(++retries, MinSleepMilliseconds, MaxSleepMilliseconds);
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void Update(int key, string translatedText)
        {
            TranslatedChunks[key] = (true, translatedText);
        }

        /// <summary>
        /// Save both source and translated chunks to disk
        /// </summary>
        public void ArchiveChunks(string jobName, string folderPath)
        {
            if (SourceChunks != null && TranslatedChunks != null)
            {
                var filePath = Path.Combine(folderPath, jobName);
                filePath = Path.ChangeExtension(filePath, "zip");
                Archive(filePath);
            }
        }

        /// <summary>
        /// Recover source and translated chunks from disk
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static SentenceChunker DeArchiveChunks(string jobName, string folderPath)
        {
            var filePath = Path.ChangeExtension(Path.Combine(folderPath, jobName), "zip");

            return DeArchiveChunks(filePath);
        }

        /// <summary>
        /// Recover source and translated chunks from disk
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static SentenceChunker DeArchiveChunks(string filePath)
        {
            var (sourceChunks, translatedChunks) = DeArchive(filePath);

            return new SentenceChunker(sourceChunks, translatedChunks);
        }

        /// <summary>
        /// Break the text inside the specified file into chunks, ensuring breaks occur on sentence boundaries
        /// </summary>
        /// <param name="textFilePath">The path of a text file</param>
        /// <returns></returns>
        public static SentenceChunker FromFile(string textFilePath, int chunkSize = 5000)
        {
            var contents = File.ReadAllText(textFilePath);

            return new SentenceChunker(contents, chunkSize);
        }

        private void Archive(string filePath)
        {
            using FileStream zipFile = new FileStream(filePath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create);
            foreach (var kvp in SourceChunks)
            {
                ZipArchiveEntry entry = archive.CreateEntry($"Source{kvp.Key}.txt");
                using StreamWriter writer = new StreamWriter(entry.Open());
                writer.Write(kvp.Value);
            }
            foreach (var kvp in TranslatedChunks)
            {
                var (completed, value) = kvp.Value;
                if (completed)
                {
                    ZipArchiveEntry entry = archive.CreateEntry($"Translated{kvp.Key}.txt");
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write(value);
                }
            }
        }

        private static (Dictionary<int, string> sourceChunks, Dictionary<int, (bool Complete, string Value)> translatedChunks) DeArchive(string filePath)
        {
            var re = new Regex("(Source|Translated)([0-9]+).txt");
            var sourceChunks = new Dictionary<int, string>();
            var translatedChunks = new Dictionary<int, (bool Complete, string Value)>();

            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        var m = re.Match(entry.FullName);
                        if (m.Success && m.Groups.Count > 1)
                        {
                            if (int.TryParse(m.Groups[2].Value, out int index))
                            {
                                using StreamReader reader = new StreamReader(entry.Open());
                                var text = reader.ReadToEnd();
                                if (entry.FullName.Contains("Source"))
                                {
                                    sourceChunks.Add(index, text);
                                }
                                else if (entry.FullName.Contains("Translated"))
                                {
                                    translatedChunks.Add(index, (true, text));
                                }
                            }
                        }
                    }
                }
            }

            // add incomplete entries to translated dictionary
            var incompleteKeys = sourceChunks.Keys.Where(key => !translatedChunks.ContainsKey(key));
            foreach (var key in incompleteKeys)
            {
                translatedChunks.Add(key, (false, null));
            }

            return (sourceChunks, translatedChunks);
        }

        private class Aggregator
        {
            private readonly Queue<int> matchIndices;
            private readonly StringBuilder sb;
            private readonly List<string> result;
            private int currentIndex;
            private int endOfSentenceIndex;

            /// <summary>
            /// Breaks the text into chunks with breaks only at sentence ends
            /// Ensure that each chunk is close to but does not exceed the chunk size
            /// </summary>
            /// <param name="chunkSize"></param>
            /// <param name="nChunks"></param>
            /// <param name="matches"></param>
            public Aggregator(int chunkSize, int nChunks, Queue<int> matches)
            {
                result = new List<string>(nChunks);
                currentIndex = 0;
                sb = new StringBuilder(chunkSize);

                // filter the sentence indices leaving only those close to chunk-sized boundaries
                var (filtered, _, _) = matches.Aggregate((Result: new Queue<int>(), MaxIndex: chunkSize, LastIndex: 0), (agg, index) =>
                {
                    if (index < agg.MaxIndex)
                    {
                        return (agg.Result, agg.MaxIndex, index);
                    }
                    else
                    {
                        agg.Result.Enqueue(agg.LastIndex);
                        return (agg.Result, agg.LastIndex + chunkSize, index);
                    }
                });

                // add the last chunk
                var lastFilteredIndex = (filtered.Count > 0)? filtered.Max() : 0;
                var lastUnfilteredIndex = matches.Max();
                if (lastFilteredIndex < lastUnfilteredIndex)
                {
                    filtered.Enqueue(lastUnfilteredIndex);
                }

                matchIndices = filtered;
                endOfSentenceIndex = matchIndices.Dequeue();
            }

            public Dictionary<int, string> Result
            {
                get
                {
                    var indexed = result.Select((s, i) => new KeyValuePair<int, string>(i, s));
                    return new Dictionary<int, string>(indexed);
                }
            }

            public void Add(char c)
            {
                currentIndex++;

                if (currentIndex > endOfSentenceIndex)
                {
                    result.Add(sb.ToString());
                    endOfSentenceIndex = (matchIndices.Count > 0) ? matchIndices.Dequeue() : int.MaxValue;
                    sb.Clear();
                }

                if (!(sb.Length == 0 && c == 32))      // ignore leading spaces
                    sb.Append(c);
            }

            /// <summary>
            /// Flush the string buffer
            /// </summary>
            public void Flush()
            {
                if (sb.Length > 0)
                {
                    result.Add(sb.ToString());
                }
            }
        }
    }
}
