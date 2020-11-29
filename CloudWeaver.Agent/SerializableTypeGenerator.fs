namespace CloudWeaver

open System.Text.Json
open System.IO
open CloudWeaver.Types
open System.Collections.Generic
open System
open TustlerModels
open CloudWeaver.Foundation.Types

type SerializableTypeGenerator() =

    static member private CreateJson(items: Dictionary<string, string option>) =

        use ms = new System.IO.MemoryStream()
        use writer = new Utf8JsonWriter(ms, new JsonWriterOptions( Indented = false ));
        writer.WriteStartObject()
        // write Json value or Json null literal
        items
        |> Seq.iter (fun kvp -> if kvp.Value.IsSome then writer.WriteString(kvp.Key, kvp.Value.Value) else writer.WriteNull(kvp.Key))
        writer.WriteEndObject()
        writer.Flush()

        ms.ToArray()
        
    /// Create a serialized representation of an enumerable of Standard module TaskItems
    static member CreateTaskItems(tasks: IEnumerable<TaskFunctionSpecifier>) : byte[] =

        use ms = new System.IO.MemoryStream()
        use writer = new Utf8JsonWriter(ms, new JsonWriterOptions( Indented = false ));
        writer.WriteStartArray()
        tasks
        |> Seq.iter (fun task ->
            writer.WriteStartObject()
            writer.WriteString("ModuleName", task.ModuleName)
            writer.WriteString("TaskName", task.TaskName)
            writer.WriteString("Description", System.String.Empty)
            writer.WriteEndObject()
        )
        writer.WriteEndArray()
        writer.Flush()

        ms.ToArray()

    /// Create a serialized representation of a Standard module FilePath
    static member CreateFilePath(fileInfo: FileInfo, fileExtension: string, pickerMode: FilePickerMode) =

        let items = [|
            KeyValuePair<string, string option>("Path", Some(fileInfo.FullName))
            KeyValuePair<string, string option>("Extension", Some(fileExtension))
            KeyValuePair<string, string option>("Mode", Some(pickerMode.ToString()))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of a Standard module FileMediaReference
    static member CreateFileMediaReference(filePath: string, mimeType: string, extension: string) =

        let items = [|
            KeyValuePair<string, string option>("FilePath", Some(filePath))
            KeyValuePair<string, string option>("MimeType", Some(mimeType))
            KeyValuePair<string, string option>("Extension", Some(extension))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))



    /// Create a serialized representation of AWS module TranscriptionDefaultTranscript
    static member CreateTranscriptionDefaultTranscript(defaultTranscript: string) = JsonSerializer.SerializeToUtf8Bytes(defaultTranscript)

    ///// Create a serialized representation of AWS module TranscriptionVocabularyName
    //static member CreateTranscriptionVocabularyName(name: string, serializerOptions) =
    
    //    let vocabularyName = VocabularyName(name)

    //    JsonSerializer.SerializeToUtf8Bytes(vocabularyName, serializerOptions)

    /// Create a serialized representation of an AWS module S3 Bucket
    static member CreateBucket(name: string, creationDate: DateTime) =

        let items = [|
            KeyValuePair<string, string option>("Name", Some(name))
            KeyValuePair<string, string option>("CreationDate", Some(creationDate.ToString("o")))   // use "o" format for round-tripping the datetime
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    ///// Create a serialized representation of an AWS module S3 Bucket Item
    //static member CreateBucketItem(key: string, bucketName: string, size: int64, lastModified: DateTime, owner: string, mimeType: string, extension: string) =

    //    let items = [|
    //        KeyValuePair<string, string option>("Key", Some(key))
    //        KeyValuePair<string, string option>("BucketName", Some(bucketName))
    //        KeyValuePair<string, string option>("Size", Some(size.ToString()))
    //        KeyValuePair<string, string option>("LastModified", Some(lastModified.ToString("o")))   // use "o" format for round-tripping the datetime
    //        KeyValuePair<string, string option>("Owner", Some(owner))
    //        KeyValuePair<string, string option>("MimeType", Some(mimeType))
    //        KeyValuePair<string, string option>("Extension", Some(extension))
    //    |]

    //    SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of an AWS module S3MediaReference
    static member CreateS3MediaReference(bucketName: string, key: string, mimeType: string, extension: string) =

        let items = [|
            KeyValuePair<string, string option>("BucketName", Some(bucketName))
            KeyValuePair<string, string option>("Key", Some(key))
            KeyValuePair<string, string option>("MimeType", Some(mimeType))
            KeyValuePair<string, string option>("Extension", Some(extension))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    ///// Create a serialized representation of an AWS module LanguageCodeDomain
    //static member CreateLanguageCodeDomain(languageDomain: LanguageCodeDomain) =

    //    let items = [|
    //        KeyValuePair<string, string option>("LanguageDomain", Some(languageDomain.ToString()))
    //        KeyValuePair<string, string option>("Name", Some(name))
    //        KeyValuePair<string, string option>("Code", Some(code))
    //    |]

    //    SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of an AWS module VocabularyName
    static member CreateVocabularyName(vocabularyName: string) =

        let items = [|
            KeyValuePair<string, string option>("VocabularyName", if isNull vocabularyName then None else Some(vocabularyName))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of AWS module TranslationTargetLanguageCodes
    static member CreateTranslationTargetLanguageCodes(iterationArgumentTypeName:string, languages: IEnumerable<IShareIterationArgument>, serializerOptions) =

        let typeResolver = TypeResolver.Create() |> Async.AwaitTask |> Async.RunSynchronously

        let stack = typeResolver.CreateRetainingStack(iterationArgumentTypeName, Guid.NewGuid(), languages)
        JsonSerializer.SerializeToUtf8Bytes<RetainingStack>(stack, serializerOptions)

    /// Create a serialized representation of AWS module TranslationTerminologyNames
    static member CreateTranslationTerminologyNames(terminologyNames: IEnumerable<string>) =

        JsonSerializer.SerializeToUtf8Bytes(terminologyNames)
