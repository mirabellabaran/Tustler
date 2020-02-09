using System;
using System.Collections.Generic;
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
    }

}


//// PollyListLexicons list the available lexicons (alphabets and language codes)
//func PollyListLexicons(pollyService* polly.Polly) (*string, error) {
//	result, err := pollyService.ListLexicons(nil)

//	if err != nil {
//		msg := fmt.Sprintf("Unable to list lexicons\nTry changing the configured region")
//		return nil, getTatorError("PollyListLexicons", msg, err)
//	}

//	lexicons, err := getJSONString(result)
//	return lexicons, err
//}

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
