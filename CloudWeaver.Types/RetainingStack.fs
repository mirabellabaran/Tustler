namespace CloudWeaver.Types

open System.Collections
open System.Collections.Generic
open System.Collections.Immutable
open System.Text.Json
open System

type IShareIterationArgument =
    abstract member Serialize : Utf8JsonWriter -> unit
    abstract member ToString: unit -> string

/// Defines a type whose internal items can be consumed until none are left
type IConsumable =
    inherit IEnumerable<IShareIterationArgument>
    abstract member Identifier: Guid with get
    abstract member Total : int with get
    abstract member Remaining : int with get
    abstract member Current : IShareIterationArgument option with get
    abstract member Consume : unit -> unit   // consume the current item
    abstract member Reset : unit -> unit
    abstract member ToString: unit -> string

/// A stack-like object that supports Stack semantics but retains all data
/// A retaining stack can be consumed but the original contents are always retrievable
[<AbstractClass>]
type RetainingStack(uid: Guid, items: IEnumerable<IShareIterationArgument>) =

    do
        if (isNull items) then invalidArg "items" "Expecting a non-null value for items"

    let _array = items.ToImmutableArray()
    let _stack = Stack<IShareIterationArgument>(Seq.rev items)  // reversed so that calling Pop() removes items in _array order

    /// the name of the module that contains the iteration argument type
    abstract ModuleName : string with get

    /// Get the unique identifier for this instance
    member this.Identifier with get() = uid

    /// Get the total count
    member this.Total with get() = _array.Length

    /// Get a count of the remaining (consumable) items
    member this.Remaining with get() = _stack.Count

    /// Get the current item (head of stack) without consuming it
    member this.Current with get() =
        match _stack.TryPeek() with
        | true, item -> Some(item)
        | _ -> None

    /// Consume the current item (head of stack) and return it
    member this.Pop() = _stack.Pop()

    /// Consume the current item (head of stack)
    member this.Consume() = _stack.Pop() |> ignore

    /// Refill the stack with the same items used at construction time
    member this.Reset() =

        _stack.Clear()

        _array
        |> Seq.rev
        |> Seq.iter (fun item -> _stack.Push(item))

    override this.ToString() = sprintf "RetainingStack of IShareIterationArgument: total=%d; remaining=%d" (_array.Length) (_stack.Count)

    interface IConsumable with

        member this.Identifier with get() = this.Identifier

        member this.Total: int = this.Total

        member this.Remaining: int = this.Remaining

        member this.Current: IShareIterationArgument option = this.Current

        member this.Consume(): unit = this.Pop() |> ignore

        member this.Reset(): unit = this.Reset()

        member this.ToString(): string = sprintf "IConsumable: Total %d, Remaining %d, Id %s" this.Total this.Remaining (this.Identifier.ToString())

    interface IEnumerable<IShareIterationArgument> with

        member this.GetEnumerator(): System.Collections.IEnumerator = 
            (_array :> IEnumerable).GetEnumerator()

        member this.GetEnumerator(): IEnumerator<IShareIterationArgument> = 
            (_array :> IEnumerable<IShareIterationArgument>).GetEnumerator()
