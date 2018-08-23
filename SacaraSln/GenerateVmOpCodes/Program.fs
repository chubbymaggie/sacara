﻿/// This simple program produce a text output with VM opcodes. 
/// The generated opcode are saved in the apprppriate src directory.
namespace GenerateVmOpCodes

open System
open System.Collections.Generic
open System.Text
open ES.Sacara.Ir.Assembler
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open System.IO
open System.Reflection
open ES.Sacara.Ir.Assembler.Instructions

module Program =
    let generateOpCodes() =
        let opCodesBytes = new HashSet<Int32>()
        let opCodes = new Dictionary<String, VmOpCodeItem>()
        let rnd = new Random()
        
        FSharpType.GetUnionCases(typeof<VmOpCodes>)
        |> Array.iter(fun case ->            
            opCodes.Add(case.Name, new VmOpCodeItem(Name=case.Name))
            for j=1 to rnd.Next(2, 6) do
                let opCode = rnd.Next(10, 65535)
                if opCodesBytes.Add(opCode) then
                    opCodes.[case.Name].Bytes.Add(opCode)
        )
        opCodes

    let saveOpCodeInAssemblerDir(opCodes: Dictionary<String, VmOpCodeItem>) =
        let opCodeJson = JsonConvert.SerializeObject(opCodes.Values, Formatting.Indented)
        Console.WriteLine(opCodeJson)
        
        // copy file
        let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let assemblerSrcFile = Path.Combine(curDir, "..", "..", "..", "ES.Sacara.Ir.Assembler", "vm_opcodes.json")
        File.WriteAllText(assemblerSrcFile, opCodeJson)
        Console.WriteLine("Files copied to: " + assemblerSrcFile)

    let convertToDword(word32: Int32) =
        let word16 = UInt16.Parse(word32.ToString())
        let wordBytes = BitConverter.GetBytes(word16) |> Array.rev
        let wordString = String.Join(String.Empty, wordBytes |> Seq.map(fun b -> b.ToString("X")))
        String.Format("0{0}h", wordString)

    let saveOpCodeinVmDir(opCodes: Dictionary<String, VmOpCodeItem>) =
        let sb = new StringBuilder()
        sb.AppendLine("; This file is auto generated, don't modify it") |> ignore

        let rnd = new Random()
        let marker1 = uint32(rnd.Next(0, 0xFFFF) <<< 18 ||| rnd.Next(0, 0xFFFF)).ToString("X").PadLeft(8, '0')
        let marker2 = uint32(rnd.Next(0, 0xFFFF) <<< 18 ||| rnd.Next(0, 0xFFFF)).ToString("X").PadLeft(8, '0')
        sb.AppendFormat("marker1 EQU 0{0}h", marker1).AppendLine() |> ignore
        sb.AppendFormat("marker2 EQU 0{0}h", marker2).AppendLine() |> ignore
        
        opCodes
        |> Seq.map(fun kv -> kv.Value)
        |> Seq.iter(fun opCode ->
            let bytes = String.Join(", ", opCode.Bytes |> Seq.map convertToDword)
            sb.AppendFormat(
                "header_{0} EQU <DWORD 0{1}h, 0{2}h, {3}h, {4}>", 
                opCode.Name,
                marker1, 
                marker2, 
                opCode.Bytes.Count, 
                bytes
            ).AppendLine() |> ignore
        )

        let fileContent = sb.ToString()
        Console.WriteLine(fileContent)
        
        // copy file
        let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        let vmSrcFile = Path.Combine(curDir, "..", "..", "..", "SacaraVm", "vm_handlers.inc")
        File.WriteAllText(vmSrcFile, fileContent)
        Console.WriteLine("Files copied to: " + vmSrcFile)

    [<EntryPoint>]
    let main argv =         
        let opCodes = generateOpCodes()
        saveOpCodeInAssemblerDir(opCodes)
        saveOpCodeinVmDir(opCodes)

        Console.WriteLine("Press enter to exit...")
        Console.ReadLine() |> ignore
        0