#!/bin/bash
# On-demand Discord ping - call directly or via alias
# Usage: .claude/hooks/discord-ping.sh "Your message here"

WEBHOOK_URL="https://discord.com/api/webhooks/1481020848302456924/M4h5jmZ1l9HNAQ0MwaX28WiYoa0DL68CZJ4xB-MS1VWE2WILZZtgpr5j4KJ6Z31VxxGs"

MESSAGE="${1:-Ping from Claude Code!}"
PROJECT=$(basename "$(pwd)")

PAYLOAD=$(jq -n \
  --arg msg "$MESSAGE" \
  --arg project "$PROJECT" \
  '{
    "embeds": [{
      "title": "Claude Code Ping",
      "description": $msg,
      "color": 16750848,
      "footer": {"text": ("Project: " + $project)},
      "timestamp": (now | todate)
    }]
  }')

curl -s -X POST "$WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" \
  >/dev/null 2>&1

echo "Discord notification sent."
