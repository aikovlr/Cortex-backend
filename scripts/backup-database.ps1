<#
.SYNOPSIS
    Script de Backup Periódico do Banco de Dados PostgreSQL (Cortex - RNF006).

.DESCRIPTION
    Executa dump comprimido do banco PostgreSQL utilizando pg_dump,
    gerando arquivos datados e realizando rotação automática (retenção padrão: 30 dias).

.PARAMETER HostName
    Endereço do servidor PostgreSQL (Padrão: localhost).

.PARAMETER Port
    Porta do servidor PostgreSQL (Padrão: 5432).

.PARAMETER Database
    Nome do banco de dados (Padrão: cortex_db).

.PARAMETER User
    Usuário do banco de dados (Padrão: postgres).

.PARAMETER BackupDir
    Diretório de destino dos arquivos de backup.

.PARAMETER RetentionDays
    Quantidade de dias para manter backups antigos (Padrão: 30).
#>

[CmdletBinding()]
param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "cortex_db",
    [string]$User = "postgres",
    [string]$BackupDir = "$PSScriptRoot\..\backups",
    [int]$RetentionDays = 30
)

$ErrorActionPreference = "Stop"

try {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $resolvedBackupDir = [System.IO.Path]::GetFullPath($BackupDir)

    if (-not (Test-Path $resolvedBackupDir)) {
        New-Item -ItemType Directory -Path $resolvedBackupDir -Force | Out-Null
        Write-Host "[CORTEX-BACKUP] Diretório de backup criado: $resolvedBackupDir" -ForegroundColor Cyan
    }

    $backupFileName = "${Database}_backup_${timestamp}.dump"
    $backupFilePath = Join-Path $resolvedBackupDir $backupFileName

    Write-Host "[CORTEX-BACKUP] Iniciando backup de $Database em $HostName:$Port..." -ForegroundColor Cyan

    # Executa pg_dump em formato custom comprimido (-Fc)
    $pgDump = "pg_dump"
    $argsList = @(
        "-h", $HostName,
        "-p", $Port,
        "-U", $User,
        "-d", $Database,
        "-F", "c",
        "-b",
        "-v",
        "-f", $backupFilePath
    )

    & $pgDump @argsList

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump falhou com código de saída $LASTEXITCODE."
    }

    $fileSize = (Get-Item $backupFilePath).Length / 1MB
    Write-Host ("[CORTEX-BACKUP] Backup concluído com sucesso: {0} ({1:N2} MB)" -f $backupFilePath, $fileSize) -ForegroundColor Green

    # Rotação / Expiração de backups com mais de $RetentionDays dias
    Write-Host "[CORTEX-BACKUP] Verificando retenção de arquivos com mais de $RetentionDays dias..." -ForegroundColor Gray
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $oldBackups = Get-ChildItem -Path $resolvedBackupDir -Filter "${Database}_backup_*.dump" | Where-Object { $_.LastWriteTime -lt $cutoffDate }

    foreach ($file in $oldBackups) {
        Remove-Item $file.FullName -Force
        Write-Host "[CORTEX-BACKUP] Arquivo antigo removido pela rotação: $($file.Name)" -ForegroundColor Yellow
    }

    Write-Host "[CORTEX-BACKUP] Procedimento de backup finalizado com êxito." -ForegroundColor Green
}
catch {
    Write-Error "[CORTEX-BACKUP] Erro durante o backup: $_"
    exit 1
}
