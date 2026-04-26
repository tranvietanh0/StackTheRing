---

origin: theonekit-core
repository: The1Studio/theonekit-core
module: null
protected: true
---
# Security Rules

## Secret Protection

- **NEVER** stage `.env`, `.pem`, `.key`, `credentials.*`, `secrets.yml`, SSH keys, or service account files
- **ALWAYS** use `.env.example` or `.env.template` for documenting required variables (without values)
- **ALWAYS** check `git status` before committing to verify no sensitive files are staged
- **NEVER** hardcode API keys, tokens, passwords, or connection strings in source code
- Use environment variables or config files (gitignored) for all secrets

## Hooks (Auto-enforced)

Two security hooks ship with TheOneKit core and are registered in `.claude/settings.json`:

1. **`privacy-guard.cjs`** (PreToolUse: Read/Glob/Grep) — blocks reading sensitive files, requires user approval
2. **`secret-guard.cjs`** (PreToolUse: Bash) — hard-blocks staging/committing/pushing sensitive files

These hooks are fail-open (if they crash, the action is allowed) to avoid breaking dev workflows.

## When Working with Secrets

1. Ask user which env vars are needed
2. Create `.env.example` with placeholder values
3. Instruct user to create `.env` locally with real values
4. Ensure `.env` is in `.gitignore`
