---
description: After a plan todo, suggest a conventional commit message (≤72 chars)
alwaysApply: true
---

# Suggest commit message after plan todos

When you finish implementing an assigned plan todo (or milestone step), end your reply with a **suggested commit message**. Do not create the commit unless the user asks.

## Format

```
type: short imperative summary
```

- **Max 72 characters** for the whole subject line (including `type:` and spaces).
- Match this repo’s style: lowercase `type`, colon, space, imperative summary.
- Prefer common types used here: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`.
- Describe the intent of **this todo’s diff**, not the whole plan.
- No trailing period. No body unless the user asks for one.

## Examples (from this repo)

```
refactor: add discovery folder and move scope helpers there
refactor: move outcomes UI into prediction-outcome feature
test: enforce server architecture import boundaries
feat: move analyst stats into research-context
```

## Placement

Put it at the end of the completion reply, clearly labeled. Use a **standalone fenced code block** (not inline backticks) so the chat UI shows a one-click copy control on the message:

**Suggested commit:**

```
refactor: extract discovery-scope helpers with tests
```

Do not wrap the message in a language-tagged fence (e.g. avoid ` ```text `) unless needed — a plain fence is enough for the copy icon.
