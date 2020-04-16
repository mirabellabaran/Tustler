using Amazon;
using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace TustlerAWSLib
{
    public class Region
    {
        public static RegionEndpoint GetRegion()
        {
            var chain = new CredentialProfileStoreChain();
            if (chain.TryGetProfile("default", out CredentialProfile basicProfile))
            {
                return basicProfile.Region;
            }
            else
            {
                return null;
            }
        }
    }
}
