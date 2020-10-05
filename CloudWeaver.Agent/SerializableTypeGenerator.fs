namespace CloudWeaver

open System.Text.Json
open System.IO
open CloudWeaver.Types
open CloudWeaver.AWS
open System.Collections.Generic

type SerializableTypeGenerator() =

    static member private CreateJson(items: Dictionary<string, string option>) =

        use ms = new System.IO.MemoryStream()
        use writer = new Utf8JsonWriter(ms, new JsonWriterOptions( Indented = false ));
        writer.WriteStartObject()
        // write Json value or Json null literal
        items
        |> Seq.iter (fun kvp -> if kvp.Value.IsSome then writer.WriteString(kvp.Key, kvp.Value.Value) else writer.WriteNull(kvp.Key))
        writer.Flush()

        ms.ToArray()
        
    /// Create a serialized representation of a Standard module TaskItem
    static member CreateTaskItem(moduleName: string, taskName: string) : byte[] =

        let items = [|
            KeyValuePair<string, string option>("ModuleName", Some(moduleName))
            KeyValuePair<string, string option>("TaskName", Some(taskName))
            KeyValuePair<string, string option>("Description", Some(System.String.Empty))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of a Standard module FilePath
    static member CreateFilePath(fileInfo: FileInfo, fileExtension: string, pickerMode: FilePickerMode) =

        let items = [|
            KeyValuePair<string, string option>("Path", Some(fileInfo.FullName))
            KeyValuePair<string, string option>("Extension", Some(fileExtension))
            KeyValuePair<string, string option>("Mode", Some(pickerMode.ToString()))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    static member CreateFileMediaReference(filePath: string, mimeType: string, extension: string) =

        let items = [|
            KeyValuePair<string, string option>("FilePath", Some(filePath))
            KeyValuePair<string, string option>("MimeType", Some(mimeType))
            KeyValuePair<string, string option>("Extension", Some(extension))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of an AWS module LanguageCodeDomain
    static member CreateLanguageCodeDomain(languageDomain: LanguageDomain, name: string, code: string) =

        let items = [|
            KeyValuePair<string, string option>("LanguageDomain", Some(languageDomain.ToString()))
            KeyValuePair<string, string option>("Name", Some(name))
            KeyValuePair<string, string option>("Code", Some(code))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))

    /// Create a serialized representation of an AWS module VocabularyName
    static member CreateVocabularyName(vocabularyName: string) =

        let items = [|
            KeyValuePair<string, string option>("VocabularyName", if isNull vocabularyName then None else Some(vocabularyName))
        |]

        SerializableTypeGenerator.CreateJson(new Dictionary<_,_>(items))
