$projectPath = Split-Path $PSScriptRoot -Parent
$windowsTerminal = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\wt.exe'
$codexPath = Join-Path $env:APPDATA 'npm\codex.ps1'

$terminalArguments = @(
    '-d', $projectPath,
    '--',
    'powershell.exe',
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', $codexPath
)

Start-Process -FilePath $windowsTerminal -Verb RunAs -ArgumentList $terminalArguments
