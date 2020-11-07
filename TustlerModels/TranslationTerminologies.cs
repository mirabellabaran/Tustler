using Amazon.Translate.Model;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
{
    public class TranslationTerminologiesViewModel
    {
        public ObservableCollection<Terminology> TranslationTerminologies
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public TranslationTerminologiesViewModel()
        {
            this.TranslationTerminologies = new ObservableCollection<Terminology>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            if (NeedsRefresh)
            {
                var terminologies = await awsInterface.Translate.ListTerminologies().ConfigureAwait(true);
                ProcessTerminologies(notifications, terminologies);
            }
        }

        private void ProcessTerminologies(NotificationsList errorList, AWSResult<List<TerminologyProperties>> terminologies)
        {
            if (terminologies.IsError)
            {
                errorList.HandleError(terminologies);
            }
            else
            {
                var terminologyProperties = terminologies.Result;
                if (terminologyProperties.Count > 0)
                {
                    var terminologyModelItems = from terminology in terminologyProperties
                                                select new Terminology
                                                {
                                                    Name = terminology.Name,
                                                    Arn = terminology.Arn,
                                                    CreatedAt = terminology.CreatedAt,
                                                    LastUpdatedAt = terminology.LastUpdatedAt,
                                                    Description = terminology.Description,
                                                    SourceLanguageCode = terminology.SourceLanguageCode,
                                                    TargetLanguageCodes = string.Join(",", terminology.TargetLanguageCodes),
                                                    TermCount = terminology.TermCount,
                                                    SizeBytes = terminology.SizeBytes
                                                };

                    this.TranslationTerminologies.Clear();
                    foreach (var item in terminologyModelItems)
                    {
                        this.TranslationTerminologies.Add(item);
                    }
                }

                NeedsRefresh = false;
            }
        }
    }

    public class Terminology
    {
        public string Name { get; internal set; }
        public string Arn { get; internal set; }
        public DateTime CreatedAt { get; internal set; }
        public DateTime LastUpdatedAt { get; internal set; }
        public string Description { get; internal set; }
        public string SourceLanguageCode { get; internal set; }
        public string TargetLanguageCodes { get; internal set; }
        public int TermCount { get; internal set; }
        public int SizeBytes { get; internal set; }
    }
}
