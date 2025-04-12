Imports quackfetchcore

Module Program '"Program" means that this module is a VB.NET program
               'and bla bla bla
    Sub Main() ' Main function, the one gets called when the app starts
        Console.WriteLine(
            "Machine ID: " & 
            quackfetchcore.MachineInfo.GetMachineId()
                          )
        #If DEBUG Then
            Console.ReadLine()
        #End If
    End Sub
End Module