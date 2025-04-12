using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace quackfetchcore
{
    public class MachineInfo
    {
        /// <summary>
        /// Obtém o ID/modelo da máquina atual baseado no sistema operacional
        /// </summary>
        /// <returns>String contendo a identificação da máquina</returns>
        public static string GetMachineId()
        {
            /*
             * Ported from Neofetch's machine ID system.
             *
             * Neofetch is licensed under MIT License:
             * 
             */
            
            /*
               The MIT License (MIT)
               
               Copyright (c) 2015-2021 Dylan Araps
               
               Permission is hereby granted, free of charge, to any person obtaining
               a copy of this software and associated documentation files (the
               "Software"), to deal in the Software without restriction, including
               without limitation the rights to use, copy, modify, merge, publish,
               distribute, sublicense, and/or sell copies of the Software, and to
               permit persons to whom the Software is furnished to do so, subject to
               the following conditions:
               
               The above copyright notice and this permission notice shall be included
               in all copies or substantial portions of the Software.
               
               THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
               EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
               MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
               IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
               CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
               TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
               SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
             */
            
            string model = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Verificar se é um dispositivo Android
                if (Directory.Exists("/system/app/") && Directory.Exists("/system/priv-app"))
                {
                    model = ExecuteShellCommand("getprop ro.product.brand") + " " +
                            ExecuteShellCommand("getprop ro.product.model");
                }
                // Verificar diferentes caminhos para informações de hardware no Linux
                else if (File.Exists("/sys/devices/virtual/dmi/id/board_vendor") &&
                         File.Exists("/sys/devices/virtual/dmi/id/board_name"))
                {
                    model = File.ReadAllText("/sys/devices/virtual/dmi/id/board_vendor").Trim() + " " +
                            File.ReadAllText("/sys/devices/virtual/dmi/id/board_name").Trim();
                }
                else if (File.Exists("/sys/devices/virtual/dmi/id/product_name") &&
                         File.Exists("/sys/devices/virtual/dmi/id/product_version"))
                {
                    model = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim() + " " +
                            File.ReadAllText("/sys/devices/virtual/dmi/id/product_version").Trim();
                }
                else if (File.Exists("/sys/firmware/devicetree/base/model"))
                {
                    model = File.ReadAllText("/sys/firmware/devicetree/base/model").Trim();
                }
                else if (File.Exists("/tmp/sysinfo/model"))
                {
                    model = File.ReadAllText("/tmp/sysinfo/model").Trim();
                }
                else
                {
                    // Caso não encontre, tenta usar o comando lshw
                    model = ExecuteShellCommand("lshw -c system | grep product | head -1").Replace("product:", "").Trim();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string hwModel = ExecuteShellCommand("sysctl -n hw.model").Trim();
                
                // Verificando se é Hackintosh
                string kextstat = ExecuteShellCommand("kextstat | grep -F -e \"FakeSMC\" -e \"VirtualSMC\"").Trim();
                if (!string.IsNullOrEmpty(kextstat))
                {
                    model = $"Hackintosh (SMBIOS: {hwModel})";
                }
                else
                {
                    model = hwModel;
                }
            }
            /*else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Utilizando WMI para obter informações do sistema no Windows
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
                    {
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            string manufacturer = queryObj["Manufacturer"]?.ToString().Trim() ?? "";
                            string computerModel = queryObj["Model"]?.ToString().Trim() ?? "";
                            
                            model = $"{manufacturer} {computerModel}";
                        }
                    }
                }
                catch (Exception ex)
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
        /// Executa um comando shell e retorna o resultado como string
        /// </summary>
        public static string ExecuteShellCommand(string command)
        {
            try
            {
                ProcessStartInfo processInfo;
                Process process;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }
                else // Linux, macOS, etc.
                {
                    processInfo = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }

                process = Process.Start(processInfo);
                process.WaitForExit();

                // Lê a saída do comando
                string output = process.StandardOutput.ReadToEnd();
                return output;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Limpa a string do modelo de informações desnecessárias ou genéricas
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

            foreach (string unwanted in unwantedStrings)
            {
                model = model.Replace(unwanted, "");
            }

            // Remove espaços extras
            model = Regex.Replace(model, @"\s+", " ").Trim();

            return model;
        }
        
        
    }
}