---

origin: theonekit-designer
repository: The1Studio/theonekit-designer
module: design-base
protected: false
---
# Living Documentation Patterns

## Design-as-Code Philosophy

Code constants are the single source of truth. Docs reflect them — never lead them.

| Truth Source | Example | Doc Field |
|-------------|---------|-----------|
| SceneSetup.cs constant | `const int UnitCount = 50` | Unit Types table count |
| PrefabCreator.cs | `baseStats.HP = 100` | Stats table HP column |
| MenuItem attribute | `[MenuItem("Tools/Demo/Setup")]` | Editor Tools menu path |
| System class name | `class CrawlerEncounterSystem` | Systems section class name |

**Rule**: Before writing any number or name in a wiki, grep the source file.
```bash
grep -n "const\|readonly\|static" "Assets/Demos/{Name}/Editor/{Name}SceneSetup.cs"
```

## Wiki Page Structure (Demo-*.md)

All demo wiki pages follow the 14-section structure defined in the `game-designer` agent.
Keep each wiki page under 300 lines. Extract deep-dives to `docs/wiki/Domain-*.md` and link.

```
docs/wiki/
├── Demo-BattleDemo2D.md        # <300 lines, links to Domain pages
├── Demo-BackpackCrawler.md
├── Domain-CombatSystem.md      # Deep dive, no line limit
├── Domain-InventoryGrid.md
└── Architecture.md
```

## Sync Triggers

| Code Change | Sections to Update |
|-------------|------------------|
| SceneSetup constant changed | Scene Structure, How to Run |
| PrefabCreator stats changed | Unit Types / Content Matrix |
| New system added to Runtime/ | Systems section |
| MenuItem path renamed | Editor Tools table |
| New authoring field added | Demo-Specific Components |
| BDP tree modified | Systems section (AI behavior) |
| Library module added | Library Coverage table |
| UI panel added/removed | Game Flow section |

## Auto-Detection Pattern

To find all constants needing documentation review after a code change:
```bash
# Find all tunable constants in a demo
grep -rn "const\|readonly static\|SerializeField" \
  "Assets/Demos/{DemoName}/" --include="*.cs" \
  | grep -v "//\|\.meta" \
  | sort

# Find all MenuItem paths (Editor Tools section)
grep -rn 'MenuItem("' "Assets/Demos/{DemoName}/Editor/" --include="*.cs"

# Find all system class names (Systems section)
grep -rn "class.*System" \  # adjust pattern for your engine
  "Assets/Demos/{DemoName}/Runtime/" --include="*.cs"
```

## Cross-Reference Conventions

### Between Wiki Pages
Use relative markdown links:
```markdown
→ See [CombatSystem deep dive](Domain-CombatSystem.md)
→ See [Architecture overview](Architecture.md#simulation-systems)
```

### Wiki ↔ Skills
Reference skills for implementation details, not design details:
```markdown
> Implementation: see engine implementation skills → relevant config class
> Navigation setup: see `agents-navigation` skill
```

### Wiki ↔ CLAUDE.md
CLAUDE.md Quick Reference section links to wiki for context:
```markdown
- **DerivedStatsSystem**: requires 7 components — see [Domain-StatsSystem](docs/wiki/Domain-StatsSystem.md)
```

## Update Verification Checklist
After updating any wiki page:
- [ ] All unit counts match current SceneSetup constants
- [ ] All stat values match current PrefabCreator values
- [ ] All system class names match current Runtime/ files
- [ ] All MenuItem paths match current `[MenuItem("...")]` attributes
- [ ] Cross-reference links resolve (no broken anchors)
- [ ] Line count under 300 (extract to Domain-*.md if over)
- [ ] Related pages updated (search for pages linking to this one)
