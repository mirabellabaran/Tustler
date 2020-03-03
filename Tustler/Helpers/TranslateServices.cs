#nullable enable
using Amazon.Translate;
using Amazon.Translate.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerAWSLib;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public static class TranslateServices
    {
        public static async Task TranslateLargeText(NotificationsList notifications, Progress<int> progress, bool useArchivedJob, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string>? terminologyNames = null)
        {
            // compute an exponential backoff delay when a Rate exceeded message is received
            long GetDelay(int retryNum, long minSleepMilliseconds, long maxSleepMilliseconds)
            {
                retryNum = Math.Max(0, retryNum);
                long currentSleepMillis = (long)(minSleepMilliseconds * Math.Pow(2, retryNum));
                return Math.Min(currentSleepMillis, maxSleepMilliseconds);
            }

            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            SentenceChunker chunker;
            if (useArchivedJob && !(GetArchivedJob(jobName) is null))
            {
                chunker = SentenceChunker.DeArchiveChunks(jobName, ApplicationSettings.FileCachePath);
            }
            else
            {
                chunker = TustlerServicesLib.SentenceChunker.FromFile(textFilePath, 300);
            }

            var count = 0;
            foreach (var kvp in chunker.Chunks)
            {
                if (!chunker.IsChunkTranslated(kvp.Key))
                {
                    var translationResult = await TranslateText(sourceLanguageCode, targetLanguageCode, kvp.Value, terminologyNames).ConfigureAwait(true);
                    if (translationResult.IsError)
                    {
                        if ((translationResult.Exception.InnerException is AmazonTranslateException) &&
                            (translationResult.Exception.InnerException as AmazonTranslateException).StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            var newDelay = GetDelay(count + 1, 10, 5000);
                            await Task.Delay((int)newDelay).ConfigureAwait(false);
                        }
                        else
                        {
                            notifications.HandleError(translationResult);
                            chunker.ArchiveChunks(jobName, ApplicationSettings.FileCachePath);
                            notifications.ShowMessage("The failed job can be restarted", $"{jobName} has been archived and can be restarted at another time using the same job name.");
                            break;
                        }
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
                var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
                filePath = Path.ChangeExtension(filePath, "txt");
                File.WriteAllText(filePath, chunker.CompletedTranslation);
                notifications.ShowMessage("Translation job completed", $"{jobName} has been saved to file {filePath}.");
            }
        }

        public static async Task<AWSResult<string>> TranslateText(string sourceLanguageCode, string targetLanguageCode, string text, List<string>? terminologyNames = null)
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

        public static string? GetArchivedJob(string jobName)
        {
            var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
            filePath = Path.ChangeExtension(filePath, "zip");
            if (File.Exists(filePath))
                return filePath;
            else
                return null;
        }
    }
}
