using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerAWSLib;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public static class TranslateServices
    {
        public static async Task TranslateLargeText(NotificationsList notifications, Progress<int> progress, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string> terminologyNames = null)
        {
            var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
            filePath = Path.ChangeExtension(filePath, "txt");
            SentenceChunker chunker;
            if (File.Exists(filePath))
            {
                chunker = SentenceChunker.DeArchiveChunks(jobName, ApplicationSettings.FileCachePath);
            }
            else
            {
                chunker = new TustlerServicesLib.SentenceChunker(textFilePath);
            }

            var count = 0;
            foreach (var kvp in chunker.Chunks)
            {
                if (!chunker.IsChunkTranslated(kvp.Key))
                {
                    var translationResult = await TranslateText(sourceLanguageCode, targetLanguageCode, kvp.Value, terminologyNames).ConfigureAwait(true);
                    if (translationResult.IsError)
                    {
                        notifications.HandleError(translationResult);
                        chunker.ArchiveChunks(jobName, ApplicationSettings.FileCachePath);
                        notifications.ShowMessage("The failed job can be restarted", $"{jobName} has been archived and can be restarted at another time using the same job name.");
                    }
                    else
                    {
                        var translatedText = translationResult.Result;
                        chunker.Update(kvp.Key, translatedText);
                    }
                }
                (progress as IProgress<int>).Report(++count * 100 / chunker.NumChunks);
            }

            if (chunker.IsJobComplete)
            {
                // save the translated text
                File.WriteAllText(filePath, chunker.CompletedTranslation);
                notifications.ShowMessage("Translation job completed", $"{jobName} has been saved to file {filePath}.");
            }
        }

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
