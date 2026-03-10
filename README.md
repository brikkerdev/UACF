# UACF - Unity Autonomous Control Framework

Unity 6.3 проект с HTTP API сервером для управления редактором через AI-агентов (Cursor, Claude Code).

## Содержимое

- **com.uacf.editor** — UPM-пакет с HTTP API (localhost:7890)
- **Docs/** — технические задания UACF и UI Framework
- **Assets/** — сцены и ассеты проекта

## Требования

- Unity 6.3+

## Быстрый старт

1. Откройте проект в Unity Editor
2. Сервер UACF запускается автоматически на порту 7890
3. Проверка: `curl http://localhost:7890/api/status` или `curl http://localhost:7890/api/ping`

Подробнее: [Packages/com.uacf.editor/README.md](Packages/com.uacf.editor/README.md)
