#!/usr/bin/env bash
# ==============================================================================
# Script de Backup Periódico do Banco de Dados PostgreSQL (Cortex - RNF006)
# ==============================================================================
# Uso agendado recomendado no cron:
# 0 2 * * * /caminho/para/scripts/backup-database.sh >> /var/log/cortex-backup.log 2>&1
# ==============================================================================

set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-cortex_db}"
DB_USER="${DB_USER:-postgres}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_DIR="${BACKUP_DIR:-$SCRIPT_DIR/../backups}"

mkdir -p "$BACKUP_DIR"

TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_backup_${TIMESTAMP}.dump"

echo "[CORTEX-BACKUP] [$(date '+%Y-%m-%d %H:%M:%S')] Iniciando backup da base $DB_NAME em $DB_HOST:$DB_PORT..."

pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -F c -b -v -f "$BACKUP_FILE"

echo "[CORTEX-BACKUP] [$(date '+%Y-%m-%d %H:%M:%S')] Backup concluído: $BACKUP_FILE ($(du -h "$BACKUP_FILE" | cut -f1))"

# Rotação de arquivos antigos
echo "[CORTEX-BACKUP] [$(date '+%Y-%m-%d %H:%M:%S')] Removendo backups com mais de $RETENTION_DAYS dias..."
find "$BACKUP_DIR" -name "${DB_NAME}_backup_*.dump" -type f -mtime +"$RETENTION_DAYS" -exec rm -f {} \;

echo "[CORTEX-BACKUP] [$(date '+%Y-%m-%d %H:%M:%S')] Procedimento de backup concluído com sucesso."
