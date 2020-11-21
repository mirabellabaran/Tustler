namespace CloudWeaver

open CloudWeaver.Types
open CloudWeaver.AWS
open CloudWeaver.MediaServices

module ArgumentResolver =

    //let private mapResponseToRequest (response: TaskResponse) =
    //    match response with
    //    | TaskResponse.SetArgument arg ->
    //        match arg with
    //        | :? AWSShareIntraModule as awsShareIntraModule ->
    //            match awsShareIntraModule.Argument with
    //            // arguments corresponding to AWSRequest items
    //            | SetAWSInterface _ -> Some(AWSRequestIntraModule(RequestAWSInterface) :> IRequestIntraModule)
    //            | SetBucket _ -> Some(AWSRequestIntraModule(RequestBucket) :> IRequestIntraModule)
    //            | SetBucketsModel _ -> Some(AWSRequestIntraModule(RequestBucketsModel) :> IRequestIntraModule)
    //            | SetS3MediaReference _ -> Some(AWSRequestIntraModule(RequestS3MediaReference) :> IRequestIntraModule)
    //            | SetTranscriptionJobsModel _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobsModel) :> IRequestIntraModule)  // see also AWSDisplayValue.DisplayTranscriptionJobsModel
    //            | SetTranscriptionJobName _ -> Some(AWSRequestIntraModule(RequestTranscriptionJobName) :> IRequestIntraModule)
    //            | SetTranscriptJSON _ -> Some(AWSRequestIntraModule(RequestTranscriptJSON) :> IRequestIntraModule)
    //            | SetTranscriptionDefaultTranscript _ -> Some(AWSRequestIntraModule(RequestTranscriptionDefaultTranscript) :> IRequestIntraModule)
    //            | SetTranscriptURI _ -> Some(AWSRequestIntraModule(RequestTranscriptURI) :> IRequestIntraModule)
    //            | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Transcription -> Some(AWSRequestIntraModule(RequestTranscriptionLanguageCode) :> IRequestIntraModule)
    //            | SetLanguage lang when lang.LanguageDomain = LanguageDomain.Translation -> Some(AWSRequestIntraModule(RequestTranslationLanguageCodeSource) :> IRequestIntraModule)
    //            | SetLanguage _ -> invalidArg "arg" "Unexpected SetLanguage parameters"
    //            | SetTranslationTargetLanguages _ -> Some(AWSRequestIntraModule(RequestTranslationTargetLanguages) :> IRequestIntraModule)
    //            | SetTranscriptionVocabularyName _ -> Some(AWSRequestIntraModule(RequestTranscriptionVocabularyName) :> IRequestIntraModule)
    //            | SetTranslationTerminologyNames _ -> Some(AWSRequestIntraModule(RequestTranslationTerminologyNames) :> IRequestIntraModule)
    //            | SetTranslationSegments _ -> Some(AWSRequestIntraModule(RequestTranslationSegments) :> IRequestIntraModule)
    //            | SetSubtitleFilePath _ -> Some(AWSRequestIntraModule(RequestSubtitleFilePath) :> IRequestIntraModule)
    //        | :? AVShareIntraModule as avShareIntraModule ->
    //            match avShareIntraModule.Argument with
    //            | SetAVInterface _ -> Some(AVRequestIntraModule(RequestAVInterface) :> IRequestIntraModule)
    //            | SetCodecName _ -> Some(AVRequestIntraModule(RequestCodecName) :> IRequestIntraModule)
    //            | SetCodecInfo _ -> Some(AVRequestIntraModule(RequestCodecInfo) :> IRequestIntraModule)
    //            | SetMediaInfo _ -> Some(AVRequestIntraModule(RequestMediaInfo) :> IRequestIntraModule)
    //        | :? StandardShareIntraModule as stdShareIntraModule ->
    //            match stdShareIntraModule.Argument with
    //            | SetNotificationsList _ -> Some(StandardRequestIntraModule(RequestNotifications) :> IRequestIntraModule)
    //            | SetTaskIdentifier _ -> Some(StandardRequestIntraModule(RequestTaskIdentifier) :> IRequestIntraModule)
    //            | SetTaskItem _ -> Some(StandardRequestIntraModule(RequestTaskItem) :> IRequestIntraModule)
    //            | SetWorkingDirectory _ -> Some(StandardRequestIntraModule(RequestWorkingDirectory) :> IRequestIntraModule)
    //            | SetSaveFlags _ -> Some(StandardRequestIntraModule(RequestSaveFlags) :> IRequestIntraModule)
    //            | SetJsonEvents _ -> Some(StandardRequestIntraModule(RequestJsonEvents) :> IRequestIntraModule)
    //            | SetFileMediaReference _ -> Some(StandardRequestIntraModule(RequestFileMediaReference) :> IRequestIntraModule)
    //            | SetLogFormatEvents _ -> Some(StandardRequestIntraModule(RequestLogFormatEvents) :> IRequestIntraModule)
    //            | SetSubTaskInputs _ -> Some(StandardRequestIntraModule(RequestSubTaskInputs) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestOpenJsonFilePath) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "json" -> Some(StandardRequestIntraModule(RequestSaveJsonFilePath) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Open && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestOpenLogFormatFilePath) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Save && path.Extension = "bin" -> Some(StandardRequestIntraModule(RequestSaveLogFormatFilePath) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Open -> Some(AVRequestIntraModule(RequestOpenMediaFilePath) :> IRequestIntraModule)
    //            | SetFilePath path when path.Mode = FilePickerMode.Save -> Some(AVRequestIntraModule(RequestSaveMediaFilePath) :> IRequestIntraModule)
    //            | SetFilePath _ -> invalidArg "arg" "Unexpected SetFilePath parameters"

    //        | _ -> None     // ignore request types from other modules
    //    | _ -> None

    ///// Create a map that contains those request arguments that are currently set
    //let integrateUIRequestArguments (args:seq<TaskResponse>) =
    //    args
    //    |> Seq.fold (fun (map:Map<_,_>) taskResponse ->
    //        let request = mapResponseToRequest taskResponse
    //        let response =
    //            match taskResponse with
    //            | TaskResponse.SetArgument arg -> Some(arg)
    //            | _ -> None
    //        if request.IsSome && response.IsSome then
    //            map.Add (request.Value, response.Value)
    //        else
    //            map
    //    ) (Map.empty)

    /// Create a map that contains those request arguments that are currently set
    let integrateUIRequestArguments (args:seq<TaskResponse>) =
        args
        |> Seq.fold (fun (map:Map<_,_>) taskResponse ->
            let requestResponsePair =
                match taskResponse with
                | TaskResponse.SetArgument (req, arg) -> Some(req, arg)
                | _ -> None
            if requestResponsePair.IsSome then
                let request, response = requestResponsePair.Value
                map.Add (request, response)
            else
                map
        ) (Map.empty)
