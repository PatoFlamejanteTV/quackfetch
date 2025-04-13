Imports qfcore

Module Program '"Program" means that this module is a VB.NET program
    'and bla bla bla
    Sub Main() ' Main function, the one gets called when the app starts

        'TODO: Display more info idfk bruh :sob:

        Console.WriteLine("-------------")
        Console.WriteLine("   QuackFetch")
        Console.WriteLine("-------------")

        Console.WriteLine(
            "Machine ID: " &
            MachineInfo.GetMachineId()
            )

        Console.WriteLine(
            "Distro:     " &
            MachineInfo.GetPrettyName()
            )

        Console.WriteLine(
            "Kernel:     " &
            MachineInfo.GetKernel()
            )

        Console.WriteLine(
            "Local IP:   " &
            MachineInfo.GetLocalIP()
            )

        Console.WriteLine("-------------")

        'Console.WriteLine(
        '    "Packages: " &
        '    MachineInfo.GetPackages()
        '    )
        ' BUG: Somehow it opens fucking pacman when i display the packages amount wtf
#If DEBUG Then
        Console.ReadLine()
#End If
    End Sub
End Module