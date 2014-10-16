﻿[<AutoOpen>]
module internal Dyfrig.Machine.Logic

open System
open System.Globalization
open Aether
open Aether.Operators
open Dyfrig.Core
open Dyfrig.Core.Operators
open Dyfrig.Http

(* Helpers *)

let inline private (!?) x =
    Option.isSome <!> x

let inline private (!.) x =
    List.isEmpty >> not <!> x

let inline private (=?) x y =
    (=) y <!> x

let inline private (?>) (x, y) f =
    (fun x y -> f (x, y)) <!> x <*> y

(* Content Negotiation *)

[<RequireQualifiedAccess>]
module Charset =

    let private defaults =
        [ SpecifiedCharset.Named "iso-8859-1" ]

    let private request =
        getPLM Request.Headers.acceptCharset

    let private supported =
        getPLM (definitionPLens >??> configurationPLens Configuration.CharsetsSupported)

    let private negotiation =
        function | Some requested, Some available -> negotiateAcceptCharset available requested
                 | Some requested, _ -> negotiateAcceptCharset defaults requested
                 | _, Some available -> available
                 | _, _ -> defaults

    let requested : MachineDecision =
        !? request

    let negotiated =
        (request, supported) ?> negotiation

    let negotiable : MachineDecision =
        !. negotiated


[<RequireQualifiedAccess>]
module Encoding =
        
    let private defaults =
        [ SpecifiedEncoding.Identity ]

    let private request =
        getPLM Request.Headers.acceptEncoding

    let private supported =
        getPLM (definitionPLens >??> configurationPLens Configuration.EncodingsSupported)

    let private negotiation =
        function | Some requested, Some available -> negotiateAcceptEncoding available requested
                 | Some requested, _ -> negotiateAcceptEncoding defaults requested
                 | _, Some available -> available
                 | _, _ -> defaults

    let requested : MachineDecision =
        !? request

    let negotiated =
        (request, supported) ?> negotiation

    let negotiable : MachineDecision =
        !. negotiated 


[<RequireQualifiedAccess>]
module Language =

    let private defaults =
        List.empty<CultureInfo>
        
    let private request =
        getPLM Request.Headers.acceptLanguage

    let private supported =
        getPLM (definitionPLens >??> configurationPLens Configuration.LanguagesSupported)

    let private negotiation =
        function | Some requested, Some available -> negotiateAcceptLanguage available requested
                 | Some requested, _ -> negotiateAcceptLanguage defaults requested
                 | _, Some available -> available
                 | _, _ -> defaults

    let requested : MachineDecision =
        !? request

    let negotiated =
        (request, supported) ?> negotiation

    let negotiable : MachineDecision =
        !. negotiated


[<RequireQualifiedAccess>]
module MediaType =

    let private defaults =
        List.empty<SpecifiedMediaRange>

    let private request =
        getPLM Request.Headers.accept

    let private supported =
        getPLM (definitionPLens >??> configurationPLens Configuration.MediaTypesSupported)

    let private negotiation =
        function | Some requested, Some available -> negotiateAccept available requested
                 | Some requested, _ -> negotiateAccept defaults requested
                 | _, Some available -> available
                 | _, _ -> defaults

    let requested : MachineDecision =
        !? request

    let negotiated =
        (request, supported) ?> negotiation

    let negotiable : MachineDecision =
        !. negotiated

(* Control *)

[<RequireQualifiedAccess>]
module IfMatch =

    let private ifMatch =
        getPLM Request.Headers.ifMatch

    let requested : MachineDecision =
        !? ifMatch

    let any : MachineDecision =
        ifMatch =? Some IfMatch.Any


[<RequireQualifiedAccess>]
module IfModifiedSince =

    let private ifModifiedSince =
        getPLM Request.Headers.ifModifiedSince

    let private lastModified =
        getPLM (definitionPLens >??> configurationPLens Configuration.LastModified)

    let requested : MachineDecision =
        !? ifModifiedSince

    let valid : MachineDecision =
            Option.map ((>) DateTime.UtcNow) >> Option.getOrElse false
        <!> ifModifiedSince

    let modified =
        owin {
            let! lm = lastModified
            let! ms = ifModifiedSince

            match lm, ms with
            | Some lm, Some ms -> return! ((<) ms) <!> lm
            | _ -> return false }


[<RequireQualifiedAccess>]
module IfNoneMatch =

    let private ifNoneMatch =
        getPLM Request.Headers.ifNoneMatch

    let requested : MachineDecision =
        !? ifNoneMatch

    let any : MachineDecision =
        ifNoneMatch =? Some IfNoneMatch.Any


[<RequireQualifiedAccess>]
module IfUnmodifiedSince =

    let private ifUnmodifiedSince =
        getPLM Request.Headers.ifUnmodifiedSince

    let private lastModified =
        getPLM (definitionPLens >??> configurationPLens Configuration.LastModified)

    let requested : MachineDecision =
        !? ifUnmodifiedSince

    let valid : MachineDecision =
            Option.map ((>) DateTime.UtcNow) >> Option.getOrElse false
        <!> ifUnmodifiedSince

    let modified =
        owin {
            let! lm = lastModified
            let! us = ifUnmodifiedSince

            match lm, us with
            | Some lm, Some us -> return! ((>) us) <!> lm
            | _ -> return true }

(* Request *)

[<RequireQualifiedAccess>]
module Method =
    
    let private defaultKnown =
        Set.ofList 
            [ DELETE
              HEAD
              GET
              OPTIONS
              PATCH
              POST
              PUT
              TRACE ]

    let private defaultSupported =
        Set.ofList
            [ GET
              HEAD ]

    let private meth =
        getLM Request.meth

    let private methodsKnown =
        getPLM (definitionPLens >??> configurationPLens Configuration.MethodsKnown)

    let private negotiateKnown =
        function | x, Some y -> Set.contains x y
                 | x, _ -> Set.contains x defaultKnown

    let private methodsSupported =
        getPLM (definitionPLens >??> configurationPLens Configuration.MethodsSupported)

    let private negotiateSupported =
        function | x, Some y -> Set.contains x y
                 | x, _ -> Set.contains x defaultSupported

    let known : MachineDecision =
        (meth, methodsKnown) ?> negotiateKnown

    let supported : MachineDecision =
        (meth, methodsSupported) ?> negotiateSupported

    let delete : MachineDecision =
        meth =? DELETE 

    let getOrHead : MachineDecision =
            (fun x -> x = GET || x = HEAD) 
        <!> meth

    let options : MachineDecision =
        meth =? OPTIONS

    let patch : MachineDecision =
        meth =? PATCH

    let post : MachineDecision =
        meth =? POST

    let put : MachineDecision =
        meth =? PUT