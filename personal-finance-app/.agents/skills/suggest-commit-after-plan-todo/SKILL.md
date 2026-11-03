---
name: suggest-commit-after-plan-todo
description: Suggest a concise Conventional Commit subject after Codex finishes implementing an assigned plan, checklist item, or milestone step. Use for completed work tracked in plan.md or todo.md; never create the commit unless the user asks.
---

# Suggest Commit After Plan Todo

After fully implementing an assigned plan or checklist item, end the completion
reply with one suggested commit subject. Do not add the suggestion to partial
progress updates or when the task remains incomplete.

Do not create a commit unless the user explicitly requests it.

## Subject

Use this format:

```text
type: short imperative summary
```

- Keep the entire subject at or below 72 characters, including the type, colon,
  and spaces.
- Use a lowercase Conventional Commit type appropriate to the completed diff,
  normally `feat`, `fix`, `refactor`, `test`, `chore`, or `docs`.
- Summarize the intent of the completed item, not the broader plan.
- Use an imperative summary with no trailing period.
- Do not add a body unless the user requests one.

## Completion Reply

Place the suggestion at the end of the final reply, clearly labeled. Put the
subject in a standalone, untagged fenced code block so the interface provides a
copy control:

**Suggested commit:**

```
refactor: centralize Data Dragon transport
```
