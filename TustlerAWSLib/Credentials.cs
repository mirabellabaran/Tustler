using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System;

namespace TustlerAWSLib
{
    public class Credentials
    {
        public static ImmutableCredentials GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            AWSCredentials awsCredentials;
            if (chain.TryGetAWSCredentials("default", out awsCredentials))
            {
                var creds = awsCredentials.GetCredentials();
                return creds;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Save the access key and secret key to the shared credentials file
        /// </summary>
        /// <remarks>Subsequent calls to CheckCredentials should return a non-null value</remarks>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        public static void StoreCredentials(string accessKey, string secretKey, RegionEndpoint region)
        {
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };
            var profile = new CredentialProfile("default", options)
            {
                Region = region
            };

            var sharedFile = new SharedCredentialsFile();
            sharedFile.RegisterProfile(profile);
        }
    }
}
