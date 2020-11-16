namespace CloudWeaver.Types

open CloudWeaver.Foundation.Types
open System.Collections.Generic

module CommonUtilities =

    /// Get any notifications generated from the last AWS call (errors or informational messages)
    let getNotificationResponse (notifications: NotificationsList) =
        Seq.map (fun note -> TaskResponse.Notification note) notifications.Notifications

    /// Get all requests that are not yet set
    let getUnResolvedRequests (argMap: Map<IRequestIntraModule, IShareIntraModule>) (required: TaskResponse []) =
        required
        |> Seq.map (fun response ->
            match response with
            | TaskResponse.RequestArgument arg -> arg
            | _ -> invalidArg "response" "Expected RequestArgument in getUnResolvedRequests when checking input arguments"
        )
        |> Seq.filter (fun request -> not (argMap.ContainsKey request))
        |> Seq.map (fun request -> TaskResponse.RequestArgument request)
        |> Seq.toArray

    // Get the first unresolved request and send to the UI to resolve the value
    let resolveByRequest (unresolvedRequests: TaskResponse []) =

        let requestStack = Stack(unresolvedRequests)

        if requestStack.Count > 0 then
            Seq.singleton (requestStack.Pop())
        else
            Seq.empty

    // Find the SetSubTaskInputs argument and map the requests to an array of responses
    let getRootTaskInputRequests argMap =
        let subTaskInputs = PatternMatchers.getSubTaskInputs argMap
        subTaskInputs.Value
        |> Seq.map (fun request -> TaskResponse.RequestArgument request)
        |> Seq.toArray
