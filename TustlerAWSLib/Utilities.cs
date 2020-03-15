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
        public static void StoreCredentials(string accessKey, string secretKey, RegionEndpoint region)
        {
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };
            var profile = new CredentialProfile("default", options);
            profile.Region = region;

            var sharedFile = new SharedCredentialsFile();
            sharedFile.RegisterProfile(profile);
        }
    }
}
