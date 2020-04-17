using Amazon.TranscribeService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels
{
    public class TranscriptionVocabulariesViewModel
    {
        public ObservableCollection<Vocabulary> TranscriptionVocabularies
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public TranscriptionVocabulariesViewModel()
        {
            this.TranscriptionVocabularies = new ObservableCollection<Vocabulary>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            if (NeedsRefresh)
            {
                var vocabularies = await awsInterface.Transcribe.ListVocabularies().ConfigureAwait(true);
                ProcessVocabularies(notifications, vocabularies);
            }
        }

        private void ProcessVocabularies(NotificationsList errorList, AWSResult<List<VocabularyInfo>> vocabularyList)
        {
            if (vocabularyList.IsError)
            {
                errorList.HandleError(vocabularyList);
            }
            else
            {
                var vocabularies = vocabularyList.Result;
                if (vocabularies.Count > 0)
                {
                    var vocabularyModelItems = from vocabulary in vocabularies
                                               select new Vocabulary
                                               {
                                                   VocabularyName = vocabulary.VocabularyName,
                                                   VocabularyState = vocabulary.VocabularyState.Value,
                                                   LanguageCode = vocabulary.LanguageCode,
                                                   LastModifiedTime = vocabulary.LastModifiedTime
                                               };

                    this.TranscriptionVocabularies.Clear();
                    foreach (var item in vocabularyModelItems)
                    {
                        this.TranscriptionVocabularies.Add(item);
                    }
                }

                NeedsRefresh = false;
            }
        }
    }

    public class Vocabulary
    {
        public string VocabularyName { get; internal set; }
        public string VocabularyState { get; internal set; }
        public string LanguageCode { get; internal set; }
        public DateTime LastModifiedTime { get; internal set; }
    }
}
