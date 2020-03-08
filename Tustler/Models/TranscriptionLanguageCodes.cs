using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Tustler.Models
{
    public class TranscriptionLanguageCodesViewModel
    {
        public ObservableCollection<LanguageCode> LanguageCodes
        {
            get;
            private set;
        }

        public TranscriptionLanguageCodesViewModel()
        {
            var codes = new LanguageCode[] {
                new LanguageCode { Name = "Arabic (Saudi Arabia)", Code="ar-SA" },
                new LanguageCode { Name = "Arabic (UAE)", Code="ar-AE" },
                new LanguageCode { Name = "Chinese", Code="zh-CN" },
                new LanguageCode { Name = "Dutch", Code="nl-NL" },
                new LanguageCode { Name = "English (British)", Code="en-GB" },
                new LanguageCode { Name = "English (American)", Code="en-US" },
                new LanguageCode { Name = "English (Australian)", Code="en-AU" },
                new LanguageCode { Name = "English (Indian)", Code="en-IN" },
                new LanguageCode { Name = "English (Ireland)", Code="en-IE" },
                new LanguageCode { Name = "English (en-AB)", Code="en-AB" },
                new LanguageCode { Name = "English (en-WL)", Code="en-WL" },
                new LanguageCode { Name = "Farsi", Code="fa-IR" },
                new LanguageCode { Name = "French", Code="fr-FR" },
                new LanguageCode { Name = "French (Canadian)", Code="fr-CA" },
                new LanguageCode { Name = "German", Code="de-DE" },
                new LanguageCode { Name = "German (Swiss)", Code="de-CH" },
                new LanguageCode { Name = "Hebrew", Code="he-IL" },
                new LanguageCode { Name = "Hindi", Code="hi-IN" },
                new LanguageCode { Name = "Indonesian", Code="id-ID" },
                new LanguageCode { Name = "Italian", Code="it-IT" },
                new LanguageCode { Name = "Japanese", Code="ja-JP" },
                new LanguageCode { Name = "Korean", Code="ko-KR" },
                new LanguageCode { Name = "Malay", Code="ms-MY" },
                new LanguageCode { Name = "Portuguese", Code="pt-PT" },
                new LanguageCode { Name = "Portuguese (Brazilian)", Code="pt-BR" },
                new LanguageCode { Name = "Russian", Code="ru-RU" },
                new LanguageCode { Name = "Spanish", Code="es-ES" },
                new LanguageCode { Name = "Spanish (American)", Code="es-US" },
                new LanguageCode { Name = "Tamil", Code="ta-IN" },
                new LanguageCode { Name = "Telugu", Code="te-IN" },
                new LanguageCode { Name = "Turkish", Code="tr-TR" },
            };


            this.LanguageCodes = new ObservableCollection<LanguageCode>(codes);
        }
    }
}
