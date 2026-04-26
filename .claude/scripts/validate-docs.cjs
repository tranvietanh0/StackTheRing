#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
// validate-docs.cjs — Standalone script: validate docs/ for broken links, orphans, stale content
// Usage: node .claude/scripts/validate-docs.cjs
// Exit 0: no issues. Exit 1: issues found.
'use strict';

const fs = require('fs');
const path = require('path');

// Resolve project root (two levels up from .claude/scripts/)
const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
const DOCS_DIR = path.join(PROJECT_ROOT, 'docs');
const STALE_DAYS = 90;

const issues = [];

// ── Helpers ────────────────────────────────────────────────────────────────

function addIssue(category, file, message) {
  issues.push({ category, file, message });
}

function readDir(dir) {
  try {
    return fs.readdirSync(dir).filter(f => f.endsWith('.md'));
  } catch {
    return [];
  }
}

function readFile(filePath) {
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch {
    return null;
  }
}

function getMtimeDays(filePath) {
  try {
    const stat = fs.statSync(filePath);
    return (Date.now() - stat.mtimeMs) / (1000 * 60 * 60 * 24);
  } catch {
    return 0;
  }
}

// ── Check 1: Broken internal links ────────────────────────────────────────
// Scans all docs/*.md for [text](path) links, verifies targets exist.

function checkBrokenLinks(docFiles) {
  // Regex matches markdown links: [text](target) — captures target
  const LINK_RE = /\[([^\]]*)\]\(([^)]+)\)/g;

  for (const file of docFiles) {
    const filePath = path.join(DOCS_DIR, file);
    const content = readFile(filePath);
    if (!content) continue;

    let match;
    while ((match = LINK_RE.exec(content)) !== null) {
      const target = match[2];

      // Skip external links (http/https/mailto) and anchors-only
      if (/^https?:\/\//.test(target) || /^mailto:/.test(target)) continue;
      if (target.startsWith('#')) continue;

      // Strip anchor fragment for file existence check
      const [filePart] = target.split('#');
      if (!filePart) continue;

      // Resolve relative to the doc file's directory
      const resolved = path.resolve(DOCS_DIR, filePart);

      if (!fs.existsSync(resolved)) {
        addIssue('broken-link', file, `broken link → '${target}' (resolved: ${resolved})`);
      }
    }
  }
}

// ── Check 2: Orphan docs ──────────────────────────────────────────────────
// Files in docs/ not referenced from any other doc.

function checkOrphanDocs(docFiles) {
  // Build set of all link targets across all docs
  const LINK_RE = /\[([^\]]*)\]\(([^)]+)\)/g;
  const referenced = new Set();

  for (const file of docFiles) {
    const filePath = path.join(DOCS_DIR, file);
    const content = readFile(filePath);
    if (!content) continue;

    let match;
    while ((match = LINK_RE.exec(content)) !== null) {
      const target = match[2];
      if (/^https?:\/\//.test(target) || target.startsWith('#')) continue;
      const [filePart] = target.split('#');
      if (!filePart) continue;
      // Normalize to basename for simple cross-reference check
      referenced.add(path.basename(filePart));
    }
  }

  // Also check CLAUDE.md in project root for references
  const claudeMd = readFile(path.join(PROJECT_ROOT, 'CLAUDE.md'));
  if (claudeMd) {
    let match;
    const re = /\[([^\]]*)\]\(([^)]+)\)/g;
    while ((match = re.exec(claudeMd)) !== null) {
      const target = match[2];
      if (/^https?:\/\//.test(target) || target.startsWith('#')) continue;
      const [filePart] = target.split('#');
      if (filePart) referenced.add(path.basename(filePart));
    }
  }

  for (const file of docFiles) {
    // Skip index-like files that are expected to not be linked
    if (file === 'README.md') continue;
    if (!referenced.has(file)) {
      addIssue('orphan', file, `orphan doc — not referenced from any other doc or CLAUDE.md`);
    }
  }
}

// ── Check 3: Stale content ────────────────────────────────────────────────
// Docs not modified in >STALE_DAYS days that reference files changed recently.

function checkStaleContent(docFiles) {
  for (const file of docFiles) {
    const filePath = path.join(DOCS_DIR, file);
    const ageDays = getMtimeDays(filePath);
    if (ageDays <= STALE_DAYS) continue;

    // Check if doc references source files that have changed recently
    const content = readFile(filePath);
    if (!content) continue;

    // Look for references to source paths (e.g., `.claude/`, `docs/`, `src/`)
    const PATH_RE = /`([^`]+\.[a-z]{2,5})`/g;
    let match;
    while ((match = PATH_RE.exec(content)) !== null) {
      const ref = match[1];
      // Only check paths that look like relative project paths
      if (ref.startsWith('.') || ref.startsWith('src/') || ref.startsWith('.claude/')) {
        const resolved = path.join(PROJECT_ROOT, ref);
        try {
          const refAgeDays = getMtimeDays(resolved);
          if (refAgeDays < STALE_DAYS / 3) {
            // Referenced file changed recently but doc is stale
            addIssue('stale', file,
              `stale doc (${Math.floor(ageDays)}d old) references recently-changed file '${ref}' (${Math.floor(refAgeDays)}d old)`);
            break; // one warning per doc is enough
          }
        } catch {
          // Referenced file doesn't exist — broken link check handles this
        }
      }
    }
  }
}

// ── Check 4: Missing required sections ───────────────────────────────────
// agents.md must list all agent .md files in .claude/agents/

function checkMissingSections(docFiles) {
  // Check agents.md covers all agents
  if (docFiles.includes('agents.md')) {
    const agentsDoc = readFile(path.join(DOCS_DIR, 'agents.md'));
    const agentsDir = path.join(PROJECT_ROOT, '.claude', 'agents');

    try {
      const agentFiles = fs.readdirSync(agentsDir).filter(f => f.endsWith('.md'));
      for (const agentFile of agentFiles) {
        const agentName = agentFile.replace('.md', '');
        if (agentsDoc && !agentsDoc.includes(agentName)) {
          addIssue('missing-section', 'agents.md',
            `agent '${agentName}' defined in .claude/agents/ but not documented in agents.md`);
        }
      }
    } catch {
      // .claude/agents/ not found — skip
    }
  }
}

// ── Main ──────────────────────────────────────────────────────────────────

function main() {
  if (!fs.existsSync(DOCS_DIR)) {
    console.error(`validate-docs: docs/ directory not found at ${DOCS_DIR}`);
    process.exit(1);
  }

  const docFiles = readDir(DOCS_DIR);
  if (docFiles.length === 0) {
    console.log('validate-docs: no .md files found in docs/');
    process.exit(0);
  }

  checkBrokenLinks(docFiles);
  checkOrphanDocs(docFiles);
  checkStaleContent(docFiles);
  checkMissingSections(docFiles);

  if (issues.length === 0) {
    console.log(`validate-docs: OK — ${docFiles.length} docs checked, no issues found`);
    process.exit(0);
  }

  // Group issues by category
  const grouped = {};
  for (const issue of issues) {
    if (!grouped[issue.category]) grouped[issue.category] = [];
    grouped[issue.category].push(issue);
  }

  console.log(`\nvalidate-docs: ${issues.length} issue(s) found in ${docFiles.length} docs\n`);

  const categoryLabels = {
    'broken-link': 'Broken Links',
    'orphan': 'Orphan Docs',
    'stale': 'Stale Content',
    'missing-section': 'Missing Sections',
  };

  for (const [cat, catIssues] of Object.entries(grouped)) {
    console.log(`## ${categoryLabels[cat] || cat} (${catIssues.length})`);
    for (const issue of catIssues) {
      console.log(`  ${issue.file}: ${issue.message}`);
    }
    console.log('');
  }

  process.exit(1);
}

main();
