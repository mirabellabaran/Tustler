using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;

namespace TustlerAWSLib
{
    public class Polly
    {
        /// <summary>
        /// List and describe the voices that are available in the configured region
        /// </summary>
        /// <param name="languageCode">A language code such as en-us</param>
        /// <returns></returns>
        public async static Task<AWSResult<List<Voice>>> DescribeVoices(string languageCode)
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = languageCode is null ?
                        new DescribeVoicesRequest() :
                        new DescribeVoicesRequest
                        {
                            LanguageCode = languageCode
                        };
                    var result = new List<Voice>();
                    DescribeVoicesResponse response;
                    do
                    {
                        response = await client.DescribeVoicesAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.Voices);
                    } while (response.NextToken != null);

                    return new AWSResult<List<Voice>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<Voice>>(null, new AWSException(nameof(DescribeVoices), "Not connected.", ex));
            }
            catch (InvalidNextTokenException ex)
            {
                return new AWSResult<List<Voice>>(null, new AWSException(nameof(DescribeVoices), "Invalid next token.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<List<Voice>>(null, new AWSException(nameof(DescribeVoices), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// Get a user-defined lexicon by name
        /// </summary>
        /// <param name="lexiconName">The name of the user-defined lexicon</param>
        /// <returns></returns>
        public async static Task<AWSResult<LexiconAttributes>> GetLexicon(string lexiconName)
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new GetLexiconRequest
                    {
                        Name = lexiconName
                    };
                    var response = await client.GetLexiconAsync(request);

                    return new AWSResult<LexiconAttributes>(response.LexiconAttributes, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<LexiconAttributes>(null, new AWSException(nameof(GetLexicon), "Not connected.", ex));
            }
            catch (LexiconNotFoundException ex)
            {
                return new AWSResult<LexiconAttributes>(null, new AWSException(nameof(GetLexicon), "Amazon Polly can't find the specified lexicon.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<LexiconAttributes>(null, new AWSException(nameof(GetLexicon), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// List all user-defined lexicons
        /// </summary>
        /// <returns></returns>
        public async static Task<AWSResult<List<LexiconDescription>>> ListLexicons()
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new ListLexiconsRequest();
                    var result = new List<LexiconDescription>();
                    ListLexiconsResponse response;
                    do
                    {
                        response = await client.ListLexiconsAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.Lexicons);
                    } while (response.NextToken != null);

                    return new AWSResult<List<LexiconDescription>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<LexiconDescription>>(null, new AWSException(nameof(ListLexicons), "Not connected.", ex));
            }
            catch (InvalidNextTokenException ex)
            {
                return new AWSResult<List<LexiconDescription>>(null, new AWSException(nameof(ListLexicons), "Invalid next token.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<List<LexiconDescription>>(null, new AWSException(nameof(ListLexicons), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// Synthesize speech from the specified text and using the specified engine and voice
        /// </summary>
        /// <param name="text">The text to convert to an audio stream</param>
        /// <param name="engine">The speech synthesis engine (standard or neural)</param>
        /// <param name="voiceId">The Id of the voice to use for synthesis</param>
        /// <returns></returns>
        public async static Task<AWSResult<PollyAudioStream>> SynthesizeSpeech(string text, Engine engine, string voiceId = "Joanna")
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new SynthesizeSpeechRequest
                    {
                        OutputFormat = "mp3",
                        //SampleRate = "24000",     // use default: 22050 or 24000, depending on the engine type
                        VoiceId = voiceId,
                        Engine = engine,
                        Text = text
                    };
                    var response = await client.SynthesizeSpeechAsync(request);

                    MemoryStream audioStream = new MemoryStream();
                    response.AudioStream.CopyTo(audioStream);

                    var contentType = response.ContentType;

                    // return the stream and content type
                    return new AWSResult<PollyAudioStream>(new PollyAudioStream(audioStream, contentType), null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<PollyAudioStream>(null, new AWSException(nameof(SynthesizeSpeech), "Not connected.", ex));
            }
            catch (LexiconNotFoundException ex)
            {
                return new AWSResult<PollyAudioStream>(null, new AWSException(nameof(SynthesizeSpeech), "Amazon Polly can't find the specified lexicon.", ex));
            }
            catch (EngineNotSupportedException ex)
            {
                return new AWSResult<PollyAudioStream>(null, new AWSException(nameof(SynthesizeSpeech), "This engine is not compatible with the voice that you have designated.", ex));
            }
            catch (TextLengthExceededException ex)
            {
                return new AWSResult<PollyAudioStream>(null, new AWSException(nameof(SynthesizeSpeech), "The supplied text is longer than the accepted limit of 6000 characters.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<PollyAudioStream>(null, new AWSException(nameof(SynthesizeSpeech), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// Start a long running speech synthesis task using a string as source text
        /// </summary>
        /// <param name="bucketName">The name of an S3 bucket to receive the synthesized output</param>
        /// <param name="key">The key prefix used to store the output within an S3 bucket</param>
        /// <param name="arn">The address (ARN) of the SNS topic used for providing status notification</param>
        /// <param name="text">The text to convert to an audio stream</param>
        /// <param name="engine">The speech synthesis engine (standard or neural)</param>
        /// <param name="voiceId">The Id of the voice to use for synthesis</param>
        /// <returns>The task state, taskId and other task parameters</returns>
        public async static Task<AWSResult<SynthesisTask>> StartSpeechSynthesisTask(string bucketName, string key, string arn, string text, Engine engine, string voiceId = "Joanna")
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new StartSpeechSynthesisTaskRequest
                    {
                        OutputS3BucketName = bucketName,
                        OutputS3KeyPrefix = key,
                        SnsTopicArn = arn,
                        OutputFormat = "mp3",
                        VoiceId = voiceId,
                        Engine = engine,
                        Text = text
                    };
                    var response = await client.StartSpeechSynthesisTaskAsync(request);

                    return new AWSResult<SynthesisTask>(response.SynthesisTask, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "Not connected.", ex));
            }
            catch (InvalidS3BucketException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "The provided Amazon S3 bucket name is invalid.", ex));
            }
            catch (InvalidS3KeyException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "The provided Amazon S3 key prefix is invalid.", ex));
            }
            catch (InvalidSnsTopicArnException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "The provided SNS topic ARN is invalid.", ex));
            }
            catch (LexiconNotFoundException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "Amazon Polly can't find the specified lexicon.", ex));
            }
            catch (EngineNotSupportedException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "This engine is not compatible with the voice that you have designated.", ex));
            }
            catch (TextLengthExceededException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "The supplied text is longer than the accepted limit of 200,000 characters.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTask), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// Start a long running speech synthesis task using a file as source text
        /// </summary>
        /// <param name="bucketName">The name of an S3 bucket to receive the synthesized output</param>
        /// <param name="key">The key prefix used to store the output within an S3 bucket</param>
        /// <param name="arn">The address (ARN) of the SNS topic used for providing status notification</param>
        /// <param name="filePath">The path to a file containing the text to convert to an audio stream</param>
        /// <param name="engine">The speech synthesis engine (standard or neural)</param>
        /// <param name="voiceId">The Id of the voice to use for synthesis</param>
        /// <returns>The task state, taskId and other task parameters</returns>
        public async static Task<AWSResult<SynthesisTask>> StartSpeechSynthesisTaskFromFile(string bucketName, string key, string arn, string filePath, Engine engine, string voiceId = "Joanna")
        {
            string text;

            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (NotSupportedException ex)
            {
                // TODO what exception is generated if filePath is binary???
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(StartSpeechSynthesisTaskFromFile), "An unknown condition has caused a service failure.", ex));
            }

            return await StartSpeechSynthesisTask(bucketName, key, arn, text, engine, voiceId);
        }

        /// <summary>
        /// Get status information on a running or completed speech synthesis task
        /// </summary>
        /// <param name="taskId">The ID of the task</param>
        /// <returns></returns>
        public async static Task<AWSResult<SynthesisTask>> GetSpeechSynthesisTask(string taskId)
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new GetSpeechSynthesisTaskRequest
                    {
                        TaskId = taskId
                    };
                    var response = await client.GetSpeechSynthesisTaskAsync(request);

                    return new AWSResult<SynthesisTask>(response.SynthesisTask, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(GetSpeechSynthesisTask), "Not connected.", ex));
            }
            catch (InvalidTaskIdException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(GetSpeechSynthesisTask), "The provided Task ID is not valid.", ex));
            }
            catch (SynthesisTaskNotFoundException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(GetSpeechSynthesisTask), "The Speech Synthesis task with requested Task ID cannot be found.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<SynthesisTask>(null, new AWSException(nameof(GetSpeechSynthesisTask), "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// List the status of any running or completed speech synthesis tasks
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public async static Task<AWSResult<List<SynthesisTask>>> ListSpeechSynthesisTasks()
        {
            try
            {
                using (var client = new AmazonPollyClient())
                {
                    var request = new ListSpeechSynthesisTasksRequest
                    {
                        // can filter on the Status attribute
                        MaxResults = 10
                    };
                    var result = new List<SynthesisTask>();
                    ListSpeechSynthesisTasksResponse response;
                    do
                    {
                        response = await client.ListSpeechSynthesisTasksAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.SynthesisTasks);
                    } while (response.NextToken != null);

                    return new AWSResult<List<SynthesisTask>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<SynthesisTask>>(null, new AWSException(nameof(ListSpeechSynthesisTasks), "Not connected.", ex));
            }
            catch (InvalidNextTokenException ex)
            {
                return new AWSResult<List<SynthesisTask>>(null, new AWSException(nameof(ListSpeechSynthesisTasks), "The NextToken is invalid.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<List<SynthesisTask>>(null, new AWSException(nameof(ListSpeechSynthesisTasks), "An unknown condition has caused a service failure.", ex));
            }
        }

    }

}
