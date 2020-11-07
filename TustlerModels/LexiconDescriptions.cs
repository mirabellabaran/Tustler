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
    public class LexiconsViewModel
    {
        public ObservableCollection<LexiconDescription> Lexicons
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public LexiconsViewModel()
        {
            this.Lexicons = new ObservableCollection<LexiconDescription>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            if (NeedsRefresh)
            {
                var result = await awsInterface.Polly.ListLexicons().ConfigureAwait(true);
                ProcessLexicons(notifications, result);
            }
        }

        private void ProcessLexicons(NotificationsList notifications, AWSResult<List<Amazon.Polly.Model.LexiconDescription>> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var lexicons = result.Result;

                if (lexicons.Count > 0)
                {
                    static void AppendCollection(ObservableCollection<LexiconDescription> collection, List<Amazon.Polly.Model.LexiconDescription> lexicons)
                    {
                        var items = from lexicon in lexicons select new LexiconDescription {
                            Name = lexicon.Name,
                            Attributes = new Lexicon
                                {
                                    Alphabet = lexicon.Attributes.Alphabet,
                                    LanguageCode = lexicon.Attributes.LanguageCode,
                                    LastModified = lexicon.Attributes.LastModified,
                                    LexemesCount = lexicon.Attributes.LexemesCount,
                                    LexiconArn = lexicon.Attributes.LexiconArn,
                                    Size = lexicon.Attributes.Size
                                }
                        };

                        collection.Clear();
                        foreach (var lexicon in items)
                        {
                            collection.Add(lexicon);
                        }
                    };
                    AppendCollection(this.Lexicons, lexicons);
                }
                else
                {
                    notifications.ShowMessage("No Lexicons defined", $"Task: list lexicons completed @ {DateTime.Now.ToShortTimeString()}");
                }

                NeedsRefresh = false;
            }
        }

    }

    public class LexiconDescription
    {
        public string Name
        {
            get;
            internal set;
        }

        public Lexicon Attributes
        {
            get;
            internal set;
        }
    }
}
