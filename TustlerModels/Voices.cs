using CloudWeaver.Foundation.Types;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
{
    public class VoicesViewModel
    {
        public ObservableCollection<Voice> Voices
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public VoicesViewModel()
        {
            this.Voices = new ObservableCollection<Voice>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string languageCode)
        {
            if (NeedsRefresh)
            {
                var result = await awsInterface.Polly.DescribeVoices(languageCode).ConfigureAwait(true);
                ProcessPollyVoices(notifications, result);
            }
        }

        private void ProcessPollyVoices(NotificationsList notifications, AWSResult<List<Amazon.Polly.Model.Voice>> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var voices = result.Result;
                if (voices.Count > 0)
                {
                    static void AppendCollection(ObservableCollection<Voice> collection, List<Amazon.Polly.Model.Voice> voices)
                    {
                        var items = from voice in voices
                                    orderby voice.Id.Value
                                    select new Voice { Id = voice.Id, Name = voice.LanguageName, LanguageCode = voice.LanguageCode.Value, Gender = voice.Gender.Value, SupportedEngines = string.Join(", ", voice.SupportedEngines) };
                                    
                        collection.Clear();
                        foreach (var voice in items)
                        {
                            collection.Add(voice);
                        }
                    };
                    AppendCollection(this.Voices, voices);
                }

                NeedsRefresh = false;
            }
        }

    }

    public class Voice
    {
        public string Id
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public string LanguageCode
        {
            get;
            internal set;
        }

        public string Gender
        {
            get;
            internal set;
        }

        public string SupportedEngines
        {
            get;
            internal set;
        }
    }

}
