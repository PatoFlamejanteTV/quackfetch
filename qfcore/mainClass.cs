using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace qfcore;

public class MachineInfo
{
    /// <summary>
    ///     Obtém o ID/modelo da máquina atual baseado no sistema operacional
    /// </summary>
    /// <returns>String contendo a identificação da máquina</returns>
    public static string GetMachineId()
    {
        /*
         * Ported from Neofetch's machine ID system.
         * Neofetch is licensed under MIT License
         */

        var model = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Verificar se é um dispositivo Android
            if (Directory.Exists("/system/app/") && Directory.Exists("/system/priv-app"))
                model = ExecuteShellCommand("getprop ro.product.brand") + " " +
                        ExecuteShellCommand("getprop ro.product.model");
            // Verificar diferentes caminhos para informações de hardware no Linux
            else if (File.Exists("/sys/devices/virtual/dmi/id/board_vendor") &&
                     File.Exists("/sys/devices/virtual/dmi/id/board_name"))
                model = File.ReadAllText("/sys/devices/virtual/dmi/id/board_vendor").Trim() + " " +
                        File.ReadAllText("/sys/devices/virtual/dmi/id/board_name").Trim();
            else if (File.Exists("/sys/devices/virtual/dmi/id/product_name") &&
                     File.Exists("/sys/devices/virtual/dmi/id/product_version"))
                model = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim() + " " +
                        File.ReadAllText("/sys/devices/virtual/dmi/id/product_version").Trim();
            else if (File.Exists("/sys/firmware/devicetree/base/model"))
                model = File.ReadAllText("/sys/firmware/devicetree/base/model").Trim();
            else if (File.Exists("/tmp/sysinfo/model"))
                model = File.ReadAllText("/tmp/sysinfo/model").Trim();
            else
                // Caso não encontre, tenta usar o comando lshw
                model = ExecuteShellCommand("lshw -c system | grep product | head -1").Replace("product:", "")
                    .Trim();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var hwModel = ExecuteShellCommand("sysctl -n hw.model").Trim();

            // Verificando se é Hackintosh
            var kextstat = ExecuteShellCommand("kextstat | grep -F -e \"FakeSMC\" -e \"VirtualSMC\"").Trim();
            if (!string.IsNullOrEmpty(kextstat))
                model = $"Hackintosh (SMBIOS: {hwModel})";
            else
                model = hwModel;
        }
        /*else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // VERY WIP
        {
        // TODO: Ter vergonha na cara e fazer um metodo melhor pra isso.
            // Utilizando WMI para obter informações do sistema no Windows
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject queryObj in results)
                    {
                        string manufacturer = queryObj["Manufacturer"]?.ToString().Trim() ?? "";
                        string computerModel = queryObj["Model"]?.ToString().Trim() ?? "";

                        if (!string.IsNullOrWhiteSpace(manufacturer) || !string.IsNullOrWhiteSpace(computerModel))
                        {
                            model = $"{manufacturer} {computerModel}".Trim();
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback se WMI falhar
                model = $"Windows PC ({Environment.MachineName})";
            }
        }*/
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            // Para FreeBSD e similares
            model = ExecuteShellCommand("sysctl -n hw.vendor hw.product").Trim();
        }
        else
        {
            // Para outros sistemas operacionais não identificados
            model = $"Unknown OS ({RuntimeInformation.OSDescription})";
        }

        // Limpar informações irrelevantes
        model = CleanModelString(model);

        // Identificações específicas para VMs conhecidas
        if (model.Contains("Standard PC") && model.Contains("QEMU"))
        {
            model = $"KVM/QEMU ({model})";
        }
        else if (model.StartsWith("OpenBSD"))
        {
            model = $"vmm ({model})";
        }
        else if (model.Contains("VirtualBox") || model.Contains("VMware"))
        {
            // Já está identificado como uma VM
        }

        return string.IsNullOrWhiteSpace(model) ? "Unknown Device" : model;
    }

    /// <summary>
    ///     Executa um comando shell e retorna o resultado como string
    /// </summary>
    public static string ExecuteShellCommand(string command)
    {
        try
        {
            ProcessStartInfo processInfo;
            Process process;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            else // Linux, macOS, etc.
                processInfo = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

            process = Process.Start(processInfo);
            process.WaitForExit();

            // Lê a saída do comando
            var output = process.StandardOutput.ReadToEnd();
            return output;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     Limpa a string do modelo de informações desnecessárias ou genéricas
    /// </summary>
    public static string CleanModelString(string model)
    {
        // Remove strings genéricas ou indesejadas
        string[] unwantedStrings = new[]
        {
            "To be filled by O.E.M.",
            "To Be Filled",
            "OEM",
            "Not Applicable",
            "System Product Name",
            "System Version",
            "Undefined",
            "Default string",
            "Not Specified",
            "Type1ProductConfigId",
            "INVALID",
            "All Series",
            "�"
        };

        foreach (var unwanted in unwantedStrings) model = model.Replace(unwanted, "");

        // Remove espaços extras
        model = Regex.Replace(model, @"\s+", " ").Trim();

        return model;
    }

    public static string GetPrettyName()
    {
        // Check if /etc/os-release exists
        var osReleasePath = "/etc/os-release";
        if (File.Exists(osReleasePath))
        {
            var lines = File.ReadAllLines(osReleasePath);
            foreach (var line in lines)
                if (line.StartsWith("PRETTY_NAME="))
                    // Extract PRETTY_NAME
                    return line.Split('=')[1].Trim('"');
        }

        // Fallback: Check other files
        string[] fallbackFiles = { "/etc/lsb-release", "/etc/debian_version", "/etc/redhat-release" };
        foreach (var path in fallbackFiles)
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();

        // If all else fails, return unknown
        return "Unknown";
    }

    public static string GetKernel()
    {
        try
        {
            // Create a new process to run the "uname -r" command
            var process = new Process();
            process.StartInfo.FileName = "uname";
            process.StartInfo.Arguments = "-r"; // Fetch kernel version
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            // Start the process and read the output
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Return the trimmed output
            return output.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return "Unknown";
        }
    }

    public static string GetPackages()
    {
        var packages = 0;
        var pkgs_h = 0;
        string manager = null;
        var managers = new List<string>();
        var managerString = string.Empty;

        bool Has(string command)
        {
            return RunCommand($"type {command}", out _) == 0;
        }

        int RunCommand(string command, out string output)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "bash",
                        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? $"/c {command}"
                            : $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode;
            }
            catch
            {
                output = string.Empty;
                return -1;
            }
        }

        int intRunCommand(string command)
        {
            return RunCommand(command, out _);
        }

        void Tot(string command)
        {
            if (RunCommand(command, out var output) == 0)
            {
                var pkgs = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                packages += pkgs.Length;
                Pac(pkgs.Length - pkgs_h);
            }
        }

        void Dir(string path)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToArray();
            packages += files.Length;
            Pac(files.Length - pkgs_h);
        }

        void Pac(int count)
        {
            if (count > 0)
            {
                managers.Add($"{count} ({manager})");
                managerString += $"{manager}, ";
            }
        }

        // Example logic for Linux package managers
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            if (Has("pacman"))
            {
                manager = "pacman";
                Tot("pacman -Qq --color never");
            }

            if (Has("dpkg"))
            {
                manager = "dpkg";
                Tot("dpkg-query -f '.\\n' -W");
            }

            if (Has("rpm"))
            {
                manager = "rpm";
                Tot("rpm -qa");
            }

            // Add more package managers here...
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // Example logic for Windows package managers
            if (Has("choco"))
            {
                manager = "choco";
                Dir(@"C:\ProgramData\chocolatey\lib");
            }

            if (Has("scoop"))
            {
                manager = "scoop";
                Dir(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\scoop\apps");
            }
        }

        if (packages == 0) return "No packages found";

        var result = packages.ToString();
        result += $" ({managerString.TrimEnd(',', ' ')})";
        return result;
    }
}