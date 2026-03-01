# Multi-Agent Orchestration with tmux and Valkey

## Overview

A multi-agent system for parallel and specialized development work using tmux panes, Valkey pub/sub for real-time coordination, and beads for persistent task management.

## Goals

- **Parallel execution**: Multiple agents working on independent tasks simultaneously
- **Specialized roles**: Agents with different expertise that hand off work
- **Hybrid oversight**: Autonomy for routine tasks, approval gates for risky operations
- **Persistence**: Work survives session restarts via beads and tmux-resurrect
- **Recovery**: Automatic detection and respawning of dead agents

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│  tmux session: "foundry-agents"                                     │
├───────────┬───────────┬───────────┬───────────┬────────────────────┤
│  Pane 0   │  Pane 1   │  Pane 2   │  Pane 3   │  Pane 4            │
│ORCHESTRATOR│ PLANNER  │IMPLEMENTER│  TESTER   │  REVIEWER          │
│  (always) │ (spawned) │ (spawned) │ (spawned) │  (spawned)         │
└─────┬─────┴─────┬─────┴─────┬─────┴─────┬─────┴──────────┬─────────┘
      │           │           │           │                │
      │           └───────────┴───────────┴────────────────┘
      │                              │
      ▼                              ▼
┌───────────┐                 ┌─────────────┐
│   Beads   │                 │    Valkey    │
│ (bd CLI)  │                 │  (pub/sub)  │
├───────────┤                 ├─────────────┤
│ • Epics   │                 │ • Heartbeat │
│ • Tasks   │                 │ • Approvals │
│ • Deps    │                 │ • Signals   │
│ • Status  │                 │             │
└─────┬─────┘                 └─────────────┘
      │
      ▼
┌───────────┐
│    Git    │
│ .beads/   │
└───────────┘
```

## Agent Roles

### ORCHESTRATOR (Pane 0) - Always running

- Receives your requests
- Decides which workflow to run (feature dev, bug fix, refactor, etc.)
- Spawns/kills agent panes as needed
- Monitors agent health via heartbeats
- Handles approval requests for risky operations
- Reports final status back to you

### PLANNER (spawned on demand)

- Breaks work into tasks
- Creates implementation specs
- Coordinates task dependencies

### IMPLEMENTER (spawned on demand)

- Writes production code
- Can run multiple in parallel for independent modules

### TESTER (spawned on demand)

- Writes tests (unit, integration)
- Can work in parallel with IMPLEMENTER
- Validates test coverage

### REVIEWER (spawned on demand)

- Code review, architecture review
- Runs full test suite
- Final quality gate before completion

## System Responsibilities

### Beads (Work Items)

| Responsibility | How |
|----------------|-----|
| What tasks need doing? | `bd ready` |
| Task dependencies | `bd dep add` |
| Status tracking | `bd update --status` |
| Persistence | Git-backed .beads/ |
| Audit trail | Git history |

### Valkey (Real-time Coordination)

| Responsibility | How |
|----------------|-----|
| Agent health | Heartbeat channel |
| Approval requests | Approval channel |
| Agent-to-agent signals | Pub/sub |
| Fast coordination | In-memory |

## Workflows

### Feature Development

```
PLANNER → IMPLEMENTER + TESTER (parallel) → REVIEWER
```

### Bug Fix

```
IMPLEMENTER → TESTER → REVIEWER
```

### Refactor

```
PLANNER → IMPLEMENTER → REVIEWER
```

### Quick Fix

```
IMPLEMENTER only
```

## Valkey Message Bus

### Channels (pub/sub for real-time)

| Channel | Purpose | Publishers | Subscribers |
|---------|---------|------------|-------------|
| `agents:heartbeat` | Health pings every 30s | All agents | ORCHESTRATOR |
| `agents:status` | State changes (idle, working, done) | All agents | ORCHESTRATOR |
| `agents:approvals` | Risky operation requests | Any agent | ORCHESTRATOR |

### Message Formats

**Heartbeat:**

```json
{
  "pane": "implementer",
  "pane_id": "%2",
  "status": "working",
  "current_task": "beads-001",
  "timestamp": "2026-02-20T10:30:00Z"
}
```

**Approval request:**

```json
{
  "id": "approval-001",
  "from": "implementer",
  "operation": "migration",
  "description": "Add IsDeleted column to CatalogItems",
  "command": "dotnet ef migrations add AddSoftDelete...",
  "risk": "high",
  "waiting": true
}
```

## Beads Integration

### Workflow with Beads

```
You: Create epic and tasks in beads

$ bd create --title="Soft-delete for Catalog" --type=epic
$ bd create --title="Add IsDeleted column" --type=task --label=implement
$ bd create --title="Update repository queries" --type=task --label=implement
$ bd create --title="Write soft-delete tests" --type=task --label=test
$ bd dep add beads-003 beads-002  # tests depend on impl

You (to ORCHESTRATOR): Work on epic beads-001

ORCHESTRATOR:
  $ bd show beads-001          # Read epic
  $ bd list --epic=beads-001   # Get child tasks

  Found 3 tasks:
  - beads-002: implement (ready)
  - beads-003: implement (ready)
  - beads-004: test (blocked by beads-002, beads-003)

  Spawning IMPLEMENTER x2 for parallel implement tasks...
```

### Task Labels to Agent Mapping

| Label | Agent |
|-------|-------|
| `implement` | IMPLEMENTER |
| `test` | TESTER |
| `plan` | PLANNER |
| `review` | REVIEWER |

## Health Monitoring and Recovery

### Heartbeat Protocol

Every agent sends a heartbeat every 30 seconds while active. ORCHESTRATOR monitors and respawns unresponsive agents after 2 minutes.

### Recovery Scenarios

| Scenario | Detection | Recovery |
|----------|-----------|----------|
| Agent crashes | No heartbeat for 2 min | `tmux respawn-pane` |
| Agent stuck | Heartbeat but no progress for 10 min | Kill and respawn, reassign task |
| Valkey dies | ORCHESTRATOR loses connection | Alert user, pause workflow |
| Task fails | Agent publishes error | Retry, reassign, or escalate |

### Monitor Script

```bash
#!/bin/bash
# monitor.sh - runs in background

declare -A last_seen

redis-cli SUBSCRIBE agents:heartbeat | while read -r line; do
  pane=$(echo "$line" | jq -r '.pane')
  last_seen[$pane]=$(date +%s)
done &

while true; do
  now=$(date +%s)
  for pane in "${!last_seen[@]}"; do
    age=$((now - last_seen[$pane]))
    if [ $age -gt 120 ]; then
      echo "Agent $pane unresponsive, respawning..."
      tmux respawn-pane -k -t "foundry-agents:$pane" \
        "~/.claude/scripts/agent-launcher.sh $pane"
    fi
  done
  sleep 60
done
```

## Approval Gates

### Operations Requiring Approval

| Operation | Risk Level |
|-----------|------------|
| Delete files | Medium |
| Database migrations | High |
| Schema changes (DROP, ALTER) | High |
| Git push to main | High |
| External API calls (prod) | High |
| Install new dependencies | Medium |

### Approval Flow

1. Agent publishes to `agents:approvals` with operation details
2. ORCHESTRATOR displays approval prompt to user
3. User types `approve <id>` or `deny <id> "reason"`
4. ORCHESTRATOR publishes decision back to channel
5. Agent receives decision, proceeds or aborts

### User Commands

```bash
approve 001          # Approve pending request
deny 001 "reason"    # Deny with explanation
list                 # Show all pending approvals
auto-approve test    # Trust this category for session
```

## Directory Structure

```
~/.claude/
├── agent-roles/                    # Role definitions
│   ├── orchestrator.md
│   ├── planner.md
│   ├── implementer.md
│   ├── tester.md
│   └── reviewer.md
├── agent-workflows/                # Workflow definitions
│   ├── feature.yml
│   ├── bugfix.yml
│   └── parallel-modules.yml
└── scripts/
    ├── swarm-start.sh              # Launch the whole system
    ├── swarm-stop.sh               # Tear down cleanly
    ├── agent-launcher.sh           # Launch individual agent
    ├── monitor.sh                  # Health monitoring
    └── approval-handler.sh         # Interactive approval UI

.agents/                            # Project-specific (optional)
├── config.yml                      # Valkey connection, timeouts
├── roles/                          # Project-specific role tweaks
└── workflows/                      # Custom workflows
```

## Scripts

### swarm-start.sh

```bash
#!/bin/bash
set -e

# Start Valkey if not running
docker start foundry-valkey 2>/dev/null || \
  docker run -d --name foundry-valkey -p 6379:6379 valkey:alpine

# Create tmux session
tmux new-session -d -s foundry-agents -n orchestrator

# Launch ORCHESTRATOR (always on)
tmux send-keys -t foundry-agents:orchestrator \
  "~/.claude/scripts/agent-launcher.sh orchestrator" Enter

# Start health monitor in background
~/.claude/scripts/monitor.sh &

echo "Swarm started. Attach with: tmux attach -t foundry-agents"
```

### agent-launcher.sh

```bash
#!/bin/bash
ROLE=$1
PANE_ID=$(tmux display-message -p '#{pane_id}')

SYSTEM_PROMPT=$(cat <<EOF
$(cat ~/.claude/agent-roles/$ROLE.md)

IDENTITY:
- Role: $ROLE
- Pane ID: $PANE_ID
- Session: foundry-agents

COMMUNICATION:
$(cat ~/.claude/scripts/valkey-commands.md)
EOF
)

claude --system-prompt "$SYSTEM_PROMPT"
```

## Role Definitions

### orchestrator.md

```markdown
# ORCHESTRATOR

You are the central coordinator for a multi-agent development system.

## Responsibilities
- Receive requests from the human user
- Select appropriate workflow (feature, bugfix, refactor, etc.)
- Spawn and manage agent panes
- Monitor agent health via heartbeats
- Handle approval requests for risky operations
- Report final results to user

## Working with Beads
When given an epic:
1. `bd show <epic-id>` - understand the goal
2. `bd list --epic=<epic-id>` - get all tasks
3. `bd ready` - find unblocked tasks
4. Match task labels to agent roles
5. Spawn agents for ready tasks (parallel when independent)
6. Monitor `bd ready` for newly unblocked tasks
7. When all tasks closed, run final review

## Commands
# Spawn agent
tmux split-window -t foundry-agents "~/.claude/scripts/agent-launcher.sh <role>"

# Kill agent
tmux kill-pane -t foundry-agents:<role>

## Never
- Start implementation yourself - delegate to agents
- Approve high-risk operations without user consent
- Kill agents mid-task without reassigning work
```

### planner.md

```markdown
# PLANNER

You break down work into implementable tasks.

## Responsibilities
- Receive high-level requests from ORCHESTRATOR
- Analyze codebase to understand scope
- Create detailed implementation specs
- Define task dependencies
- Create beads issues for each task

## Process
1. Read the request thoroughly
2. Explore relevant code with Glob/Grep/Read
3. Identify all files that need changes
4. Create beads issues with appropriate labels
5. Set up dependencies with `bd dep add`
6. Notify ORCHESTRATOR when planning complete

## Never
- Write implementation code yourself
- Create vague tasks
- Skip acceptance criteria
```

### implementer.md

```markdown
# IMPLEMENTER

You write production code based on specs.

## Workflow
1. Claim task: `bd update <id> --status=in_progress`
2. Read the spec thoroughly
3. Explore existing code patterns
4. Implement the change
5. Run local validation (build, lint)
6. Complete: `bd close <id>`
7. Signal ORCHESTRATOR via Valkey

## Risky Operations (MUST request approval)
- File deletion
- Database migrations
- Schema changes
- Dependency changes

## Heartbeat
Send every 30 seconds:
redis-cli PUBLISH agents:heartbeat '{"pane":"implementer","status":"working",...}'
```

### tester.md

```markdown
# TESTER

You write tests that validate implementations.

## Workflow
1. Claim task: `bd update <id> --status=in_progress`
2. Read the spec and acceptance criteria
3. Review the implementation
4. Write comprehensive tests
5. Run tests to verify they pass
6. Complete: `bd close <id>`

## Standards
- Follow existing test patterns
- Test happy path AND edge cases
- Mock external dependencies
```

### reviewer.md

```markdown
# REVIEWER

You are the quality gate before work is marked complete.

## Workflow
1. Review all code changes
2. Review all test changes
3. Run full test suite
4. Make decision: approve or request changes
5. Signal result to ORCHESTRATOR

## Checklist
- Code matches the spec
- Follows project patterns
- Tests cover acceptance criteria
- All tests pass
- No security issues
```

## Usage

### Starting a Session

```bash
~/.claude/scripts/swarm-start.sh
tmux attach -t foundry-agents
```

### Giving Work

```
You (in ORCHESTRATOR pane): Work on epic beads-001
```

### Navigating Panes

```bash
Ctrl+b w          # Window/pane selector
Ctrl+b q          # Show pane numbers
Ctrl+b arrow      # Navigate between panes
Ctrl+b z          # Zoom current pane
```

### Ending a Session

```bash
~/.claude/scripts/swarm-stop.sh

# Or save state for later
# Prefix + Ctrl+s (save with tmux-resurrect)
# Prefix + Ctrl+r (restore)
```

## Layout Example

```
┌─────────────────────┬─────────────────────┐
│    ORCHESTRATOR     │      PLANNER        │
│                     │                     │
│  [your interaction] │  [planning work]    │
│                     │                     │
├─────────────────────┼─────────────────────┤
│    IMPLEMENTER      │      TESTER         │
│                     │                     │
│  [writing code]     │  [writing tests]    │
│                     │                     │
├─────────────────────┴─────────────────────┤
│                 REVIEWER                   │
│           [reviewing when ready]           │
└───────────────────────────────────────────┘
```

## Future Enhancements

- Dynamic scaling (spawn more IMPLEMENTERs for large epics)
- Specialized agents (SECURITY_REVIEWER, PERF_TESTER)
- Cross-project coordination
- Metrics dashboard for agent performance
- Learning from approval patterns (auto-approve trusted operations)
