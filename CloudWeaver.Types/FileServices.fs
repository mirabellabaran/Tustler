namespace CloudWeaver.Types


/// <summary>
/// Modified from https://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature/9435701#9435701
/// </summary>
/// <see cref="Frederick Samson"/>
type FileServices =

    /// Attempts to get the mimetype of a file using one of three methods in a specified order.
    /// Returns null if the mimetype can not be deduced.
    /// Note that the second method requires that the file exists on disk.
    /// Modified from https://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature/9435701#9435701
    /// cref="Frederick Samson"
    static member GetMimeType(filePath: string) : string =
        
        let methods = [|
            TustlerServicesLib.MimeTypeDictionary.GetMimeTypeFromList;
            TustlerWinPlatformLib.NativeMethods.GetMimeTypeFromFile;
            TustlerWinPlatformLib.RegistryServices.GetMimeTypeFromRegistry;
        |]

        let mimeType =
            methods
            |> Seq.tryPick (fun func ->
                let result = func(filePath)
                if isNull result then
                    None
                else
                    Some(result)
            )

        if mimeType.IsSome then mimeType.Value else null
