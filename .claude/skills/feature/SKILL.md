---
name: feature
description: Run the cfeed feature pipeline (analyze, plan, approve, implement, test, document) for a requirement or bug fix. Use when the user gives a new requirement, feature or fix that touches more than one file.
argument-hint: <requirement or bug description>
---

Orchestrate the pipeline for this requirement: **$ARGUMENTS**

You are the orchestrator. Delegate each stage to its agent with the Agent tool (`subagent_type` = the agent name). Agents hand off through files in `.pipeline/`; pass them the requirement text and tell them which files to read. Do not do the stages' work yourself.

## 0. Preflight
1. Run `git status --short`. If there are uncommitted changes other than `.pipeline/` and `.vscode/`, tell the user and ask whether to continue (the documenter diffs against `HEAD`).
2. Run `powershell -File scripts\test.ps1`. If the baseline does not build or pass, stop and report; do not start a feature on a broken baseline.
3. Create `.pipeline/` if missing and clear old stage files.

## 1. Analyze
Run `analyzer` with the requirement. If its output has blocking **Open questions**, ask the user (AskUserQuestion) and re-run with the answers.

## 2. Plan
Run `planner`. If it listed open questions, ask the user and re-run.

## 3. Approval gate
Show the user the **Goal, Tasks and Acceptance criteria** from `.pipeline/02-plan.md` (summarize; link the file). Ask for approval. Do not continue without a clear yes. If the user requests changes, re-run `planner` with their feedback.

## 4. Implement, then test (max 3 rounds)
Repeat:
1. Run `implementer` (on rounds 2 and 3 tell it to fix the failures in `.pipeline/03-test-report.md`).
2. Run `tester`.
3. If `03-test-report.md` says `PASS`, go on to step 5.
4. If it says `FAIL` after round 3, stop. Show the user the failures and ask how to proceed.

If the tester diagnoses a *test* bug, the tester fixes it within the round. Only code bugs go back to the implementer.

## 5. Document
Run `documenter`.

## 6. Report
Give the user a short summary: what changed (from `git status`/`git diff --stat`), test totals, `[manual]` criteria they still need to verify, and anything not automated. Do **not** commit; ask if they want a commit.

## Rules
- Build and test only through `scripts\build.ps1` and `scripts\test.ps1`.
- Never skip the approval gate, even for small changes.
- Keep your own messages short; the details live in `.pipeline/*.md`.
