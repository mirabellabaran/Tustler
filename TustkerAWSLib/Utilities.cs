using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;

namespace TustlerAWSLib
{
    public class Utilities
    {
        //RegionEndpoint _region;

        //public RegionEndpoint Region
        //{
        //    get
        //    {
        //        if (_region == null)
        //        {
        //            _region = GetRegion();
        //            return _region;
        //        }
        //        else
        //        {
        //            return _region;
        //        }
        //    }
        //}

        public static string CheckCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            AWSCredentials awsCredentials;
            if (chain.TryGetAWSCredentials("default", out awsCredentials))
            {
                var creds = awsCredentials.GetCredentials();
                return creds.AccessKey;
            }
            else
            {
                return null;
            }
        }

        public static RegionEndpoint GetRegion()
        {
            var chain = new CredentialProfileStoreChain();
            CredentialProfile basicProfile;
            if (chain.TryGetProfile("default", out basicProfile))
            {
                return basicProfile.Region;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the configured region for all clients
        /// </summary>
        public static void SetSessionRegion(string region)
        {
            // e.g. "ap-southeast-2"
            AWSConfigs.AWSRegion = region;
        }

        /// <summary>
        /// Save the access key and secret key to the shared credentials file
        /// </summary>
        /// <remarks>Subsequent calls to CheckCredentials should return a non-null value</remarks>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        public static void StoreCredentials(string accessKey, string secretKey)
        {
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };
            var profile = new CredentialProfile("default", options);
            profile.Region = RegionEndpoint.APSoutheast2;
            var sharedFile = new SharedCredentialsFile();
            sharedFile.RegisterProfile(profile);
        }
    }
}

//// Package awsgosdk provides functions that interact with Amazon Web Services
//// All functions return an error value, usually with a result value.
//// In most cases the result value is the JSON string returned from the AWS web service call.
//package awsgosdk

//import(
//	"encoding/json"
//	"fmt"
//	"io"
//	"io/ioutil"
//	"os"
//	"path/filepath"

//	"github.com/aws/aws-sdk-go/aws"
//	"github.com/aws/aws-sdk-go/aws/awserr"
//	"github.com/aws/aws-sdk-go/aws/session"
//	"github.com/aws/aws-sdk-go/service/polly"
//	"github.com/aws/aws-sdk-go/service/s3"
//	"github.com/aws/aws-sdk-go/service/s3/s3manager"
//	"github.com/aws/aws-sdk-go/service/sns"
//	"github.com/aws/aws-sdk-go/service/sqs"
//	"github.com/aws/aws-sdk-go/service/transcribeservice"
//	"github.com/aws/aws-sdk-go/service/translate"

//	"github.com/gabriel-vasile/mimetype"
//)

//// switch aerr.Code() {
//// case s3.ErrCodeNoSuchBucket:
//// 	msg := fmt.Sprintf("Bucket %s does not exist", bucketName)
//// case s3.ErrCodeNoSuchKey:
//// 	msg := fmt.Sprintf("An item with key %s does not exist in bucket %s", itemName, bucketName)
//// case polly.ErrCodeTextLengthExceededException:
//// 	msg := "Input text too long. Use the PollySynthesizeSpeechTask instead."
//// case polly.ErrCodeTextLengthExceededException:
//// 	msg := "Input text too long; the maximum is 200,000 characters"

////===========================================================

//// CreateSession create a new session using the shared configuration file stored in the folder .aws
//func CreateSession() * session.Session
//{
//	sess := session.Must(
//		session.NewSessionWithOptions(
//			session.Options { SharedConfigState: session.SharedConfigEnable}))

//	_, err := sess.Config.Credentials.Get()
//	if err != nil
//	{
//		fmt.Println("Error getting session credentials:")
//		fmt.Println(err)
//		os.Exit(1)
//	}

//	// // add logging
//	// sess.Handlers.Send.PushFront(func(r *request.Request) {
//	// 	// Log every request made and its payload
//	// 	logger.Println("Request: %s/%s, Payload: %s",
//	// 		r.ClientInfo.ServiceName, r.Operation, r.Params)
//	// })

//	return sess
//}

//// CreateSessionForRegion create a session for the specified region
//// Use this once the region is known to avoid refetching the configuration
//func CreateSessionForRegion(region string) * session.Session
//{
//	sess := session.Must(session.NewSessionWithOptions(session.Options
//	{
//	Config: aws.Config{ Region: aws.String(region)},
//	}))

//	return sess
//}

//// GetConfiguredRegion return the configured region
//// (see the shared configuration file stored in the folder .aws)
//func GetConfiguredRegion() (region*string) {
//	sess := CreateSession()

//	return sess.Config.Region
//}

////===========================================================

//// TatorError holds the function context and the underlying error information
//type TatorError struct {
//	Context string // The called function context
// Code       string // The specific error type (usually an AWS error code e.g. s3.ErrCodeNoSuchBucket)
// Message    string // A friendly message for end-user consumption
// InnerError string // The underlying error value
//}

//func(e TatorError) Error() string {
//	return fmt.Sprintf("%s (%s): %s [%s]", e.Context, e.Code, e.Message, e.InnerError)
//}

//// ToJSON Encode the error as a JSON string
//func(e TatorError) ToJSON() * string {
//	bytes, err := json.Marshal(e)
//	if err != nil {
//		json := fmt.Sprintf(`{
//            "Context": "ToJSON",
//			"Code": "JSON encoding error",
//			"Message": "An error occurred while JSON encoding the inner error condition",
//            "Err": "%s"
//		}`, e.InnerError)
//		return &json
//	}

//	result := string (bytes)
//	return &result
//}

////===========================================================

//func getJSONString(v interface{ }) (*string, error) {
//	bytes, err := json.Marshal(v)
//	if err != nil {
//		return nil, TatorError{
//			"getJSONString",
//			"JSON encoding error",
//			"An error occurred while JSON encoding the inner error condition",
//			err.Error()}
//	}

//	result := string (bytes)
//	return &result, nil
//}

//func getTatorError(functionName string, message string, err error) TatorError {

//	// http://docs.aws.amazon.com/AmazonS3/latest/API/ErrorResponses.html
//	if aerr, ok := err.(awserr.Error); ok {
//		return TatorError{functionName, aerr.Code(), message, err.Error()}
//	}

//	return TatorError{functionName, "TatorError", message, err.Error()}
//}





//// PollyDescribeVoices list and describe the voices that are available in the configured region
//func PollyDescribeVoices(pollyService* polly.Polly, languageCode string) (*string, error) {
//	input := &polly.DescribeVoicesInput{LanguageCode: aws.String(languageCode)}
//	result, err := pollyService.DescribeVoices(input)

//	if err != nil {
//		msg := fmt.Sprintf("Unable to list voices for language code %q\nTry a different code or change the configured region", languageCode)
//		return nil, getTatorError("PollyDescribeVoices", msg, err)
//	}

//	voices, err := getJSONString(result)
//	return voices, err
//}

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

//// SNSListTopics list the SNS topics
//func SNSListTopics(snsService* sns.SNS) (*string, error) {
//	result, err := snsService.ListTopics(nil)
//	if err != nil {
//		msg := "List SNS topics failed"
//		return nil, getTatorError("SNSListTopics", msg, err)
//	}

//	topics, err := getJSONString(result)
//	return topics, err
//}

//// SNSSendMessageToTopic send a message to the specified topic
//func SNSSendMessageToTopic(snsService* sns.SNS, topicARN string, message string) (*string, error) {
//	input := &sns.PublishInput{
//		Message:  aws.String(message),
//		TopicArn: aws.String(topicARN),
//	}

//	result, err := snsService.Publish(input)
//	if err != nil {
//		msg := fmt.Sprintf("Failed to send message to topic %s", topicARN)
//		return nil, getTatorError("SNSSendMessageToTopic", msg, err)
//	}

//	// return the MessageId
//	output, err := getJSONString(result)
//	return output, err
//}

//// SQSListQueues list the SQS queues
//func SQSListQueues(sqsService* sqs.SQS) (*string, error) {
//	result, err := sqsService.ListQueues(nil)
//	if err != nil {
//		msg := "List SQS queues failed"
//		return nil, getTatorError("SQSListQueues", msg, err)
//	}

//	queues, err := getJSONString(result)
//	return queues, err
//}

//// SQSReceiveMessage wait on a single message from the specified queue in long poll mode
//// Note that this function can return both a result AND an error if the receive succeeds but the delete from queue fails
//func SQSReceiveMessage(sqsService* sqs.SQS, queueURL string) (*string, error) {
//	input := &sqs.ReceiveMessageInput{
//		AttributeNames: []* string{
//			aws.String(sqs.MessageSystemAttributeNameSentTimestamp),
//		},
//		MessageAttributeNames: []* string{
//			aws.String(sqs.QueueAttributeNameAll),
//		},
//		QueueUrl:            &queueURL,
//		MaxNumberOfMessages: aws.Int64(1),
//		VisibilityTimeout:   aws.Int64(20), // seconds
//		WaitTimeSeconds:     aws.Int64(20), // seconds
//	}

//	result, err := sqsService.ReceiveMessage(input)
//	if err != nil {
//		msg := fmt.Sprintf("Failed on receive message from queue %s", queueURL)
//		return nil, getTatorError("SQSReceiveMessage", msg, err)
//	}

//	messages, err := getJSONString(result)

//	// delete the message from the queue (note: MaxNumberOfMessages set to one above)
//	if len(result.Messages) > 0 {
//		_, err := sqsService.DeleteMessage(&sqs.DeleteMessageInput{
//			QueueUrl:      &queueURL,
//			ReceiptHandle: result.Messages[0].ReceiptHandle,
//		})

//		if err != nil {
//			msg := fmt.Sprintf("Failed on delete message from queue %s", queueURL)
//			return messages, getTatorError("SQSReceiveMessage", msg, err)
//		}
//	}

//	return messages, err
//}

//// TranslateText translate the supplied text from the specified source language to the target language
//func TranslateText(translateService* translate.Translate, sourceLanguageCode string, targetLanguageCode string, text string) (*string, error) {
//	input := &translate.TextInput{
//		SourceLanguageCode: aws.String(sourceLanguageCode),
//		TargetLanguageCode: aws.String(targetLanguageCode),
//		Text:               aws.String(text),
//	}

//	result, err := translateService.Text(input)
//	if err != nil {
//		msg := fmt.Sprintf("Translation (from %s to %s) failed", sourceLanguageCode, targetLanguageCode)
//		return nil, getTatorError("TranslateText", msg, err)
//	}

//	// return the TranslatedText as JSON
//	output, err := getJSONString(result)
//	return output, err
//}

//// TranscribeStartTranscriptionJob start an asynchronous speech recognition (transcription) task
//func TranscribeStartTranscriptionJob(transcribeService* transcribeservice.TranscribeService, jobName string, bucketName string, medias3Location string, languageCode string) (*string, error) {

//	input := &transcribeservice.StartTranscriptionJobInput{
//		LanguageCode: aws.String(languageCode),
//		Media: &transcribeservice.Media{
//			MediaFileUri: aws.String(medias3Location), // S3 location of the input media file
//		},
//		//MediaFormat: aws.String(mediaFormat),
//		OutputBucketName:     aws.String(bucketName),
//		TranscriptionJobName: aws.String(jobName),
//	}

//	result, err := transcribeService.StartTranscriptionJob(input)
//	if err != nil {
//		msg := fmt.Sprintf("Transcription failed for job %s", jobName)
//		return nil, getTatorError("TranscribeStartTranscriptionJob", msg, err)
//	}

//	job, err := getJSONString(result.TranscriptionJob)
//	return job, err
//}

//// TranscribeGetTranscriptionJob get the status of an asynchronous speech recognition (transcription) task
//func TranscribeGetTranscriptionJob(transcribeService* transcribeservice.TranscribeService, jobName string) (*string, error) {

//	input := &transcribeservice.GetTranscriptionJobInput{
//		TranscriptionJobName: aws.String(jobName),
//	}

//	result, err := transcribeService.GetTranscriptionJob(input)
//	if err != nil {
//		msg := fmt.Sprintf("Get transcription job failed (job name: %s)", jobName)
//		return nil, getTatorError("TranscribeGetTranscriptionJob", msg, err)
//	}

//	job, err := getJSONString(result.TranscriptionJob)
//	return job, err
//}

//// TranscribeListTranscriptionJobs display a list of speech recognition (transcription) tasks
//func TranscribeListTranscriptionJobs(transcribeService* transcribeservice.TranscribeService) (*string, error) {

//	result, err := transcribeService.ListTranscriptionJobs(nil)
//	if err != nil {
//		msg := "List transcription jobs failed"
//		return nil, getTatorError("TranscribeListTranscriptionJobs", msg, err)
//	}

//	jobs, err := getJSONString(result)
//	return jobs, err
//}
