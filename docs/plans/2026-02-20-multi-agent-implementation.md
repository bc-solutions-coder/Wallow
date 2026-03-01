# Multi-Agent Orchestration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the scripts and role files needed to run a multi-agent development swarm with tmux, Valkey, and beads integration.

**Architecture:** Shell scripts launch tmux session with ORCHESTRATOR pane, which spawns specialized agents (PLANNER, IMPLEMENTER, TESTER, REVIEWER) as needed. Valkey handles real-time coordination (heartbeats, approvals). Beads handles persistent task management.

**Tech Stack:** Bash scripts, tmux, Valkey (Docker), Claude Code CLI

---

## Task 1: Create Directory Structure

**Files:**
- Create: `~/.claude/agent-roles/` (directory)
- Create: `~/.claude/agent-workflows/` (directory)
- Create: `~/.claude/scripts/` (directory)

**Step 1: Create directories**

```bash
mkdir -p ~/.claude/agent-roles
mkdir -p ~/.claude/agent-workflows
mkdir -p ~/.claude/scripts
```

**Step 2: Verify structure**

Run: `ls -la ~/.claude/ | grep -E "agent-|scripts"`
Expected: Three directories listed

**Step 3: Commit** (skip - no git for ~/.claude)

---

## Task 2: Create ORCHESTRATOR Role File

**Files:**
- Create: `~/.claude/agent-roles/orchestrator.md`

**Step 1: Write orchestrator role**

```markdown
# ORCHESTRATOR

You are the central coordinator for a multi-agent development system.

## Responsibilities
- Receive requests from the human user
- Select appropriate workflow (feature, bugfix, refactor, etc.)
- Spawn and manage agent panes via tmux
- Monitor agent health via Valkey heartbeats
- Handle approval requests for risky operations
- Report final results to user

## Available Workflows
| Workflow | Agents | Use When |
|----------|--------|----------|
| feature | PLANNER → IMPLEMENTER + TESTER → REVIEWER | New functionality |
| bugfix | IMPLEMENTER → TESTER → REVIEWER | Fixing bugs |
| refactor | PLANNER → IMPLEMENTER → REVIEWER | Code restructuring |
| quick | IMPLEMENTER only | Trivial changes |

## Working with Beads
When given an epic or task:
1. `bd show <id>` - understand the goal
2. `bd list --epic=<id>` - get child tasks (if epic)
3. `bd ready` - find unblocked tasks
4. Match task labels to agent roles:
   - `implement` → IMPLEMENTER
   - `test` → TESTER
   - `plan` → PLANNER
   - `review` → REVIEWER
5. Spawn agents for ready tasks (parallel when independent)
6. Monitor `bd ready` for newly unblocked tasks
7. When all tasks closed, run final review

## Spawning Agents
```bash
# Spawn new agent in split pane
tmux split-window -t foundry-agents "~/.claude/scripts/agent-launcher.sh <role>"

# Spawn with horizontal split
tmux split-window -h -t foundry-agents "~/.claude/scripts/agent-launcher.sh <role>"

# Kill agent pane
tmux kill-pane -t foundry-agents:<pane-id>
```

## Monitoring Health
Listen for heartbeats:
```bash
redis-cli SUBSCRIBE agents:heartbeat
```
If no heartbeat from agent for 2 minutes, respawn it.

## Handling Approvals
When you receive on `agents:approvals`:
1. Display the request clearly to user
2. Wait for user input: `approve <id>` or `deny <id> "reason"`
3. Publish decision: `redis-cli PUBLISH agents:approvals '{"id":"<id>","approved":true}'`

## Rules
- NEVER implement code yourself - delegate to agents
- NEVER approve high-risk operations without user consent
- NEVER kill agents mid-task without reassigning work
- ALWAYS report status when workflows complete
```

**Step 2: Verify file created**

Run: `head -20 ~/.claude/agent-roles/orchestrator.md`
Expected: Shows the ORCHESTRATOR header and responsibilities

---

## Task 3: Create PLANNER Role File

**Files:**
- Create: `~/.claude/agent-roles/planner.md`

**Step 1: Write planner role**

```markdown
# PLANNER

You break down work into implementable tasks.

## Responsibilities
- Receive high-level requests from ORCHESTRATOR
- Analyze codebase to understand scope
- Create detailed implementation specs
- Define task dependencies
- Create beads issues for each task

## Planning Process
1. Read the request thoroughly
2. Explore relevant code with Glob/Grep/Read
3. Identify all files that need changes
4. Break into smallest independent tasks
5. Define clear acceptance criteria for each
6. Create beads issues with appropriate labels

## Creating Tasks in Beads
```bash
# Create implementation task
bd create --title="<clear description>" --type=task --label=implement \
  --description="<detailed spec with acceptance criteria>"

# Create test task
bd create --title="Write tests for <feature>" --type=task --label=test \
  --description="<what to test, edge cases to cover>"

# Set dependencies
bd dep add <child-id> <parent-id>  # child depends on parent
```

## Task Spec Format
Each task description should include:
- **Goal**: What this task accomplishes
- **Files**: Which files to create/modify
- **Approach**: How to implement it
- **Acceptance Criteria**: How to verify it's done

## Signaling Completion
When planning is complete:
```bash
redis-cli PUBLISH agents:status '{"pane":"planner","event":"plan_complete","tasks":["beads-001","beads-002"]}'
```

## Rules
- NEVER write implementation code yourself
- NEVER create vague tasks like "fix the bug"
- ALWAYS include acceptance criteria
- ALWAYS set up task dependencies correctly

## Heartbeat
Send every 30 seconds while working:
```bash
redis-cli PUBLISH agents:heartbeat '{"pane":"planner","status":"working","timestamp":"'$(date -Iseconds)'"}'
```
```

**Step 2: Verify file created**

Run: `head -20 ~/.claude/agent-roles/planner.md`
Expected: Shows the PLANNER header and responsibilities

---

## Task 4: Create IMPLEMENTER Role File

**Files:**
- Create: `~/.claude/agent-roles/implementer.md`

**Step 1: Write implementer role**

```markdown
# IMPLEMENTER

You write production code based on specs from PLANNER.

## Responsibilities
- Claim tasks from beads queue
- Write clean code following project patterns
- Request approval for risky operations
- Signal completion when done

## Workflow
1. Check for assigned work: `bd list --status=in_progress --assignee=implementer`
2. If none, check ready work: `bd ready --label=implement`
3. Claim task: `bd update <id> --status=in_progress --assignee=implementer`
4. Read the spec: `bd show <id>`
5. Explore existing code patterns
6. Implement the change
7. Run local validation: `dotnet build && dotnet test`
8. Complete: `bd close <id>`
9. Signal: `redis-cli PUBLISH agents:status '{"pane":"implementer","event":"task_complete","task":"<id>"}'`
10. Return to step 1

## Risky Operations (MUST request approval)
Before executing any of these, request approval:
- File deletion
- Database migrations
- Schema changes (DROP, ALTER)
- Dependency changes (adding packages)
- Git push to main
- Anything touching auth/billing/payments

## Requesting Approval
```bash
redis-cli PUBLISH agents:approvals '{
  "id": "approval-'$(date +%s)'",
  "from": "implementer",
  "operation": "<type>",
  "description": "<what you want to do>",
  "command": "<exact command>",
  "risk": "high",
  "waiting": true
}'
```
Then WAIT for approval response before proceeding.

## Code Standards
- Follow existing patterns in the codebase
- Keep changes minimal and focused
- Don't refactor unrelated code
- Add comments only where logic isn't obvious
- Run `dotnet format` before completing

## Heartbeat
Send every 30 seconds while working:
```bash
redis-cli PUBLISH agents:heartbeat '{"pane":"implementer","status":"working","current_task":"<id>","timestamp":"'$(date -Iseconds)'"}'
```

## Rules
- NEVER skip approval for risky operations
- NEVER make changes outside the task scope
- ALWAYS run build before marking complete
```

**Step 2: Verify file created**

Run: `head -20 ~/.claude/agent-roles/implementer.md`
Expected: Shows the IMPLEMENTER header and responsibilities

---

## Task 5: Create TESTER Role File

**Files:**
- Create: `~/.claude/agent-roles/tester.md`

**Step 1: Write tester role**

```markdown
# TESTER

You write tests that validate implementations.

## Responsibilities
- Write unit tests for new code
- Write integration tests where appropriate
- Ensure edge cases are covered
- Validate acceptance criteria from specs

## Workflow
1. Check for assigned work: `bd list --status=in_progress --assignee=tester`
2. If none, check ready work: `bd ready --label=test`
3. Claim task: `bd update <id> --status=in_progress --assignee=tester`
4. Read the spec: `bd show <id>`
5. Review the implementation (read the changed files)
6. Write comprehensive tests
7. Run tests: `dotnet test`
8. Complete: `bd close <id>`
9. Signal: `redis-cli PUBLISH agents:status '{"pane":"tester","event":"task_complete","task":"<id>"}'`
10. Return to step 1

## Test Standards
- Follow existing test patterns in the codebase
- Use descriptive test names that explain the scenario
- Test happy path AND edge cases
- Mock external dependencies
- Aim for meaningful coverage, not 100%

## Test Structure (xUnit)
```csharp
public class FeatureTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var sut = new SystemUnderTest();

        // Act
        var result = sut.Method(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

## Project Test Commands
```bash
dotnet test                                      # All tests
dotnet test tests/Modules/Catalog/...           # Module tests
dotnet test --filter "FullyQualifiedName~Name"  # Filtered
dotnet test --collect:"XPlat Code Coverage"     # With coverage
```

## Heartbeat
Send every 30 seconds while working:
```bash
redis-cli PUBLISH agents:heartbeat '{"pane":"tester","status":"working","current_task":"<id>","timestamp":"'$(date -Iseconds)'"}'
```

## Rules
- NEVER write implementation code (only tests)
- NEVER skip edge case testing
- NEVER leave tests that don't compile or run
- ALWAYS verify tests pass before completing
```

**Step 2: Verify file created**

Run: `head -20 ~/.claude/agent-roles/tester.md`
Expected: Shows the TESTER header and responsibilities

---

## Task 6: Create REVIEWER Role File

**Files:**
- Create: `~/.claude/agent-roles/reviewer.md`

**Step 1: Write reviewer role**

```markdown
# REVIEWER

You are the quality gate before work is marked complete.

## Responsibilities
- Review code changes for correctness
- Verify tests are comprehensive
- Check adherence to project patterns
- Run full test suite
- Approve or request changes

## Workflow
1. Receive review request from ORCHESTRATOR
2. Read the original spec/task: `bd show <id>`
3. Review all code changes: `git diff main...HEAD`
4. Review all test changes
5. Run full test suite: `dotnet test`
6. Make decision: approve or request changes
7. Signal result to ORCHESTRATOR

## Review Checklist
- [ ] Code matches the spec requirements
- [ ] No unrelated changes included
- [ ] Follows project patterns (check similar code)
- [ ] Error handling is appropriate
- [ ] Tests cover acceptance criteria
- [ ] Tests cover edge cases
- [ ] All tests pass
- [ ] No obvious security issues
- [ ] No performance red flags

## Signaling Approval
```bash
redis-cli PUBLISH agents:status '{
  "pane": "reviewer",
  "event": "review_complete",
  "task": "<id>",
  "status": "approved",
  "notes": "<any notes>"
}'
```

## Requesting Changes
```bash
redis-cli PUBLISH agents:status '{
  "pane": "reviewer",
  "event": "review_complete",
  "task": "<id>",
  "status": "changes_requested",
  "issues": ["issue 1", "issue 2"]
}'
```

## Heartbeat
Send every 30 seconds while working:
```bash
redis-cli PUBLISH agents:heartbeat '{"pane":"reviewer","status":"working","current_task":"<id>","timestamp":"'$(date -Iseconds)'"}'
```

## Rules
- NEVER approve work that doesn't meet criteria
- NEVER fix code yourself (send back to IMPLEMENTER)
- NEVER skip running the test suite
- ALWAYS provide specific feedback when requesting changes
```

**Step 2: Verify file created**

Run: `head -20 ~/.claude/agent-roles/reviewer.md`
Expected: Shows the REVIEWER header and responsibilities

---

## Task 7: Create Workflow Definitions

**Files:**
- Create: `~/.claude/agent-workflows/feature.yml`
- Create: `~/.claude/agent-workflows/bugfix.yml`
- Create: `~/.claude/agent-workflows/quick.yml`

**Step 1: Write feature workflow**

```yaml
name: feature
description: Full feature development pipeline

steps:
  - name: planning
    spawn: planner
    wait_for: plan_complete

  - name: implementation
    spawn:
      - implementer
      - tester
    parallel: true
    wait_for:
      - task_complete from implementer
      - task_complete from tester

  - name: review
    spawn: reviewer
    wait_for: review_complete
    on_changes_requested:
      goto: implementation
```

**Step 2: Write bugfix workflow**

```yaml
name: bugfix
description: Bug fix with testing (skip planning)

steps:
  - name: implementation
    spawn: implementer
    wait_for: task_complete

  - name: testing
    spawn: tester
    wait_for: task_complete

  - name: review
    spawn: reviewer
    wait_for: review_complete
    on_changes_requested:
      goto: implementation
```

**Step 3: Write quick workflow**

```yaml
name: quick
description: Quick fix, single implementer only

steps:
  - name: implementation
    spawn: implementer
    wait_for: task_complete
```

**Step 4: Verify files created**

Run: `ls -la ~/.claude/agent-workflows/`
Expected: Three .yml files listed

---

## Task 8: Create Agent Launcher Script

**Files:**
- Create: `~/.claude/scripts/agent-launcher.sh`

**Step 1: Write launcher script**

```bash
#!/bin/bash
# agent-launcher.sh - Launch a Claude Code agent with role-specific context
set -e

ROLE=$1
if [ -z "$ROLE" ]; then
    echo "Usage: agent-launcher.sh <role>"
    echo "Roles: orchestrator, planner, implementer, tester, reviewer"
    exit 1
fi

ROLE_FILE="$HOME/.claude/agent-roles/$ROLE.md"
if [ ! -f "$ROLE_FILE" ]; then
    echo "Error: Role file not found: $ROLE_FILE"
    exit 1
fi

PANE_ID=$(tmux display-message -p '#{pane_id}' 2>/dev/null || echo "standalone")

# Build system prompt
SYSTEM_PROMPT=$(cat <<EOF
$(cat "$ROLE_FILE")

---

## Session Identity
- **Role**: $ROLE
- **Pane ID**: $PANE_ID
- **Session**: foundry-agents
- **Started**: $(date -Iseconds)

## Valkey Commands Reference
\`\`\`bash
# Send heartbeat (do this every 30 seconds while working)
redis-cli PUBLISH agents:heartbeat '{"pane":"$ROLE","status":"working","timestamp":"'\$(date -Iseconds)'"}'

# Listen for messages (ORCHESTRATOR does this)
redis-cli SUBSCRIBE agents:heartbeat agents:status agents:approvals

# Publish status update
redis-cli PUBLISH agents:status '{"pane":"$ROLE","event":"<event>","data":{...}}'

# Request approval (for risky operations)
redis-cli PUBLISH agents:approvals '{"id":"approval-'\$(date +%s)'","from":"$ROLE","operation":"...","waiting":true}'
\`\`\`

## Beads Commands Reference
\`\`\`bash
bd ready                    # Find available work
bd show <id>                # View task details
bd update <id> --status=in_progress --assignee=$ROLE  # Claim task
bd close <id>               # Complete task
bd list --epic=<id>         # List tasks in epic
\`\`\`
EOF
)

# Launch Claude Code with the system prompt
echo "Launching $ROLE agent..."
claude --system-prompt "$SYSTEM_PROMPT"
```

**Step 2: Make executable**

Run: `chmod +x ~/.claude/scripts/agent-launcher.sh`

**Step 3: Verify script**

Run: `~/.claude/scripts/agent-launcher.sh 2>&1 | head -5`
Expected: Shows usage message (no role provided)

---

## Task 9: Create Swarm Start Script

**Files:**
- Create: `~/.claude/scripts/swarm-start.sh`

**Step 1: Write swarm start script**

```bash
#!/bin/bash
# swarm-start.sh - Launch the multi-agent swarm
set -e

SESSION="foundry-agents"
SCRIPTS_DIR="$HOME/.claude/scripts"

echo "=== Starting Foundry Agent Swarm ==="

# Check for Valkey, start if needed
echo "Checking Valkey..."
if ! docker ps | grep -q foundry-valkey; then
    if docker ps -a | grep -q foundry-valkey; then
        echo "Starting existing Valkey container..."
        docker start foundry-valkey
    else
        echo "Creating new Valkey container..."
        docker run -d --name foundry-valkey -p 6379:6379 valkey:alpine
    fi
fi

# Verify Valkey is responding
sleep 1
if ! redis-cli ping > /dev/null 2>&1; then
    echo "Error: Valkey not responding"
    exit 1
fi
echo "Valkey: OK"

# Kill existing session if present
if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Killing existing session..."
    tmux kill-session -t "$SESSION"
fi

# Create new tmux session with ORCHESTRATOR
echo "Creating tmux session..."
tmux new-session -d -s "$SESSION" -n main

# Set up nice layout
tmux set-option -t "$SESSION" -g mouse on

# Launch ORCHESTRATOR in the first pane
echo "Launching ORCHESTRATOR..."
tmux send-keys -t "$SESSION:main" "$SCRIPTS_DIR/agent-launcher.sh orchestrator" Enter

echo ""
echo "=== Swarm Started ==="
echo ""
echo "Attach with:  tmux attach -t $SESSION"
echo ""
echo "Pane navigation:"
echo "  Ctrl+b w     - Window/pane selector"
echo "  Ctrl+b q     - Show pane numbers"
echo "  Ctrl+b arrow - Navigate panes"
echo "  Ctrl+b z     - Zoom current pane"
echo ""
```

**Step 2: Make executable**

Run: `chmod +x ~/.claude/scripts/swarm-start.sh`

**Step 3: Verify script syntax**

Run: `bash -n ~/.claude/scripts/swarm-start.sh && echo "Syntax OK"`
Expected: "Syntax OK"

---

## Task 10: Create Swarm Stop Script

**Files:**
- Create: `~/.claude/scripts/swarm-stop.sh`

**Step 1: Write swarm stop script**

```bash
#!/bin/bash
# swarm-stop.sh - Gracefully stop the multi-agent swarm
set -e

SESSION="foundry-agents"

echo "=== Stopping Foundry Agent Swarm ==="

# Kill tmux session
if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Killing tmux session..."
    tmux kill-session -t "$SESSION"
    echo "Session killed."
else
    echo "No active session found."
fi

# Optionally stop Valkey (comment out to keep Valkey running)
# echo "Stopping Valkey..."
# docker stop foundry-valkey

echo ""
echo "=== Swarm Stopped ==="
echo ""
echo "Note: Valkey container is still running."
echo "To stop Valkey: docker stop foundry-valkey"
echo ""
```

**Step 2: Make executable**

Run: `chmod +x ~/.claude/scripts/swarm-stop.sh`

**Step 3: Verify script syntax**

Run: `bash -n ~/.claude/scripts/swarm-stop.sh && echo "Syntax OK"`
Expected: "Syntax OK"

---

## Task 11: Create Health Monitor Script

**Files:**
- Create: `~/.claude/scripts/monitor.sh`

**Step 1: Write monitor script**

```bash
#!/bin/bash
# monitor.sh - Monitor agent health and respawn dead agents
set -e

SESSION="foundry-agents"
SCRIPTS_DIR="$HOME/.claude/scripts"
TIMEOUT_SECONDS=120  # 2 minutes without heartbeat = dead

declare -A last_seen
declare -A pane_ids

log() {
    echo "[$(date '+%H:%M:%S')] $1"
}

respawn_agent() {
    local role=$1
    local pane=${pane_ids[$role]}

    log "ALERT: Agent '$role' unresponsive, respawning..."

    if [ -n "$pane" ]; then
        tmux respawn-pane -k -t "$SESSION:$pane" "$SCRIPTS_DIR/agent-launcher.sh $role" 2>/dev/null || \
        tmux split-window -t "$SESSION" "$SCRIPTS_DIR/agent-launcher.sh $role"
    else
        tmux split-window -t "$SESSION" "$SCRIPTS_DIR/agent-launcher.sh $role"
    fi

    log "Agent '$role' respawned"
}

log "Starting health monitor (timeout: ${TIMEOUT_SECONDS}s)"
log "Listening for heartbeats on agents:heartbeat..."

# Background process to check for dead agents
(
    while true; do
        sleep 30
        now=$(date +%s)
        for role in "${!last_seen[@]}"; do
            age=$((now - last_seen[$role]))
            if [ $age -gt $TIMEOUT_SECONDS ]; then
                respawn_agent "$role"
                unset last_seen[$role]
            fi
        done
    done
) &
CHECKER_PID=$!

# Cleanup on exit
trap "kill $CHECKER_PID 2>/dev/null" EXIT

# Listen for heartbeats
redis-cli SUBSCRIBE agents:heartbeat | while read -r type; do
    read -r channel
    read -r message

    if [ "$type" = "message" ]; then
        # Parse JSON (simple extraction)
        pane=$(echo "$message" | grep -o '"pane":"[^"]*"' | cut -d'"' -f4)
        pane_id=$(echo "$message" | grep -o '"pane_id":"[^"]*"' | cut -d'"' -f4)
        status=$(echo "$message" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)

        if [ -n "$pane" ]; then
            last_seen[$pane]=$(date +%s)
            pane_ids[$pane]=$pane_id
            log "Heartbeat: $pane ($status)"
        fi
    fi
done
```

**Step 2: Make executable**

Run: `chmod +x ~/.claude/scripts/monitor.sh`

**Step 3: Verify script syntax**

Run: `bash -n ~/.claude/scripts/monitor.sh && echo "Syntax OK"`
Expected: "Syntax OK"

---

## Task 12: Test the Setup

**Step 1: Verify all files exist**

Run:
```bash
echo "=== Role Files ===" && ls -la ~/.claude/agent-roles/
echo "=== Workflow Files ===" && ls -la ~/.claude/agent-workflows/
echo "=== Scripts ===" && ls -la ~/.claude/scripts/
```

Expected: All files listed with correct permissions (scripts should be executable)

**Step 2: Test Valkey connection**

Run:
```bash
docker start foundry-valkey 2>/dev/null || docker run -d --name foundry-valkey -p 6379:6379 valkey:alpine
sleep 2
redis-cli ping
```

Expected: "PONG"

**Step 3: Test agent launcher (dry run)**

Run: `~/.claude/scripts/agent-launcher.sh orchestrator --help 2>&1 | head -3 || echo "Launcher works (Claude would start here)"`

Expected: Either Claude help or the echo message

**Step 4: Document completion**

The multi-agent system is now ready. To use:

```bash
# Start the swarm
~/.claude/scripts/swarm-start.sh

# Attach to watch
tmux attach -t foundry-agents

# In ORCHESTRATOR pane, give work:
# "Work on epic beads-001" or "Add soft-delete to Catalog"

# Stop when done
~/.claude/scripts/swarm-stop.sh
```

---

## Summary

| Component | File |
|-----------|------|
| ORCHESTRATOR role | `~/.claude/agent-roles/orchestrator.md` |
| PLANNER role | `~/.claude/agent-roles/planner.md` |
| IMPLEMENTER role | `~/.claude/agent-roles/implementer.md` |
| TESTER role | `~/.claude/agent-roles/tester.md` |
| REVIEWER role | `~/.claude/agent-roles/reviewer.md` |
| Feature workflow | `~/.claude/agent-workflows/feature.yml` |
| Bugfix workflow | `~/.claude/agent-workflows/bugfix.yml` |
| Quick workflow | `~/.claude/agent-workflows/quick.yml` |
| Agent launcher | `~/.claude/scripts/agent-launcher.sh` |
| Swarm start | `~/.claude/scripts/swarm-start.sh` |
| Swarm stop | `~/.claude/scripts/swarm-stop.sh` |
| Health monitor | `~/.claude/scripts/monitor.sh` |
