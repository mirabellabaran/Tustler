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
        | Invoke ->
            seq {
                let unresolvedRequests = getUnResolvedRequests argMap inputs

                if unresolvedRequests.Length = 0 then
                    yield! getCodecInfo argMap
                else
                    yield! resolveByRequest unresolvedRequests
            }
