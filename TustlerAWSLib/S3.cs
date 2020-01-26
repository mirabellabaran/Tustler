using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TustlerAWSLib
{
    // AmazonS3Exception

    public class S3
    {
        /// <summary>
        /// Get a list of all buckets and their creation times
        /// </summary>
        /// <returns></returns>
        public async static Task<AWSResult<List<S3Bucket>>> ListBuckets()
        {
            try
            {
                using (var client = new AmazonS3Client())
                {
                    var response = await client.ListBucketsAsync();
                    return new AWSResult<List<S3Bucket>>(response.Buckets, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<S3Bucket>>(null, ex);
            }
        }

        /// <summary>
        /// Get a list of bucket items for the specified bucket
        /// </summary>
        /// <param name="bucketName">The name of the bucket to fetch items from</param>
        /// <returns></returns>
        public async static Task<AWSResult<List<S3Object>>> ListBucketItems(string bucketName)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 10
                };
                ListObjectsV2Response response;
                using (var client = new AmazonS3Client())
                {
                    do
                    {
                        response = await client.ListObjectsV2Async(request);

                        // Process the response.
                        foreach (S3Object entry in response.S3Objects)
                        {
                            Console.WriteLine("key = {0} size = {1}",
                                entry.Key, entry.Size);
                        }
                        Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                        request.ContinuationToken = response.NextContinuationToken;
                    } while (response.IsTruncated);

                    return new AWSResult<List<S3Object>>(response.S3Objects, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<S3Object>>(null, ex);
            }
        }

        public async static Task<AWSResult<MetadataCollection>> GetItemMetadata(string bucketName, string key)
        {
            try
            {
                using (var client = new AmazonS3Client())
                {
                    var response = await client.GetObjectMetadataAsync(bucketName, key);

                    return new AWSResult<MetadataCollection>(response.Metadata, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<MetadataCollection>(null, ex);
            }
        }
    }
}


//// S3GetItemMetaData get the user attached metadata for the specified bucket item
//// (see S3UploadFile)
//func S3GetItemMetaData(s3Service* s3.S3, bucketName string, itemName string) (*string, error) {
//	result, err := s3Service.HeadObject(&s3.HeadObjectInput{
//		Bucket: aws.String(bucketName),
//		Key:    aws.String(itemName),
//	})
//	if err != nil {
//		msg := fmt.Sprintf("Get item metadata failed for bucket %q, item %q", bucketName, itemName)
//		return nil, getTatorError("S3GetItemMetaData", msg, err)
//	}

//	metadata, err := getJSONString(result)
//	return metadata, err
//}

//// S3UploadFile upload the contents of the given file to an S3 bucket
//func S3UploadFile(session* session.Session, bucketName string, filename string) (*string, error) {
//	file, err := os.Open(filename)
//	if err != nil {
//		msg := fmt.Sprintf("Unable to open file %q", filename)
//		return nil, getTatorError("S3UploadFile", msg, err)
//	}
//	defer file.Close()

//	// attempt to detect file type
//mime, extension, err := mimetype.DetectReader(file)
//	if err != nil {
//		msg := fmt.Sprintf("File type detection failed on %q", filename)
//		return nil, getTatorError("S3UploadFile", msg, err)
//	}

//	// reset file position
//	_, err = file.Seek(0, io.SeekStart)
//	if err != nil {
//		msg := fmt.Sprintf("Failed resetting file position on %q", filename)
//		return nil, getTatorError("S3UploadFile", msg, err)
//	}

//	uploader := s3manager.NewUploader(session)

//	_, keyname := filepath.Split(filename)
//	result, err := uploader.Upload(&s3manager.UploadInput{
//		Bucket: aws.String(bucketName),
//		Key:    aws.String(keyname),
//		Body:   file,
//		Metadata: map[string]* string{
//			"MimeType":  aws.String(mime),
//			"Extension": aws.String(extension),
//		},
//	})

//	if err != nil {
//		if aerr, ok := err.(awserr.Error); ok {
//			switch aerr.Code() {
//			case s3.ErrCodeNoSuchBucket:
//				msg := fmt.Sprintf("Bucket %s does not exist", bucketName)
//				return nil, getTatorError("S3UploadFile", msg, err)
//			default:
//				msg := fmt.Sprintf("Unable to upload %q to %q", filename, bucketName)
//				return nil, getTatorError("S3UploadFile", msg, err)
//			}
//		}
//	}

//	// return JSON containing the Location (S3 URL) which was the destination for the upload
//	output, err := getJSONString(result)
//	return output, err
//}

//type s3DownloadOutput struct {
//	Filename string
//	NumBytes int64
//}

//// S3DownloadFile download a file from the specified bucket to a local file
//// using the specified item name as the key
//// Returns (filename, numBytes, err)
//func S3DownloadFile(session* session.Session, bucketName string, itemName string, fileName string) (*string, error) {
//	file, err := os.Create(fileName)
//	if err != nil {
//		msg := fmt.Sprintf("Unable to create file %q", fileName)
//		return nil, getTatorError("S3DownloadFile", msg, err)
//	}

//	downloader := s3manager.NewDownloader(session)

//	numBytes, err := downloader.Download(file,
//		&s3.GetObjectInput{
//			Bucket: aws.String(bucketName),
//			Key:    aws.String(itemName),
//		})
//	if err != nil {
//		msg := fmt.Sprintf("Download failed for bucket %q, item %q", bucketName, itemName)
//		return nil, getTatorError("S3DownloadFile", msg, err)
//	}

//	result := s3DownloadOutput{
//		Filename: file.Name(),
//		NumBytes: numBytes,
//	}

//	output, err := getJSONString(result)
//	return output, err
//}

//// S3DeleteItem delete the specified item from the specified bucket
//func S3DeleteItem(s3Service* s3.S3, bucketName string, itemName string) error {
//	_, err := s3Service.DeleteObject(&s3.DeleteObjectInput{Bucket: aws.String(bucketName), Key: aws.String(itemName)})
//	if err != nil {
//		msg := fmt.Sprintf("Delete failed for bucket %q, item %q", bucketName, itemName)
//		return getTatorError("S3DeleteItem", msg, err)
//	}
//	err = s3Service.WaitUntilObjectNotExists(&s3.HeadObjectInput{
//		Bucket: aws.String(bucketName),
//		Key:    aws.String(itemName),
//	})
//	if err != nil {
//		msg := fmt.Sprintf("Waiting on delete failed for item %q in bucket %q", itemName, bucketName)
//		return getTatorError("S3DeleteItem", msg, err)
//	}

//	return nil
//}
