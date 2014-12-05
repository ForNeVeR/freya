﻿[<AutoOpen>]
module Freya.Machine.Execution

open Freya.Core
open Freya.Core.Operators
open Freya.Pipeline
open Freya.Types

(* Traversal *)

let private action a =
    freya {
        do! a.Action
        do! executionR { Id = a.Id }

        return a.Next }

let private decision d =
    freya {
        let! next =
               (function | true -> d.True
                         | _ -> d.False)
            <!> d.Decision

        do! executionR { Id = d.Id }

        return next }

let private handler (h: FreyaMachineHandlerNode) =
    freya {
        do! executionR { Id = h.Id }

        return h.Handler }

let private operation o =
    freya {
        do! o.Operation
        do! executionR { Id = o.Id }

        return o.Next }

let private traverse (graph: FreyaMachineGraph) =
    let rec eval from =
        freya {
            match Map.find from graph with
            | ActionNode a -> return! action a >>= eval
            | DecisionNode d -> return! decision d >>= eval
            | HandlerNode h -> return! handler h
            | OperationNode o -> return! operation o >>= eval }

    eval Decisions.ServiceAvailable

(* Compilation *)

let private nodes =
      actions
    @ decisions
    @ handlers
    @ operations

let compileFreyaMachine (machine: FreyaMachine) : FreyaPipeline =
    let definition = snd (machine Map.empty)
    let graph = construct definition nodes

    freya {
        do! initR ()
        do! setPLM definitionPLens definition
        do! traverse graph >>= represent

        return Halt }