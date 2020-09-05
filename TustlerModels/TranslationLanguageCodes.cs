using System.Collections.ObjectModel;

namespace TustlerModels
{
    public class TranslationLanguageCodesViewModel : LanguageCodesViewModel
    {
        public override ObservableCollection<LanguageCode> LanguageCodes
        {
            get;
            set;
        }

        public TranslationLanguageCodesViewModel()
        {
            var codes = new LanguageCode[] {
                new LanguageCode { Name = "Afrikaans", Code = "af" },
                new LanguageCode { Name = "Albanian", Code = "sq" },
                new LanguageCode { Name = "Amharic", Code = "am" },
                new LanguageCode { Name = "Arabic", Code = "ar" },
                new LanguageCode { Name = "Azerbaijani", Code = "az" },
                new LanguageCode { Name = "Bengali", Code = "bn" },
                new LanguageCode { Name = "Bosnian", Code = "bs" },
                new LanguageCode { Name = "Bulgarian", Code = "bg" },
                new LanguageCode { Name = "Chinese (Simplified)", Code = "zh" },
                new LanguageCode { Name = "Chinese (Traditional)", Code = "zh-TW" },
                new LanguageCode { Name = "Croatian", Code = "hr" },
                new LanguageCode { Name = "Czech", Code = "cs" },
                new LanguageCode { Name = "Danish", Code = "da" },
                new LanguageCode { Name = "Dari", Code = "fa-AF" },
                new LanguageCode { Name = "Dutch", Code = "nl" },
                new LanguageCode { Name = "English", Code = "en" },
                new LanguageCode { Name = "Estonian", Code = "et" },
                new LanguageCode { Name = "Finnish", Code = "fi" },
                new LanguageCode { Name = "French", Code = "fr" },
                new LanguageCode { Name = "French (Canadian)", Code = "fr-CA" },
                new LanguageCode { Name = "Georgian", Code = "ka" },
                new LanguageCode { Name = "German", Code = "de" },
                new LanguageCode { Name = "Greek", Code = "el" },
                new LanguageCode { Name = "Hausa", Code = "ha" },
                new LanguageCode { Name = "Hebrew", Code = "he" },
                new LanguageCode { Name = "Hindi", Code = "hi" },
                new LanguageCode { Name = "Hungarian", Code = "hu" },
                new LanguageCode { Name = "Indonesian", Code = "id" },
                new LanguageCode { Name = "Italian", Code = "it" },
                new LanguageCode { Name = "Japanese", Code = "ja" },
                new LanguageCode { Name = "Korean", Code = "ko" },
                new LanguageCode { Name = "Latvian", Code = "lv" },
                new LanguageCode { Name = "Malay", Code = "ms" },
                new LanguageCode { Name = "Norwegian", Code = "no" },
                new LanguageCode { Name = "Persian", Code = "fa" },
                new LanguageCode { Name = "Pashto", Code = "ps" },
                new LanguageCode { Name = "Polish", Code = "pl" },
                new LanguageCode { Name = "Portuguese", Code = "pt" },
                new LanguageCode { Name = "Romanian", Code = "ro" },
                new LanguageCode { Name = "Russian", Code = "ru" },
                new LanguageCode { Name = "Serbian", Code = "sr" },
                new LanguageCode { Name = "Slovak", Code = "sk" },
                new LanguageCode { Name = "Slovenian", Code = "sl" },
                new LanguageCode { Name = "Somali", Code = "so" },
                new LanguageCode { Name = "Spanish", Code = "es" },
                new LanguageCode { Name = "Swahili", Code = "sw" },
                new LanguageCode { Name = "Swedish", Code = "sv" },
                new LanguageCode { Name = "Tagalog", Code = "tl" },
                new LanguageCode { Name = "Tamil", Code = "ta" },
                new LanguageCode { Name = "Thai", Code = "th" },
                new LanguageCode { Name = "Turkish", Code = "tr" },
                new LanguageCode { Name = "Ukrainian", Code = "uk" },
                new LanguageCode { Name = "Urdu", Code = "ur" },
                new LanguageCode { Name = "Vietnamese", Code = "vi" }
            };


            this.LanguageCodes = new ObservableCollection<LanguageCode>(codes);
        }
    }

    public enum LanguageCodesViewModelType
    {
        Translation,
        Transcription
    }

    public abstract class LanguageCodesViewModel
    {
        public abstract ObservableCollection<LanguageCode> LanguageCodes { get; set; }
    }

    public class LanguageCode
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
