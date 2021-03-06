﻿namespace FSharpPlus

open System

/// Additional operations on Option
module Option =
    let apply f x =
        match (f, x) with 
        | Some f, Some x -> Some (f x) 
        | _              -> None


/// Additional operations on Result<'Ok,'Error>
[<RequireQualifiedAccess>]
module Result =
    let result x = Ok x
    let throw  x = Error x    
    let apply f x = match (f, x) with (Ok a, Ok b)     -> Ok (a b) | Error e, _ | _, Error e -> Error e: Result<'b, 'e>
    let map   f                   = function Ok v      -> Ok (f v) | Error e                 -> Error e
    let flatten                   = function Ok (Ok v) -> Ok v     | Ok (Error e) | Error e  -> Error e
    let bind (f:'t -> _)          = function Ok v      -> f v      | Error e                 -> Error e: Result<'v,'e>
    let inline catch (f:'t -> _)  = function Ok v      -> Ok v     | Error e                 -> f e    : Result<'v,'e>
    let inline either f g         = function Ok v      -> f v      | Error e                 -> g e


/// Additional operations on Choice
[<RequireQualifiedAccess>]
module Choice =
    let result x = Choice1Of2 x
    let throw  x = Choice2Of2 x    
    let apply f x = match (f, x) with Choice1Of2 a, Choice1Of2 b              -> Choice1Of2 (a b) | Choice2Of2 e, _ | _, Choice2Of2 e        -> Choice2Of2 e: Choice<'b,'e>
    let map   f                          = function Choice1Of2 v              -> Choice1Of2 (f v) | Choice2Of2 e                             -> Choice2Of2 e
    let flatten                          = function Choice1Of2 (Choice1Of2 v) -> Choice1Of2 v     | Choice1Of2 (Choice2Of2 e) | Choice2Of2 e -> Choice2Of2 e
    let bind (f:'t -> _)                 = function Choice1Of2 v              -> f v              | Choice2Of2 e                             -> Choice2Of2 e: Choice<'v,'e>
    let inline catch (f:'t -> _)         = function Choice1Of2 v              -> Choice1Of2 v     | Choice2Of2 e                             -> f e         : Choice<'v,'e>
    let inline either f g                = function Choice2Of2 v              -> f v              | Choice1Of2 e                             -> g e


/// Additional operations on Seq
module Seq =
    let bind (f:'a->seq<'b>) x = Seq.collect f x
    let apply f x = bind (fun f -> Seq.map ((<|) f) x) f
    let foldBack f x z = Array.foldBack f (Seq.toArray x) z

    let chunkBy projection (source : _ seq) = seq {
        use e = source.GetEnumerator()
        if e.MoveNext() then
            let g = ref (projection e.Current)
            let members = ref (ResizeArray())
            (!members).Add(e.Current)
            while (e.MoveNext()) do
                let key = projection e.Current
                if !g = key then (!members).Add(e.Current)
                else
                    yield (!g, !members)
                    g := key
                    members := ResizeArray()
                    (!members).Add(e.Current)
            yield (!g, !members)}

    // http://codebetter.com/matthewpodwysocki/2009/05/06/functionally-implementing-intersperse/
    let intersperse sep list = seq {
        let notFirst = ref false
        for element in list do 
            if !notFirst then yield sep
            yield element
            notFirst := true}

    let intercalate separator source = seq {
        let notFirst = ref false
        for element in source do 
            if !notFirst then yield! separator
            yield! element
            notFirst := true}

    let split separators source =
        let split options = seq {
            match separators |> Seq.map Seq.toList |> Seq.toList with
            | []         -> yield source
            | separators ->
                let buffer = ResizeArray()
                let candidate = separators |> List.map List.length |> List.max |> ResizeArray
                let mutable i = 0
                for item in source do
                    candidate.Add item
                    match separators |> List.filter (fun sep -> sep.Length > i && item = sep.[i]) with
                    | [] ->
                        i <- 0
                        buffer.AddRange candidate
                        candidate.Clear()                    
                    | seps ->
                        if seps |> List.exists (fun sep -> sep.Length = i + 1) then
                            i <- 0
                            if options = StringSplitOptions.None || buffer.Count > 0 then yield buffer.ToArray() :> seq<_>
                            buffer.Clear()
                            candidate.Clear()                        
                        else i <- i + 1
                if candidate.Count > 0 then buffer.AddRange candidate
                if options = StringSplitOptions.None || buffer.Count > 0 then yield buffer :> seq<_> }
        split StringSplitOptions.None

    let replace (oldValue:seq<'t>) (newValue:seq<'t>) (source:seq<'t>) :seq<'t> = seq {
        let old = oldValue |> Seq.toList
        if (old.Length = 0) then
            yield! source
        else
            let candidate = ResizeArray(old.Length)
            let mutable sindex = 0
            for item in source do
                candidate.Add(item)
                if (item = old.[sindex]) then
                    sindex <- sindex + 1
                    if (sindex >= old.Length) then
                        sindex <- 0
                        yield! newValue
                        candidate.Clear()                    
                else
                    sindex <- 0
                    yield! candidate
                    candidate.Clear()                
            yield! candidate}

    /// <summary>Returns a sequence that drops N elements of the original sequence and then yields the
    /// remaining elements of the sequence.</summary>
    /// <remarks>When count exceeds the number of elements in the sequence it
    /// returns an empty sequence instead of throwing an exception.</remarks>
    /// <param name="count">The number of items to drop.</param>
    /// <param name="source">The input sequence.</param>
    ///
    /// <returns>The result sequence.</returns>
    let drop i (source:seq<_>) =
        let mutable count = i
        use e = source.GetEnumerator()
        while (count > 0 && e.MoveNext()) do count <- count-1
        seq {while (e.MoveNext()) do yield e.Current}

    let replicate count initial = Linq.Enumerable.Repeat(initial, count)


/// Additional operations on List
module List =
    let singleton x = [x]
    let cons x y = x :: y
    let apply f x = List.collect (fun f -> List.map ((<|) f) x) f
    let tails x = let rec loop = function [] -> [] | _::xs as s -> s::(loop xs) in loop x
    let take i list = Seq.take i list |> Seq.toList

    let skip i list =
        let rec listSkip lst = function 
            | 0 -> lst 
            | n -> listSkip (List.tail lst) (n-1)
        listSkip list i


    /// <summary>Returns a list that drops N elements of the original list and then yields the
    /// remaining elements of the list.</summary>
    /// <remarks>When count exceeds the number of elements in the list it
    /// returns an empty list instead of throwing an exception.</remarks>
    /// <param name="count">The number of items to drop.</param>
    /// <param name="source">The input list.</param>
    ///
    /// <returns>The result list.</returns>
    let drop i list = 
        let rec loop i lst = 
            match (lst, i) with
            | ([] as x, _) | (x, 0) -> x
            | x, n -> loop (n-1) (List.tail x)
        if i > 0 then loop i list else list

    let intercalate (separator:list<_>) (source:seq<list<_>>) = source |> Seq.intercalate separator |> Seq.toList
    let intersperse element source = source |> List.toSeq |> Seq.intersperse element |> Seq.toList                              : list<'T>
    let split (separators:seq<list<_>>) (source:list<_>) = source |> List.toSeq |> Seq.split separators |> Seq.map Seq.toList
    let replace oldValue newValue source = source |> List.toSeq |> Seq.replace oldValue newValue |> Seq.toList                  : list<'T>


/// Additional operations on Array
module Array =
    let intercalate (separator:_ []) (source:seq<_ []>) = source |> Seq.intercalate separator |> Seq.toArray
    let intersperse element source = source |> Array.toSeq |> Seq.intersperse element |> Seq.toArray                            : 'T []
    let split (separators:seq<_ []>) (source:_ []) = source |> Array.toSeq |> Seq.split separators |> Seq.map Seq.toArray
    let replace oldValue newValue source = source |> Array.toSeq |> Seq.replace oldValue newValue |> Seq.toArray                : 'T []


/// Additional operations on String
module String =
    open System.Text
    open System.Globalization

    let intercalate (separator:string) (source:seq<string>) = String.Join(separator, source)
    let intersperse (element: char) (source: string) = String.Join("", Array.ofSeq (source |> Seq.intersperse element))
    let split (separators:seq<string>) (source:string) = source.Split(Seq.toArray separators, StringSplitOptions.None) :> seq<_>
    let replace (oldValue: string) newValue (source: string) = if oldValue.Length = 0 then source else source.Replace(oldValue, newValue)

    let isSubString subString (source:string) = source.Contains subString
    let startsWith  subString (source:string) = source.StartsWith (subString, false, CultureInfo.InvariantCulture)
    let endsWith    subString (source:string) = source.EndsWith   (subString, false, CultureInfo.InvariantCulture)
    let contains    char      (source:string) = Seq.contains char source
    let toUpper (source:string) = if isNull source then source else source.ToUpperInvariant()
    let toLower (source:string) = if isNull source then source else source.ToLowerInvariant()    
    let trimWhiteSpaces (source:string) = source.Trim()

    let normalize normalizationForm (source:string) = if isNull source then source else source.Normalize normalizationForm
    let removeDiacritics (source:string) =
        if isNull source then source
        else 
            source 
            |> normalize NormalizationForm.FormD
            |> String.filter (fun ch -> CharUnicodeInfo.GetUnicodeCategory ch <> UnicodeCategory.NonSpacingMark)
            |> normalize NormalizationForm.FormC


/// Module containing F#+ Extension Methods  
module Extensions =

    type Collections.Generic.IEnumerable<'T>  with
        member this.GetSlice = function
            | None  , None   -> this
            | Some a, None   -> this |> Seq.skip a
            | None  , Some b -> this |> Seq.take b
            | Some a, Some b -> this |> Seq.skip a |> Seq.take (b-a+1)


    type List<'T> with
        
        member this.GetSlice = function
            | None  , None   -> this
            | Some a, None   when a < 0 -> this |> List.skip (this.Length + a)
            | Some a, None              -> this |> List.skip                a 
            | None  , Some b when b < 0 -> this |> List.take (this.Length + b)
            | None  , Some b            -> this |> List.take                b
            | Some a, Some b when a >= 0 && b >= 0 -> this |> List.skip a |> List.take b
            | Some a, Some b -> 
                let l = this.Length
                let f i = if i < 0 then l + i else i
                let a = f a
                this |> List.skip a |> List.take (f b - a + 1)

         

    // http://msdn.microsoft.com/en-us/library/system.threading.tasks.task.whenall.aspx 

    open System.Threading
    open System.Threading.Tasks

    let private (|Canceled|Faulted|Completed|) (t: Task<'a>) =
        if t.IsCanceled then Canceled
        else if t.IsFaulted then Faulted(t.Exception)
        else Completed(t.Result)

    type Task<'t> with
        static member WhenAll(tasks : Task<'a>[], ?cancellationToken : CancellationToken) =
            let tcs = TaskCompletionSource<'a[]>()
            let cancellationToken = defaultArg cancellationToken CancellationToken.None
            cancellationToken.Register((fun () -> tcs.TrySetCanceled() |> ignore)) |> ignore
            let results = Array.zeroCreate<'a>(tasks.Length)
            let pending = ref results.Length
            tasks 
            |> Seq.iteri (fun i t ->
                let continuation = function
                | Canceled -> tcs.TrySetCanceled() |> ignore
                | Faulted(e) -> tcs.TrySetException(e) |> ignore
                | Completed(r) -> 
                    results.[i] <- r
                    if Interlocked.Decrement(pending) = 0 then 
                        tcs.SetResult(results)
                t.ContinueWith(continuation, cancellationToken,
                               TaskContinuationOptions.ExecuteSynchronously,
                               TaskScheduler.Default) |> ignore)
            tcs.Task