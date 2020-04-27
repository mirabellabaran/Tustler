using System;
using System.Collections.Generic;
using System.Text;
using TustlerAWSLib.Mocks;
using TustlerInterfaces;

namespace TustlerAWSLib
{
    public class AmazonWebServiceInterface
    {
        public readonly RuntimeOptions options;

        public AmazonWebServiceInterface(RuntimeOptions options)
        {
            this.options = options;

            if (options.IsMocked)
            {
                EnableMocking(options);
            }
            else
            {
                DisableMocking();
            }
        }

        public bool IsMocked
        {
            get
            {
                return S3 is MockS3;
            }
            set
            {
                if (value)
                {
                    EnableMocking(options);
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

        private void EnableMocking(RuntimeOptions options)
        {
            S3 = new MockS3();
            Polly = new MockPolly(this, options.NotificationsARN);      // needs to publish an SNS notification
            SNS = new MockSNS(this, options.NotificationsARN);          // needs to pass notifications to the SQS queue
            SQS = new MockSQS(options.NotificationsQueueURL);
            Transcribe = new MockTranscribe(this);
            Translate = new MockTranslate(this);
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
