# Development Roadmap — Stack The Ring

## Project Phases

### Phase 1: Core Framework (Current)

**Status:** Complete

- [x] Unity 6 project setup
- [x] VContainer DI integration
- [x] UniTask async framework
- [x] MessagePipe pub/sub
- [x] Addressables asset loading
- [x] State machine foundation
- [x] MVP screen system
- [x] Loading flow (LoadingScene → MainScene)
- [x] Project documentation

---

### Phase 2: Game Mechanics (Current)

**Status:** ✅ Complete

**Conveyor System:**
- [x] Spline-based conveyor (Dreamteck Splines)
- [x] RowBall containers (5 balls each)
- [x] PathFollower component
- [x] Looping conveyor belt

**Ball/Ring System:**
- [x] Ball prefab with color support (4 colors)
- [x] RowBall container management
- [x] Ball collection/removal from rows
- [x] DOTween jump animations

**Slot & Stacking:**
- [x] 4 stacking slots
- [x] Color-based collector placement
- [x] Stack limit per slot (configurable)
- [x] Stack clearing when full

**Attraction Mechanics:**
- [x] Progress-based attraction zones
- [x] Curved path attraction (DOTween)
- [x] Color matching logic

**Level System:**
- [x] LevelData ScriptableObject
- [x] LevelManager with load/save
- [x] Level progression tracking

**Game States:**
- [x] `GamePlayState` — Active gameplay with ITickable
- [x] `GameWinState` — Level completed
- [x] `GameLoseState` — Game over (no possible moves)

---

### Phase 3: Progression System (Next)

**Status:** In Progress

**Level System:**
- [x] Level data structure (LevelData ScriptableObject)
- [x] Level loader (Resources + Addressables fallback)
- [x] Difficulty scaling (ConveyorSpeed, StackLimit, RingCount)
- [x] Level completion tracking (HighestUnlockedLevel)

**Scoring:**
- [ ] Score calculation (time bonus, combo bonus)
- [x] Basic score on level complete
- [ ] Star rating system (1-3 stars)

**User Progress:**
- [x] `UserLocalData` persistence
- [x] Level unlock progression
- [ ] Statistics tracking (total games, total rings cleared)

---

### Phase 4: UI/UX Polish

**Status:** Planned

**Screens:**
- [x] `LoadingScreenPresenter` — Initial loading
- [ ] `HomeScreenPresenter` — Main menu
- [ ] `LevelSelectScreenPresenter` — Level grid
- [ ] `GameHUDPresenter` — In-game overlay (score, level)
- [ ] `PauseScreenPresenter` — Pause menu
- [ ] `ResultScreenPresenter` — Win/Lose popup
- [ ] `SettingsScreenPresenter` — Options

**Visual Feedback:**
- [x] Ball attraction curved path
- [x] Stack clear scale animation
- [x] Arrival punch effect
- [ ] Collector tap feedback
- [ ] Success celebration particles
- [ ] Lose condition warning

**Audio:**
- [ ] Background music
- [ ] UI SFX (tap, swipe)
- [ ] Gameplay SFX (ball collect, stack clear, win, lose)

---

### Phase 5: Monetization & Analytics

**Status:** Planned

**Ads Integration:**
- [ ] Interstitial ads (between levels)
- [ ] Rewarded ads (retry, hints)
- [ ] Banner ads (if applicable)

**IAP (Optional):**
- [ ] Remove ads purchase
- [ ] Theme packs

**Analytics:**
- [ ] Firebase/GameAnalytics setup
- [ ] Level completion events
- [ ] Retention metrics
- [ ] Funnel analysis

---

### Phase 6: Release Preparation

**Status:** Planned

**Optimization:**
- [ ] Profiler analysis
- [ ] Draw call optimization
- [ ] Memory management
- [ ] Loading time reduction

**Platform Specific:**
- [ ] Android build configuration
- [ ] iOS build configuration
- [ ] Splash screen/icons
- [ ] Privacy policy compliance

**Testing:**
- [ ] Device compatibility matrix
- [ ] Performance benchmarks
- [ ] Crash-free rate targets

**Store Assets:**
- [ ] App icon variations
- [ ] Screenshots (multiple devices)
- [ ] Feature graphic
- [ ] Store description

---

## Technical Debt Backlog

| Item | Priority | Effort | Notes |
|------|----------|--------|-------|
| Object pooling for balls | High | Medium | Currently using Instantiate/Destroy |
| Unit tests for game systems | Medium | Medium | ConveyorController, SlotManager |
| Screen transition animations | Low | Low | Fade/slide presets |
| Addressables memory cleanup | Medium | Medium | Unload unused assets |
| Combo system | Medium | Medium | Rapid collection bonus |
| Tutorial system | High | High | First-time player guidance |
| Pause state | Low | Low | GamePauseState not implemented |

---

## Future Considerations

**Not Committed (Nice-to-Have):**
- Daily challenges
- Leaderboards (Google Play Games / Game Center)
- Multiple themes/skins
- Tutorial system
- Achievements
- Cloud save sync

---

## Dependencies to Monitor

| Package | Current | Latest | Update Notes |
|---------|---------|--------|--------------|
| VContainer | 1.16.9 | Check | Stable |
| UniTask | 2.5.10 | Check | Stable |
| MessagePipe | 1.8.1 | Check | Stable |
| Addressables | 2.9.0 | Check | Unity 6 compatible |
| Dreamteck Splines | - | Check | Conveyor path system |
| DOTween Pro | - | Check | Animation system |

---

## Release Milestones

| Milestone | Target | Definition of Done |
|-----------|--------|-------------------|
| **Alpha** | ✅ Done | Core gameplay loop playable |
| **Beta** | TBD | All levels, basic UI, no major bugs |
| **RC** | TBD | Monetization, analytics, store assets |
| **Launch** | TBD | Store submission approved |

---

## Notes

- Prioritize core gameplay loop before polish
- Test on low-end devices early (target: 2GB RAM devices)
- Keep APK size < 100MB for better conversion
- Plan for soft launch in test market before global
