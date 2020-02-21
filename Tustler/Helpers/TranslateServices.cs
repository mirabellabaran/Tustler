using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerAWSLib;

namespace Tustler.Helpers
{
    public static class TranslateServices
    {
        public static async Task<AWSResult<string>> TranslateText(string sourceLanguageCode, string targetLanguageCode, string text, List<string> terminologyNames = null)
        {
            return await Translate.TranslateText(sourceLanguageCode, targetLanguageCode, text, terminologyNames).ConfigureAwait(true);
        }

        public static string ProcessTranslatedResult(NotificationsList notifications, AWSResult<string> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return null;
            }
            else
            {
                return result.Result;
            }
        }
    }
}
