using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels
{
    public class LexiconAttributesViewModel
    {
        public ObservableCollection<Lexicon> Attributes
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public LexiconAttributesViewModel()
        {
            this.Attributes = new ObservableCollection<Lexicon>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string lexiconName)
        {
            if (NeedsRefresh)
            {
                var result = await awsInterface.Polly.GetLexicon(lexiconName).ConfigureAwait(true);
                ProcessLexiconAttributes(notifications, result);
            }
        }

        private void ProcessLexiconAttributes(NotificationsList notifications, AWSResult<Amazon.Polly.Model.LexiconAttributes> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var attributes = result.Result;

                this.Attributes.Add(new Lexicon
                {
                    Alphabet = attributes.Alphabet,
                    LanguageCode = attributes.LanguageCode,
                    LastModified = attributes.LastModified,
                    LexemesCount = attributes.LexemesCount,
                    LexiconArn = attributes.LexiconArn,
                    Size = attributes.Size
                });

                NeedsRefresh = false;
            }
        }

    }

    public class Lexicon
    {
        public string Alphabet
        {
            get;
            internal set;
        }

        public string LanguageCode
        {
            get;
            internal set;
        }

        public DateTime LastModified
        {
            get;
            internal set;
        }

        public int LexemesCount
        {
            get;
            internal set;
        }

        public string LexiconArn
        {
            get;
            internal set;
        }

        public int Size
        {
            get;
            internal set;
        }
    }
}
