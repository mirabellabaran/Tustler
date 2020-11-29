namespace CloudWeaver.AWS

open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open CloudWeaver.Types

module public Converters =

    /// Json converter for LanguageCodeDomain (contains an enum-like union type)
    type LanguageCodeDomainConverter() =
        inherit JsonConverter<LanguageCodeDomain>()

        override this.Read(reader, _typeToConvert, options) =

            let standardConverter = Converters.getStandardConverter options.Converters
            let dict = standardConverter.Read(&reader, typeof<LanguageCodeDomain>, options)

            if (dict.ContainsKey "LanguageDomain") && (dict.ContainsKey "Name") && (dict.ContainsKey "Code") then
                let languageDomain =
                    let strValue = JsonSerializedValue.getString (dict.["LanguageDomain"])
                    match strValue with
                    | "Transcription" -> LanguageDomain.Transcription
                    | "Translation" -> LanguageDomain.Translation
                    | _ -> invalidArg "LanguageDomain" "Value not set"
                let name = JsonSerializedValue.getString (dict.["Name"])
                let code = JsonSerializedValue.getString (dict.["Code"])
                LanguageCodeDomain(languageDomain, name, code)
            else                
                raise (JsonException("Error parsing LanguageCodeDomain"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            writer.WriteString("LanguageDomain", instance.LanguageDomain.ToString())
            writer.WriteString("Name", instance.Name)
            writer.WriteString("Code", instance.Code)
            writer.WriteEndObject()

    /// Json converter for VocabularyName
    type VocabularyNameConverter() =
        inherit JsonConverter<VocabularyName>()

        override this.Read(reader, _typeToConvert, options) =

            let standardConverter = Converters.getStandardConverter options.Converters
            let dict = standardConverter.Read(&reader, typeof<VocabularyName>, options)

            if (dict.ContainsKey "VocabularyName") then
                let name = JsonSerializedValue.getString (dict.["VocabularyName"])
                VocabularyName(name)
            else
                raise (JsonException("Error parsing VocabularyName"))


        override this.Write(writer, instance, _options) =
            writer.WriteStartObject()
            if instance.VocabularyName.IsSome then
                writer.WriteString("VocabularyName", instance.VocabularyName.Value)
            else
                writer.WriteNull("VocabularyName")
            writer.WriteEndObject()
