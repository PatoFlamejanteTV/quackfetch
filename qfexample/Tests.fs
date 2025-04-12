open qfcore
open System

let getMachineInfo () =
    try
        let machineId = MachineInfo.GetMachineId()
        printfn "Machine ID: %s" machineId
        machineId
    with ex ->
        printfn "Error trying to get machine info: %s" ex.Message
        "Erro"

let executeShellCommand command =
    try
        printfn "Executing command: %s" command
        let result = MachineInfo.ExecuteShellCommand(command)
        printfn "Command result: %s" result
        result
    with ex ->
        printfn "Error while executing command: %s" ex.Message
        "Erro na execução"

let getPlatformInfo () =
    try
        match Environment.OSVersion.Platform with
        | PlatformID.Unix -> executeShellCommand "uname -a"
        | PlatformID.Win32NT -> executeShellCommand "systeminfo | findstr /B /C:\"OS\""
        | PlatformID.MacOSX -> executeShellCommand "sw_vers"
        | _ -> "Unknown platform"
    with ex ->
        printfn "Error while trying to get machine info: %s" ex.Message
        "Error"

let demonstrateModelCleaning () =
    try
        let modelExamples = [ "Model-ABC (Rev.1)"; "System_XYZ-123"; "Device/456-Test" ]

        modelExamples
        |> List.iter (fun model ->
            let cleaned = MachineInfo.CleanModelString(model)
            printfn "Original: %s --> Cleaned: %s" model cleaned)
    with ex ->
        printfn "Error while cleaning: %s" ex.Message

let isValidCommand (command: string) =
    // Lista de comandos permitidos para maior segurança
    let allowedCommands = [ "dir"; "ls"; "echo"; "hostname"; "systeminfo"; "uname" ]

    let commandBase = command.Split(' ').[0].Trim().ToLower()
    List.contains commandBase allowedCommands

let processCommandLineArgs (args: string[]) =
    if args.Length > 0 then
        printfn "\nProcessing command line arguments:"

        args
        |> Array.iteri (fun i arg ->
            printfn "Argument %d: %s" i arg

            if arg.StartsWith("cmd:") then
                let cmd = arg.Substring(4)

                if isValidCommand cmd then
                    executeShellCommand cmd |> ignore
                else
                    printfn "Comando não permitido: %s" cmd)

[<EntryPoint>]
let main argv =
    try
        let machineId = getMachineInfo ()
        printfn "Utilizando máquina: %s" machineId

        let platformInfo = getPlatformInfo ()
        printfn "Informações da plataforma: %s" platformInfo

        demonstrateModelCleaning ()
        processCommandLineArgs argv

#if DEBUG
        printfn "\nPressione qualquer tecla para sair..."
        Console.ReadLine() |> ignore
#endif

        0 // código de sucesso
    with ex ->
        printfn "Erro fatal: %s" ex.Message
        1 // código de erro
