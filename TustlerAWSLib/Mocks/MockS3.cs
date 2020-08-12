using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib.Mocks
{
    public static class MyExtensions
    {
        public static IEnumerable<(string itemA, string itemB)> Combine(this string[] namesA, string[] namesB)
        {
            foreach (var itemA in namesA)
            {
                foreach (var itemB in namesB)
                    yield return (itemA, itemB);
            }
        }
    }

    public class MockS3 : IAmazonWebInterfaceS3
    {
        private readonly IEnumerable<S3Bucket> buckets;
        private readonly ConcurrentDictionary<string, List<S3Object>> bucketItemDictionary;
        private readonly ConcurrentDictionary<string, (string, string)> metaDataDictionary;   // key is S3 item key; value is (mimetype, extension)

        public MockS3()
        {
            var bucketNames = new string[] { "tator", "test" };
            buckets = bucketNames.Select(bucketName => new S3Bucket() { BucketName = bucketName, CreationDate = DateTime.Now });

            // each bucket gets the same sequence of keys
            var itemKeys = new string[] { "item1", "item2", "item3" };
            var keysAndBuckets = itemKeys.Combine(bucketNames);

            // create the initial metadata dictionary
            metaDataDictionary = new ConcurrentDictionary<string, (string, string)>(itemKeys.Select(s3key =>
            {
                // assign metadata by item name
                var pair = s3key switch
                {
                    "item1" => ("audio/mpeg", "mp3"),
                    "item2" => ("video/mp4", "mp4"),
                    "item3" => ("text/plain", "txt"),
                    _ => throw new ArgumentException("Unknown key")
                };

                return KeyValuePair.Create(s3key, pair);
            }));

            var bucketItems = keysAndBuckets.Select(pair => CreateS3Object(pair.itemB, pair.itemA))
                .GroupBy(item => item.BucketName, (dictkey, items) => KeyValuePair.Create(dictkey, new List<S3Object>(items)));

            bucketItemDictionary = new ConcurrentDictionary<string, List<S3Object>>(bucketItems);
        }

        private S3Object CreateS3Object(string bucketName, string key)
        {
            return new S3Object()
            {
                BucketName = bucketName,
                ETag = "ETag",
                Key = key,
                LastModified = DateTime.Now - TimeSpan.FromDays(1.0),
                Owner = new Owner() { DisplayName = "TustlerOwner", Id = "TustlerId" },
                Size = 100,
                StorageClass = S3StorageClass.Standard
            };
        }

        /// <summary>
        /// Add a new bucket item
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="key"></param>
        /// <param name="mimetype"></param>
        /// <param name="extension"></param>
        /// <returns>The specified key, or a modified key if the key already exists in this bucket</returns>
        public string AddBucketItem(string bucketName, string key, string mimetype, string extension)
        {
            var keyExists = bucketItemDictionary[bucketName].Exists(obj => obj.Key == key);
            var newKey = keyExists ? $"{key}-{Guid.NewGuid()}" : key;

            var obj = CreateS3Object(bucketName, newKey);

            bucketItemDictionary.AddOrUpdate(bucketName,
                dictkey => new List<S3Object>() { obj },
                (dictkey, items) => { items.Add(obj); return items;  }
            );

            metaDataDictionary.AddOrUpdate(newKey, (mimetype, extension), (outerKey, existingList) => existingList);

            return newKey;
        }

        public async Task<AWSResult<List<S3Bucket>>> ListBuckets()
        {
            await Task.Delay(1000);
            return await Task.FromResult(new AWSResult<List<S3Bucket>>(new List<S3Bucket>(buckets), null));
        }

        public async Task<AWSResult<List<S3Object>>> ListBucketItems(string bucketName)
        {
            var bucketItems = new List<S3Object>(bucketItemDictionary[bucketName]);

            await Task.Delay(1000);
            return await Task.FromResult(new AWSResult<List<S3Object>>(bucketItems, null));
        }

        public async Task<AWSResult<MetadataCollection>> GetItemMetadata(string bucketName, string key)
        {
            var metadata = new MetadataCollection();
            await Task.Delay(300);

            if (metaDataDictionary.ContainsKey(key))
            {
                var (mimeType, extension) = metaDataDictionary[key];

                metadata["mimetype"] = mimeType;
                metadata["extension"] = extension;

                return await Task.FromResult(new AWSResult<MetadataCollection>(metadata, null));
            }
            else
            {
                var ex = new AWSException("Mock S3 GetItemMetadata", "Get metadata error", new AmazonS3Exception("The S3 item key does not exist in the metadata dictionary"));
                return await Task.FromResult(new AWSResult<MetadataCollection>(null, ex));
            }
        }

        public async Task<AWSResult<(bool?, string)>> DeleteBucketItem(string bucketName, string key)
        {
            var itemExists = bucketItemDictionary.ContainsKey(bucketName) && bucketItemDictionary[bucketName].Exists(item => item.Key == key);
            await Task.Delay(1000);

            if (itemExists)
            {
                bucketItemDictionary.AddOrUpdate(bucketName, (List<S3Object>)null, (outerKey, existingList) =>
               {
                   return new List<S3Object>(existingList.Where(item => item.Key != key));
               });

                return await Task.FromResult(new AWSResult<(bool?, string)>((true, key), null));
            }
            else
            {
                return await Task.FromResult(new AWSResult<(bool?, string)>((false, key), null));
            }
        }

        public async Task<AWSResult<(bool?, string)>> UploadItem(string bucketName, string newKey, string filePath, string mimetype, string extension)
        {
            // add an item to the pretend S3 bucket
            var itemExists = bucketItemDictionary.ContainsKey(bucketName) && bucketItemDictionary[bucketName].Exists(item => item.Key == newKey);
            await Task.Delay(1000);

            if (itemExists)
            {
                var ex = new AWSException("Mock S3 UploadItem", "Upload error", new AmazonS3Exception("An item with that key already exists"));
                return await Task.FromResult(new AWSResult<(bool?, string)>((false, filePath), ex));
            }
            else
            {
                bucketItemDictionary.AddOrUpdate(bucketName, (List<S3Object>) null, (outerKey, existingList) =>
                    {
                        var newItem = new S3Object()
                        {
                            BucketName = bucketName,
                            ETag = "ETag",
                            Key = newKey,
                            LastModified = DateTime.Now - TimeSpan.FromDays(1.0),
                            Owner = new Owner() { DisplayName = "TustlerOwner", Id = "TustlerId" },
                            Size = 100,
                            StorageClass = S3StorageClass.Standard
                        };

                        existingList.Add(newItem);

                        return existingList;
                    });

                metaDataDictionary.AddOrUpdate(newKey, (mimetype, extension), (outerKey, existingList) => existingList);

                return await Task.FromResult(new AWSResult<(bool?, string)>((true, filePath), null));
            }
        }

        public async Task<AWSResult<(bool?, string)>> DownloadItemToFile(string bucketName, string key, string filePath)
        {
            // pretend to retrieve an item from the pretend S3 bucket
            var itemExists = bucketItemDictionary.ContainsKey(bucketName) && bucketItemDictionary[bucketName].Exists(item => item.Key == key);
            await Task.Delay(1000);

            if (itemExists)
            {
                // create an empty 'placeholder' file at the specified file path
                File.Create(filePath);

                return await Task.FromResult(new AWSResult<(bool?, string)>((true, filePath), null));
            }
            else
            {
                var ex = new AWSException("Mock S3 DownloadItemToFile", "Download error", new AmazonS3Exception("The item key does not exist"));
                return await Task.FromResult(new AWSResult<(bool?, string)>((null, filePath), ex));
            }
        }

        public async Task<AWSResult<Stream>> DownloadItemAsStream(string bucketName, string key)
        {
            // pretend to retrieve an item from the pretend S3 bucket
            var itemExists = bucketItemDictionary.ContainsKey(bucketName) && bucketItemDictionary[bucketName].Exists(item => item.Key == key);
            await Task.Delay(1000);

            if (itemExists)
            {
                var data = Encoding.UTF8.GetBytes("This is mocked stream data");
                var memstream = new MemoryStream(data);

                return await Task.FromResult(new AWSResult<Stream>(memstream, null));
            }
            else
            {
                var ex = new AWSException("Mock S3 DownloadItemAsStream", "Download error", new AmazonS3Exception("The item key does not exist"));
                return await Task.FromResult(new AWSResult<Stream>(null, ex));
            }
        }
    }
}
