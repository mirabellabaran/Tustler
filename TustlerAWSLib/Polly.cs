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
                return new AWSResult<List<Voice>>(null, new AWSException("DescribeVoices", "Not connected.", ex));
            }
            catch (InvalidNextTokenException ex)
            {
                return new AWSResult<List<Voice>>(null, new AWSException("DescribeVoices", "Invalid next token.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<List<Voice>>(null, new AWSException("DescribeVoices", "An unknown condition has caused a service failure.", ex));
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
                return new AWSResult<LexiconAttributes>(null, new AWSException("GetLexicon", "Not connected.", ex));
            }
            catch (LexiconNotFoundException ex)
            {
                return new AWSResult<LexiconAttributes>(null, new AWSException("GetLexicon", "Amazon Polly can't find the specified lexicon.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<LexiconAttributes>(null, new AWSException("GetLexicon", "An unknown condition has caused a service failure.", ex));
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
                return new AWSResult<List<LexiconDescription>>(null, new AWSException("ListLexicons", "Not connected.", ex));
            }
            catch (InvalidNextTokenException ex)
            {
                return new AWSResult<List<LexiconDescription>>(null, new AWSException("ListLexicons", "Invalid next token.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<List<LexiconDescription>>(null, new AWSException("ListLexicons", "An unknown condition has caused a service failure.", ex));
            }
        }

        /// <summary>
        /// Synthesize speech from the specified text and using the specified engine and voice
        /// </summary>
        /// <param name="text">The text to convert to an audio stream</param>
        /// <param name="engine">The speech synthesis engine (standard or neural)</param>
        /// <param name="voiceId">The Id of the voice to use for synthesis</param>
        /// <returns></returns>
        public async static Task<AWSResult<PollyResponse>> SynthesizeSpeech(string text, Engine engine, string voiceId = "Joanna")
        {
            var methodName = nameof(SynthesizeSpeech);
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
                    var contentLength = response.ContentLength;

                    // write to file and return the file path and content type
                    return new AWSResult<PollyResponse>(new PollyResponse(audioStream, contentType, contentLength), null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<PollyResponse>(null, new AWSException(methodName, "Not connected.", ex));
            }
            catch (LexiconNotFoundException ex)
            {
                return new AWSResult<PollyResponse>(null, new AWSException("SynthesizeSpeech", "Amazon Polly can't find the specified lexicon.", ex));
            }
            catch (EngineNotSupportedException ex)
            {
                return new AWSResult<PollyResponse>(null, new AWSException("SynthesizeSpeech", "This engine is not compatible with the voice that you have designated.", ex));
            }
            catch (TextLengthExceededException ex)
            {
                return new AWSResult<PollyResponse>(null, new AWSException("SynthesizeSpeech", "The supplied text is longer than the accepted limit of 6000 characters.", ex));
            }
            catch (ServiceFailureException ex)
            {
                return new AWSResult<PollyResponse>(null, new AWSException("SynthesizeSpeech", "An unknown condition has caused a service failure.", ex));
            }
        }

    }

}



//// PollySynthesizeSpeech create an audio file from the specified text file
//func PollySynthesizeSpeech(pollyService* polly.Polly, inputString string, outputfileName string, outputFormat string, voiceID string) error {

//	// check output filename
//	if ext := filepath.Ext(outputfileName); ext != fmt.Sprintf(".%s", outputFormat) {
//		msg := "The output filename extension must match the output format"
//		internalError := fmt.Errorf("non-matching extension format (%q vs %q)", ext, outputFormat)
//		return getTatorError("PollySynthesizeSpeech", msg, internalError)
//	}

//	input := &polly.SynthesizeSpeechInput{OutputFormat: aws.String(outputFormat), Text: aws.String(inputString),
//		VoiceId: aws.String(voiceID)}
//	output, err := pollyService.SynthesizeSpeech(input)

//	if err != nil {
//		msg := fmt.Sprintf("Synthesize speech failed for output format %q, voice %q", outputFormat, voiceID)
//		return getTatorError("PollySynthesizeSpeech", msg, err)
//	}

//	outFile, err := os.Create(outputfileName)
//	if err != nil {
//		msg := fmt.Sprintf("Error creating output file %q", outputfileName)
//		return getTatorError("PollySynthesizeSpeech", msg, err)
//	}

//	defer outFile.Close()
//	_, err = io.Copy(outFile, output.AudioStream)
//	if err != nil {
//		msg := fmt.Sprintf("Error writing output file %q", outputfileName)
//		return getTatorError("PollySynthesizeSpeech", msg, err)
//	}

//	return nil
//}

//// PollyStartSpeechSynthesisTask start an asynchronous speech synthesis task
//func PollyStartSpeechSynthesisTask(pollyService* polly.Polly, inputFileName string, bucketName string, itemName string, outputFormat string, voiceID string) (*string, error) {

//	inputString, err := getFileContentsAsString(inputFileName)
//	if err != nil {
//		return nil, err
//	}

//	input := &polly.StartSpeechSynthesisTaskInput{
//		OutputFormat:       aws.String(outputFormat),
//		OutputS3BucketName: aws.String(bucketName),
//		OutputS3KeyPrefix:  aws.String(itemName),
//		Text:               inputString,
//		VoiceId:            aws.String(voiceID)}

//	return pollyStartSpeechSynthesisTaskImpl(pollyService, input)
//}

//// PollyStartSpeechSynthesisTaskWithNotification start an asynchronous speech synthesis task
//// with notification on completion via the specified topic ARN
//func PollyStartSpeechSynthesisTaskWithNotification(pollyService* polly.Polly, inputFileName string, bucketName string, itemName string, outputFormat string, voiceID string, topicARN string) (*string, error) {

//	inputString, err := getFileContentsAsString(inputFileName)
//	if err != nil {
//		return nil, err
//	}

//	input := &polly.StartSpeechSynthesisTaskInput{
//		OutputFormat:       aws.String(outputFormat),
//		OutputS3BucketName: aws.String(bucketName),
//		OutputS3KeyPrefix:  aws.String(itemName),
//		Text:               inputString,
//		SnsTopicArn:        aws.String(topicARN),
//		VoiceId:            aws.String(voiceID)}

//	return pollyStartSpeechSynthesisTaskImpl(pollyService, input)
//}

//func getFileContentsAsString(inputFileName string) (*string, error) {
//	contents, err := ioutil.ReadFile(inputFileName)
//	if err != nil {
//		msg := fmt.Sprintf("Error while reading the input filename")
//		return nil, getTatorError("getFileContentsAsString", msg, err)
//	}

//	strContents := string (contents[:])
//	return &strContents, nil
//}

//func pollyStartSpeechSynthesisTaskImpl(pollyService* polly.Polly, input* polly.StartSpeechSynthesisTaskInput) (*string, error) {

//	result, err := pollyService.StartSpeechSynthesisTask(input)

//	if err != nil {
//		msg := fmt.Sprintf("Speech synthesis task failed for output format %q, voice %q", * input.OutputFormat, * input.VoiceId)
//		return nil, getTatorError("pollyStartSpeechSynthesisTaskImpl", msg, err)
//	}

//	task, err := getJSONString(result.SynthesisTask)
//	return task, err
//}

//// PollyGetSpeechSynthesisTask get information on a speech synthesis task
//func PollyGetSpeechSynthesisTask(pollyService* polly.Polly, taskID string) (*string, error) {

//	input := &polly.GetSpeechSynthesisTaskInput{TaskId: aws.String(taskID)}

//	result, err := pollyService.GetSpeechSynthesisTask(input)
//	if err != nil {
//		msg := fmt.Sprintf("Get speech synthesis task failed for taskID %q", taskID)
//		return nil, getTatorError("PollyGetSpeechSynthesisTask", msg, err)
//	}

//	task, err := getJSONString(result.SynthesisTask)
//	return task, err
//}

//// PollyListSpeechSynthesisTasks generate a list of known speech synthesis tasks
//func PollyListSpeechSynthesisTasks(pollyService* polly.Polly) (*string, error) {

//	result, err := pollyService.ListSpeechSynthesisTasks(nil)
//	if err != nil {
//		msg := "List speech synthesis tasks failed"
//		return nil, getTatorError("PollyListSpeechSynthesisTasks", msg, err)
//	}

//	tasks, err := getJSONString(result)
//	return tasks, err
//}
