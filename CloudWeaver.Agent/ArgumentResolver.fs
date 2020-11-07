namespace CloudWeaver

open CloudWeaver.Types
open CloudWeaver.AWS
open CloudWeaver.Foundation.Types

module ArgumentResolver =

    let private mapResponseToRequest (response: TaskResponse) =
        match response with
        | TaskResponse.SetArgument arg ->
            match arg with
            | :? AWSShareIntraModule as awsShareIntraModule ->
                match awsShareIntraModule.Argument with
                // arguments corresponding to AWSRequest items
                | SetAWSInterface _ -> Some(AWSRequestIntraModule(RequestAWSInterface) :> IRequestIntraModule)
                | SetBucket _ -> Some(AWSRequestIntraModule(RequestBucket) :> IRequestIntraModule)
                | SetBucketsModel _ -> Some(AWSRequestIntraModule(RequestBucketsModel) :> IRequestIntraModule)
                | SetS3MediaReference _ -> Some(AWSRequestIntraModule(RequestS3MediaReference) :> IRequestIntraModule)
                | SetTranscriptionJobsModel _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobsModel) :> IRequestIntraModule)  // see also AWSDisplayValue.DisplayTranscriptionJobsModel
                | SetTranscriptionJobName _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobName) :> IRequestIntraModule)
                | SetTranscriptJSON _ -> Some(AWSRequestIntraModule(RequestTranscriptJSON) :> IRequestIntraModule)
                | SetTranscriptionDefaultTranscript _ -> Some(AWSRequestIntraModule(RequestTranscriptionDefaultTranscript) :> IRequestIntraModule)
                | SetTranscriptURI _ -> Some(AWSRequestIntraModule(RequestTranscriptURI) :> IRequestIntraModule)
                | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Transcription -> Some(AWSRequestIntraModule(RequestTranscriptionLanguageCode) :> IRequestIntraModule)
                | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Translation -> Some(AWSRequestIntraModule(RequestTranslationLanguageCodeSource) :> IRequestIntraModule)
                | SetLanguage _ -> invalidArg "arg" "Unexpected SetLanguage parameters"
                | SetTranslationTargetLanguages _ -> Some(AWSRequestIntraModule(RequestTranslationTargetLanguages) :> IRequestIntraModule)
                | SetTranscriptionVocabularyName _ -> Some(AWSRequestIntraModule(RequestTranscriptionVocabularyName) :> IRequestIntraModule)
                | SetTranslationTerminologyNames _ -> Some(AWSRequestIntraModule(RequestTranslationTerminologyNames) :> IRequestIntraModule)
                | SetTranslationSegments _ -> Some(AWSRequestIntraModule(RequestTranslationSegments) :> IRequestIntraModule)
                | SetSubtitleFilePath _ -> Some(AWSRequestIntraModule(RequestSubtitleFilePath) :> IRequestIntraModule)
            | :? StandardShareIntraModule as stdShareIntraModule ->
                match stdShareIntraModule.Argument with
                | SetNotificationsList _ -> Some(StandardRequestIntraModule(RequestNotifications) :> IRequestIntraModule)
                | SetTaskIdentifier _ -> Some(StandardRequestIntraModule(RequestTaskIdentifier) :> IRequestIntraModule)
                | SetTaskItem _ -> Some(StandardRequestIntraModule(RequestTaskItem) :> IRequestIntraModule)
                | SetWorkingDirectory _ -> Some(StandardRequestIntraModule(RequestWorkingDirectory) :> IRequestIntraModule)
                | SetSaveFlags _ -> Some(StandardRequestIntraModule(RequestSaveFlags) :> IRequestIntraModule)
                | SetJsonEvents _ -> Some(StandardRequestIntraModule(RequestJsonEvents) :> IRequestIntraModule)
                | SetFileMediaReference _ -> Some(StandardRequestIntraModule(RequestFileMediaReference) :> IRequestIntraModule)
                | SetLogFormatEvents _ -> Some(StandardRequestIntraModule(RequestLogFormatEvents) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestOpenJsonFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestSaveJsonFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestOpenLogFormatFilePath) :> IRequestIntraModule)
                | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestSaveLogFormatFilePath) :> IRequestIntraModule)
                | SetFilePath _ -> invalidArg "arg" "Unexpected SetFilePath parameters"

            | _ -> None     // ignore request types from other modules
        | _ -> None

    /// Create a map that contains those request arguments that are currently set
    let integrateUIRequestArguments (args:InfiniteList<MaybeResponse>) =
        args
        |> Seq.takeWhile (fun mr -> mr.IsSet)
        |> Seq.fold (fun (map:Map<_,_>) mr ->
            let request = mapResponseToRequest mr.Value
            let response =
                match mr.Value with
                | TaskResponse.SetArgument arg -> Some(arg)
                | _ -> None
            if request.IsSome && response.IsSome then
                map.Add (request.Value, response.Value)
            else
                map
        ) (Map.empty)
