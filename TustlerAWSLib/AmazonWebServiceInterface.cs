using System;
using System.Collections.Generic;
using System.Text;
using TustlerAWSLib.Mocks;
using TustlerInterfaces;

namespace TustlerAWSLib
{
    public class AmazonWebServiceInterface
    {
        public AmazonWebServiceInterface()
        {
            S3 = new MockS3();
        }

        public IAmazonWebInterfaceS3 S3
        {
            get;
            internal set;
        }
    }
}
