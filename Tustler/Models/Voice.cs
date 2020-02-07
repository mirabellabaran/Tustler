using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tustler.Models
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

        public async Task Refresh(NotificationsList notifications, string languageCode)
        {
            if (NeedsRefresh)
            {
                var result = await TustlerAWSLib.Polly.DescribeVoices(languageCode).ConfigureAwait(true);
                ProcessPollyVoices(notifications, result);
            }
        }

        private void ProcessPollyVoices(NotificationsList notifications, TustlerAWSLib.AWSResult<List<Amazon.Polly.Model.Voice>> result)
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
                    static void AppendBucketCollection(ObservableCollection<Voice> collection, List<Amazon.Polly.Model.Voice> voices)
                    {
                        var items = from voice in voices select new Voice { Name = voice.LanguageName, LanguageCode = voice.LanguageCode.Value, Gender = voice.Gender.Value, SupportedEngines = voice.SupportedEngines };

                        collection.Clear();
                        foreach (var voice in items)
                        {
                            collection.Add(voice);
                        }
                    };
                    AppendBucketCollection(this.Voices, voices);
                }

                NeedsRefresh = false;
            }
        }

    }

    public class Voice
    {
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

        public List<string> SupportedEngines
        {
            get;
            internal set;
        }
    }

}
