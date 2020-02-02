using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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

        public async static Task<AWSResult<bool?>> DeleteBucketItem(string bucketName, string key)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                using (var client = new AmazonS3Client())
                {
                    var response = await client.DeleteObjectAsync(deleteObjectRequest);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent) // default return code
                    {
                        return new AWSResult<bool?>(null, new ApplicationException($"Request returned status code: {response.HttpStatusCode}"));
                    }
                    else
                    {
                        return new AWSResult<bool?>(true, null);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, ex);
            }
        }

        public async static Task<AWSResult<bool?>> UploadItem(string bucketName, string filePath)
        {
            try
            {
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    FilePath = filePath,
                    StorageClass = S3StorageClass.Standard,
                    //PartSize = 6291456, // 6 MB.
                    //Key = keyName,
                    //CannedACL = S3CannedACL.BucketOwnerFullControl
                };
                //fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                //fileTransferUtilityRequest.Metadata.Add("param2", "Value2");

                using (var client = new AmazonS3Client())
                {
                    var fileTransferUtility = new TransferUtility(client);

                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                    return new AWSResult<bool?>(true, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, ex);
            }
        }

        public async static Task<AWSResult<bool?>> DownloadItem(string bucketName, string key, string filePath)
        {
            try
            {
                var fileTransferUtilityRequest = new TransferUtilityDownloadRequest
                {
                    FilePath = filePath,
                    BucketName = bucketName,
                    Key = key,
                };

                using (var client = new AmazonS3Client())
                {
                    var fileTransferUtility = new TransferUtility(client);

                    await fileTransferUtility.DownloadAsync(fileTransferUtilityRequest);
                    return new AWSResult<bool?>(true, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<bool?>(null, ex);
            }
        }

    }
}



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
