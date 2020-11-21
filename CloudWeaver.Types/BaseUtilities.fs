namespace CloudWeaver.Types

open Microsoft.FSharp.Reflection
open System

module BaseUtilities =

    // extract the name of a discrimated union field as a string
    let toString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None

    // return the module name and associated request argument from a stringified IRequestIntraModule
    let deStringifyRequest (request: string) =
        let span = ReadOnlySpan<char>(request.ToCharArray())
        let index = span.IndexOf('.')
        span.Slice(0, index).ToString(), span.Slice(index + 1).ToString()
