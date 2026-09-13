---
name: code-review
description: Review pull requests in mcp-windows for concrete bugs in MCP and CLI contracts, Windows UI automation, element identity, snapshots, bounded searches, and service lifetime. Use for pull request reviews and re-reviews, including changes to batches, macros, tests, and guidance.
---

# Review Windows MCP changes

Review for observable correctness and user impact, not preferred coding style.
Do not edit product code, merge pull requests, or change repository settings during a review.

## Establish the intended behavior

1. Read the current PR head, diff, linked issues, and acceptance criteria. Distinguish
   deliberate breaking changes from accidental regressions; do not demand compatibility
   that the change explicitly excludes.
2. Follow [repository instructions](../../copilot-instructions.md) and
   [testing guidance](../../testing.instructions.md). Use the existing implementation
   and tests to confirm what the public interface actually promises.
3. Trace affected callers and result consumers, not just the edited method. Relevant
   surfaces include tool registration and schemas, raw request validation, CLI parsing
   and dispatch, shared automation services, batches, macro save/replay, result models,
   recovery hints, help, and examples.
4. For re-reviews, inspect existing threads and the complete review summaries, including
   suppressed findings. Check each against the latest code. Do not repeat a fixed finding
   or assume that an unresolved thread still represents a bug.

## Prioritize these failure modes

| Area | What to verify |
|---|---|
| Input contracts | Omitted, null, empty, whitespace, unknown, and mode-inappropriate arguments remain distinguishable where the contract requires it. A schema alone does not prove the runtime rejects an argument. Check validation before binding erases property presence. |
| MCP/CLI parity | Both entry points reach the same operation behavior. CLI aliases, quoting, child arguments, relative paths, working directories, output, and exit codes preserve intent. Do not infer an alias is invalid without checking the parser. |
| Target identity | An ID resolves the original observed control or fails. Replacement, handle/process reuse, provider failure, owner restart, eviction, and automation-thread disposal must not redirect it through a name or position search. |
| Read and action scope | A failed explicit target must not widen into whole-window text, OCR, another table, or an unrelated dialog. Parent/proximity references and coordinate fallbacks must belong to the intended window and control. |
| Batches and macros | Validate the entire request before side effects. Save-time and replay validation must agree. Null steps and invalid property presence are handled. Only an unambiguous discovery result may supply a later action's reference. |
| Bounded work | Enforce limits before bulk fetching or allocating results, not afterward. Incomplete searches cannot prove absence or uniqueness. Check deadlines, final probes, cancellation, and cleanup without arbitrary timeout padding. |
| Service lifetime | Check caller isolation, pipe permissions, startup coordination, build compatibility, request/output limits, and resource ownership. Whole batches must not interleave; read-only waits must not block their triggering actions. Do not replay requests after an uncertain response. |
| Windows input and outcomes | Verify foreground ownership, focus, current coordinates/DPI, and input return values before acting. Dispatch, dialog disappearance, and an actual application outcome are different evidence. Cleanup must not close user applications or release another operation's resources. |
| Snapshots | Differences must refer to the caller's known baseline and reconstruct the current tree, including actionable IDs. Missing or outdated baselines require a full response. Do not preserve an old ID by assigning it to a replacement control. |
| Measurements and guidance | Compare actual serialized outputs using equivalent captures and metadata. Do not relabel historical benchmarks. Verify examples and recovery hints against current argument names, validation, and MCP/CLI ownership. |

MCP instances and the CLI daemon intentionally have separate runtime state. Shared
implementation does not imply shared sessions. Consult the architecture guidance in
the repository before proposing another service or session abstraction.

## Gather evidence safely

- Use available read-only GitHub context for related code, issues, and CI results.
  Verify that a result covers the current commit; a green older run is not evidence
  for new changes. If context or tools are unavailable, state that limitation.
- Consult FlaUI and pywinauto for changed Windows automation, modal-dialog, and wait
  behavior. Use the sister-project references in the repository instructions for
  cross-cutting service patterns; do not assume another project's comments prove
  its implementation or that Office-specific behavior applies here.
- Start with the smallest relevant existing tests when execution is available.
  Prefer real SDK invocation and separate CLI-process regressions for boundary bugs.
  Pure unit tests are useful for parsing, budgets, comparisons, and controlled clocks.
- Never connect review tooling to a live user desktop or enable desktop-input tests
  on a shared machine. Use dedicated Windows CI evidence for mouse/keyboard behavior.
  Do not run paid LLM tests or Office/browser benchmarks without explicit permission.
- Do not weaken assertions, insert tool hints into task-focused LLM prompts, blindly
  rerun side-effecting operations, or label a failure harmless because a retry passed.

## Report actionable findings

For each finding, identify the triggering input or state, the actual versus required
behavior, its user impact, and the relevant code path or regression evidence. Attach
the finding to an appropriate changed line and describe a focused correction.

Report concrete issues introduced or materially worsened by the change. Follow enough
surrounding code to rule out existing guards before commenting. Avoid speculative
redesigns, style-only findings, duplicate symptoms of one root cause, and unrelated
pre-existing problems. Do not invent findings to meet a quota.

Use plain English. Distinguish confirmed bugs, unverified risks, and unavailable
validation. If no actionable issue is found, say so without implying unrun tests passed.
