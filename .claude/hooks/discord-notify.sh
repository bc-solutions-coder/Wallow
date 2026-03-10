#!/bin/bash
# Discord notification hook for Claude Code
# Reads hook event JSON from stdin and sends a formatted Discord message.

WEBHOOK_URL="https://discord.com/api/webhooks/1481020848302456924/M4h5jmZ1l9HNAQ0MwaX28WiYoa0DL68CZJ4xB-MS1VWE2WILZZtgpr5j4KJ6Z31VxxGs"

EVENT=$(cat)

HOOK_EVENT=$(echo "$EVENT" | jq -r '.hook_event_name // "Unknown"')
SESSION_ID=$(echo "$EVENT" | jq -r '.session_id // "unknown"' | cut -c1-8)
CWD=$(echo "$EVENT" | jq -r '.cwd // "unknown"')
PROJECT=$(basename "$CWD")

# Build message based on event type
case "$HOOK_EVENT" in
  SessionStart)
    TITLE="Session Started"
    DESC="Claude Code session started in **${PROJECT}**"
    COLOR=3066993  # green
    ;;
  SessionEnd)
    TITLE="Session Ended"
    DESC="Claude Code session ended in **${PROJECT}**"
    COLOR=15158332  # red
    ;;
  Stop)
    TITLE="Task Complete"
    COLOR=3447003  # blue

    # Pull active bead info
    BEAD_INFO=$(cd "$CWD" && bd list --status=in_progress --flat 2>/dev/null | head -5)
    NL=$'\n'
    if [ -n "$BEAD_INFO" ]; then
      BEAD_LINES=""
      while IFS= read -r line; do
        BEAD_ID=$(echo "$line" | awk '{print $2}')
        BEAD_TITLE=$(echo "$line" | sed 's/^.*- //')
        BEAD_LINES="${BEAD_LINES}${NL}• \`${BEAD_ID}\` — ${BEAD_TITLE}"
      done <<< "$BEAD_INFO"
      DESC="Claude Code finished a response in **${PROJECT}**${NL}${NL}**Active beads:**${BEAD_LINES}"
    else
      DESC="Claude Code finished a response in **${PROJECT}**${NL}${NL}No active beads."
    fi
    ;;
  *)
    TITLE="$HOOK_EVENT"
    DESC="Event in **${PROJECT}**"
    COLOR=9807270  # grey
    ;;
esac

# Send to Discord
PAYLOAD=$(jq -n \
  --arg title "$TITLE" \
  --arg desc "$DESC" \
  --argjson color "$COLOR" \
  --arg footer "Session: $SESSION_ID | Project: $PROJECT" \
  '{
    "embeds": [{
      "title": $title,
      "description": $desc,
      "color": $color,
      "footer": {"text": $footer},
      "timestamp": (now | todate)
    }]
  }')

curl -s -X POST "$WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" \
  >/dev/null 2>&1

exit 0
