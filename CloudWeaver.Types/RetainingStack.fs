namespace CloudWeaver.Types

open System.Collections
open System.Collections.Generic
open System.Collections.Immutable
open System.Text.Json

type IShareIterationArgument =
    abstract member Serialize : Utf8JsonWriter -> unit
    abstract member ToString: unit -> string

/// Defines a type whose internal items can be consumed until none are left
type IConsumable =
    inherit IEnumerable<IShareIterationArgument>
    abstract member Total : int with get
    abstract member Remaining : int with get
    abstract member Consume : unit -> IShareIterationArgument   // consume the current item and return the value
    abstract member Reset : unit -> unit

/// A stack-like object that supports Stack semantics but retains all data
/// A retaining stack can be consumed but the original contents are always retrievable
type RetainingStack(items: IEnumerable<IShareIterationArgument>) =

    do
        if (isNull items) then invalidArg "items" "Expecting a non-null value for items"

    let _array = items.ToImmutableArray()
    let _stack = Stack<IShareIterationArgument>(Seq.rev items)  // reversed so that calling Pop() removes items in _array order

    ///// Create a RetainingStack from an IConsumable
    //static member CreateFrom(consumable: IConsumable) =
    
    //    let stack = new RetainingStack(consumable)
    //    let count = consumable.Total - consumable.Remaining
    //    for x in 1..count do
    //        stack.Pop() |> ignore

    //    stack

    /// Get a count of the remaining (consumable) items
    member this.Remaining with get() = _stack.Count

    /// Get the total count
    member this.Total with get() = _array.Length

    /// Get the current item (head of stack) without consuming it
    member this.Current with get() = _stack.Peek()

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

        member this.Total: int = this.Total

        member this.Remaining: int = this.Remaining

        member this.Consume(): IShareIterationArgument = this.Pop()

        member this.Reset(): unit = this.Reset()

    interface IEnumerable<IShareIterationArgument> with

        member this.GetEnumerator(): System.Collections.IEnumerator = 
            (_array :> IEnumerable).GetEnumerator()

        member this.GetEnumerator(): IEnumerator<IShareIterationArgument> = 
            (_array :> IEnumerable<IShareIterationArgument>).GetEnumerator()

/// A wrapper for serialization/deserialization: retains the essential property values needed for reconstructing a RetainingStack
type RetainingStackSerializationWrapper(total, remaining, items: IEnumerable<IShareIterationArgument>) =
    
    /// Default constructor for deserialization
    new() = RetainingStackSerializationWrapper(0, 0, Seq.empty)

    new(consumable: IConsumable) = RetainingStackSerializationWrapper(consumable.Total, consumable.Remaining, consumable)
        
    member val Total = total with get, set

    member val Remaining = remaining with get, set

    member val Items: IEnumerable<IShareIterationArgument> = items with get, set

    member this.Unwrap(): RetainingStack =
        let stack = new RetainingStack(this.Items)
        let count = this.Total - this.Remaining
        for x in 1..count do
            stack.Pop() |> ignore

        stack
