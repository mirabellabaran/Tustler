namespace CloudWeaver.MediaServices

open System
open CloudWeaver.Types
open CloudWeaver.Foundation.Types
open CloudWeaver.Types.CommonUtilities

[<CloudWeaverTaskFunctionModule>]
module public Tasks =

    [<HideFromUI>]
    let GetCodecInfo (queryMode: TaskFunctionQueryMode) (argMap: Map<IRequestIntraModule, IShareIntraModule>) =

        let getCodecInfo argMap =
            let argsRecord = {|
                AVInterface = PatternMatchers.getAVInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                CodecName = PatternMatchers.getCodecName argMap;
            |}

            let avInterface = argsRecord.AVInterface.Value
            let notifications = argsRecord.Notifications.Value
            let codecName = argsRecord.CodecName.Value

            let codecPair = TustlerFFMPEG.MediaServices.GetCodecInfo(avInterface, notifications, codecName)

            seq {
                yield! getNotificationResponse notifications
                if not (isNull codecPair) then
                    yield (AVArgument.SetCodecInfo codecPair).toSetArgumentTaskResponse()
                yield TaskResponse.TaskComplete ("Task complete", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestAVInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestCodecName));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Find a codec by name and return an encoder/decoder pair")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestCodecInfo)))
        | SubTasks -> Seq.empty
        | Invoke ->
            seq {
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! getCodecInfo argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let GetMediaInfo (queryMode: TaskFunctionQueryMode) (argMap: Map<IRequestIntraModule, IShareIntraModule>) =

        let getMediaInfo argMap =
            let argsRecord = {|
                AVInterface = PatternMatchers.getAVInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                OpenFilePath = PatternMatchers.getOpenMediaFilePath argMap;
            |}

            let avInterface = argsRecord.AVInterface.Value
            let notifications = argsRecord.Notifications.Value
            let inputFilePath = argsRecord.OpenFilePath.Value

            let mediaInfo = TustlerFFMPEG.MediaServices.GetMediaInfo(avInterface, notifications, inputFilePath.Path)

            seq {
                yield! getNotificationResponse notifications
                if not (isNull mediaInfo) then
                    yield (AVArgument.SetMediaInfo mediaInfo).toSetArgumentTaskResponse()
                yield TaskResponse.TaskComplete ("Task complete", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestAVInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestOpenMediaFilePath));
            |]

        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Get media information for a media file")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestMediaInfo)))
        | SubTasks -> Seq.empty
        | Invoke ->
            seq {
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! getMediaInfo argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }

    [<HideFromUI>]
    let StripAudioStream (queryMode: TaskFunctionQueryMode) (argMap: Map<IRequestIntraModule, IShareIntraModule>) =

        let stripAudioStream argMap =
            let argsRecord = {|
                AVInterface = PatternMatchers.getAVInterface argMap;
                Notifications = PatternMatchers.getNotifications argMap;
                OpenFilePath = PatternMatchers.getOpenMediaFilePath argMap;
                SaveFilePath = PatternMatchers.getSaveMediaFilePath argMap;
            |}

            let avInterface = argsRecord.AVInterface.Value
            let notifications = argsRecord.Notifications.Value
            let inputFilePath = argsRecord.OpenFilePath.Value
            let outputFilePath = argsRecord.SaveFilePath.Value

            let success = TustlerFFMPEG.MediaServices.Transcode(avInterface, notifications, inputFilePath.Path, outputFilePath.Path)

            seq {
                yield! getNotificationResponse notifications
                if success then
                    // get the mimetype (used for S3 file classification)
                    let mimeType = FileServices.GetMimeType(outputFilePath.Path)
                    let fileReference = FileMediaReference(outputFilePath.Path, mimeType, outputFilePath.Extension)
                    yield (StandardArgument.SetFileMediaReference fileReference).toTaskResponse()
                    yield TaskResponse.TaskComplete ("Audio stripping and transcoding completed successfully", DateTime.Now)
                else
                    yield TaskResponse.TaskComplete ("Audio stripping and transcoding failed", DateTime.Now)
            }

        let inputs = [|
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestAVInterface));
            TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestNotifications));
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestSaveMediaFilePath));
            TaskResponse.RequestArgument (AVRequestIntraModule(AVRequest.RequestOpenMediaFilePath));
            |]


        match queryMode with
        | Description -> Seq.singleton (TaskResponse.TaskDescription "Get the best audio stream from a media file, transcoding if necessary")
        | Inputs -> Seq.ofArray inputs
        | Outputs -> Seq.singleton (TaskResponse.RequestArgument (StandardRequestIntraModule(StandardRequest.RequestFileMediaReference)))
        | SubTasks -> Seq.empty
        | Invoke ->
            seq {
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! stripAudioStream argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }
