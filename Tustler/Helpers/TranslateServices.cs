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
using TustlerInterfaces;
using TustlerServicesLib;
using TustlerWinPlatformLib;

namespace Tustler.Helpers
{
    public static class TranslateServices
    {
        /// <summary>
        /// Translate a file containing text and output to a new text file
        /// </summary>
        /// <remarks>Newline characters may be introduced at chunk boundaries</remarks>
        public static async Task TranslateLargeText(NotificationsList notifications, Progress<int> progress, bool useArchivedJob, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string>? terminologyNames = null)
        {
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
                chunker = TustlerServicesLib.SentenceChunker.FromFile(textFilePath);
            }

            await ProcessChunks(chunker, notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames).ConfigureAwait(true);

            if (chunker.IsJobComplete)
            {
                // save the translated text
                var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
                filePath = Path.ChangeExtension(filePath, "txt");
                File.WriteAllText(filePath, chunker.CompletedTranslation);
                notifications.ShowMessage("Translation job completed", $"{jobName} has been saved to file {filePath}.");
            }
        }

        /// <summary>
        /// Translate a file containing one complete sentence per line and output to a new file containing one complete sentence per line
        /// </summary>
        public static async Task TranslateSentences(NotificationsList notifications, Progress<int> progress, bool useArchivedJob, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string>? terminologyNames = null)
        {
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
                // the text file is assumed to contain a sequence of sentences, with one complete sentence per line
                var sentences = File.ReadAllLines(textFilePath);
                chunker = new TustlerServicesLib.SentenceChunker(sentences);
            }

            await ProcessChunks(chunker, notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames).ConfigureAwait(true);

            if (chunker.IsJobComplete)
            {
                // save the translated text
                var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
                filePath = Path.ChangeExtension(filePath, "txt");
                File.WriteAllLines(filePath, chunker.AllSentences);
                notifications.ShowMessage("Translation job completed", $"{jobName} has been saved to file {filePath}.");
            }
        }

        private static async Task ProcessChunks(SentenceChunker chunker, NotificationsList notifications, Progress<int> progress, string jobName, string sourceLanguageCode, string targetLanguageCode, List<string>? terminologyNames)
        {
            async Task<(bool IsErrorState, bool RecoverableError)> Translator(int index, string text)
            {
                var translationResult = await TranslateText(sourceLanguageCode, targetLanguageCode, text, terminologyNames).ConfigureAwait(true);
                if (translationResult.IsError)
                {
                    var ex = translationResult.Exception;
                    if (
                        !(ex.InnerException is null) &&
                        (ex.InnerException is AmazonTranslateException) &&
                        (ex.InnerException as AmazonTranslateException)?.StatusCode == HttpStatusCode.TooManyRequests
                        )
                    {
                        return (true, true);
                    }
                    else
                    {
                        notifications.HandleError(translationResult);
                        chunker.ArchiveChunks(jobName, ApplicationSettings.FileCachePath);
                        notifications.ShowMessage("The failed job can be restarted", $"{jobName} has been archived and can be restarted at another time using the same job name.");
                        return (true, false);
                    }
                }
                else
                {
                    var translatedText = translationResult.Result;
                    chunker.Update(index, translatedText);

                    (progress as IProgress<int>).Report(index * 100 / chunker.NumChunks);

                    return (false, false);
                }
            }

            await chunker.ProcessChunks(Translator).ConfigureAwait(true);
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
