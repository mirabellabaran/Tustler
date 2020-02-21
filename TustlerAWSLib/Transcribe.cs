using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerAWSLib
{
    public class Transcribe
    {
    }
}

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
