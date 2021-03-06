#nullable enable
using Amazon.Translate;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels.Services
{
    public static class TranslateServices
    {
        /// <summary>
        /// Translate a file containing text and output to a new text file
        /// </summary>
        /// <remarks>Newline characters may be introduced at chunk boundaries</remarks>
        public static async Task TranslateLargeText(AmazonWebServiceInterface awsInterface, NotificationsList notifications, Progress<int> progress, bool useArchivedJob, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string>? terminologyNames = null)
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

            await ProcessChunks(awsInterface, chunker, notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames).ConfigureAwait(true);

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
        public static async Task TranslateSentences(AmazonWebServiceInterface awsInterface, NotificationsList notifications, Progress<int> progress, bool useArchivedJob, string jobName, string sourceLanguageCode, string targetLanguageCode, string textFilePath, List<string>? terminologyNames = null)
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

            await ProcessChunks(awsInterface, chunker, notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames).ConfigureAwait(true);

            if (chunker.IsJobComplete)
            {
                // save the translated text
                var filePath = Path.Combine(ApplicationSettings.FileCachePath, jobName);
                filePath = Path.ChangeExtension(filePath, "txt");
                File.WriteAllLines(filePath, chunker.AllSentences);
                notifications.ShowMessage("Translation job completed", $"{jobName} has been saved to file {filePath}.");
            }
        }

        // translate one chunk and process the result
        public static async Task<(bool IsErrorState, bool RecoverableError)> TranslateProcessor(AmazonWebServiceInterface awsInterface, SentenceChunker chunker, NotificationsList notifications, Progress<int> progress, string jobName, string sourceLanguageCode, string targetLanguageCode, List<string>? terminologyNames, int index, string text)
        {
            var translationResult = await awsInterface.Translate.TranslateText(sourceLanguageCode, targetLanguageCode, text, terminologyNames).ConfigureAwait(true);
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

                if (progress is object)
                    (progress as IProgress<int>).Report(index * 100 / chunker.NumChunks);

                return (false, false);
            }
        }

        private static async Task ProcessChunks(AmazonWebServiceInterface awsInterface, SentenceChunker chunker, NotificationsList notifications, Progress<int> progress, string jobName, string sourceLanguageCode, string targetLanguageCode, List<string>? terminologyNames)
        {
            Task<(bool IsErrorState, bool RecoverableError)> Translator(int index, string text)
            {
                return TranslateProcessor(awsInterface, chunker, notifications, progress, jobName, sourceLanguageCode, targetLanguageCode, terminologyNames, index, text);
            }

            await chunker.ProcessChunks(Translator).ConfigureAwait(true);
        }

        public static async Task<AWSResult<string>> TranslateText(AmazonWebServiceInterface awsInterface, string sourceLanguageCode, string targetLanguageCode, string text, List<string>? terminologyNames = null)
        {
            return await awsInterface.Translate.TranslateText(sourceLanguageCode, targetLanguageCode, text, terminologyNames).ConfigureAwait(true);
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
            return GetArchivedJobImpl(filePath);
        }

        public static string? GetArchivedJob(string folder, string jobName)
        {
            var filePath = Path.Combine(folder, jobName);
            return GetArchivedJobImpl(filePath);
        }

        private static string? GetArchivedJobImpl(string filePath)
        {
            filePath = Path.ChangeExtension(filePath, "zip");
            if (File.Exists(filePath))
                return filePath;
            else
                return null;
        }
    }
}
