using System;
using System.Collections.Generic;
using System.Text;
using TustlerAWSLib.Mocks;
using TustlerInterfaces;

namespace TustlerAWSLib
{
    public class AmazonWebServiceInterface
    {
        public AmazonWebServiceInterface(RuntimeOptions options)
        {
            if (options.IsMocked)
            {
                EnableMocking();
            }
            else
            {
                DisableMocking();
            }
        }

        public bool IsMocked
        {
            set
            {
                if (value)
                {
                    EnableMocking();
                }
                else
                {
                    DisableMocking();
                }
            }
        }

        public IAmazonWebInterfaceS3 S3
        {
            get;
            internal set;
        }

        public IAmazonWebInterfacePolly Polly
        {
            get;
            internal set;
        }

        public IAmazonWebInterfaceSNS SNS
        {
            get;
            internal set;
        }

        public IAmazonWebInterfaceSQS SQS
        {
            get;
            internal set;
        }

        public IAmazonWebInterfaceTranscribe Transcribe
        {
            get;
            internal set;
        }

        public IAmazonWebInterfaceTranslate Translate
        {
            get;
            internal set;
        }

        private void EnableMocking()
        {
            S3 = new MockS3();

            Polly = new Polly();
            SNS = new SNS();
            SQS = new SQS();
            Transcribe = new Transcribe();
            Translate = new Translate();
        }

        private void DisableMocking()
        {
            S3 = new S3();
            Polly = new Polly();
            SNS = new SNS();
            SQS = new SQS();
            Transcribe = new Transcribe();
            Translate = new Translate();
        }
    }
}
