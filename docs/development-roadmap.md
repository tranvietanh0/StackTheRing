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

### Phase 2: Game Mechanics (Next)

**Status:** Planned

**Ring Stacking Core:**
- [ ] Ring prefab system (various sizes/colors)
- [ ] Stack pole mechanics
- [ ] Ring physics (drop, collision)
- [ ] Stack validation (size order)
- [ ] Success/failure detection

**Player Interaction:**
- [ ] Touch/click input handling
- [ ] Ring selection feedback
- [ ] Drag-and-drop mechanics
- [ ] Release and snap-to-pole

**Game States:**
- [ ] `GamePlayState` — Active gameplay
- [ ] `GamePauseState` — Pause menu
- [ ] `GameResultState` — Win/lose screen

---

### Phase 3: Progression System

**Status:** Planned

**Level System:**
- [ ] Level data structure (ScriptableObject or Blueprint)
- [ ] Level loader
- [ ] Difficulty scaling (more rings, more poles, time limit)
- [ ] Level completion tracking

**Scoring:**
- [ ] Score calculation (time, moves, accuracy)
- [ ] High score persistence
- [ ] Star rating system (1-3 stars)

**User Progress:**
- [ ] `UserLocalData` expansion
- [ ] Level unlock progression
- [ ] Statistics tracking

---

### Phase 4: UI/UX Polish

**Status:** Planned

**Screens:**
- [ ] `HomeScreenPresenter` — Main menu
- [ ] `LevelSelectScreenPresenter` — Level grid
- [ ] `GameHUDPresenter` — In-game overlay
- [ ] `PauseScreenPresenter` — Pause menu
- [ ] `ResultScreenPresenter` — Level complete
- [ ] `SettingsScreenPresenter` — Options

**Visual Feedback:**
- [ ] Ring selection highlight
- [ ] Valid/invalid placement indicators
- [ ] Success celebration effects
- [ ] DOTween animations throughout

**Audio:**
- [ ] Background music
- [ ] UI SFX (tap, swipe)
- [ ] Gameplay SFX (ring drop, success, fail)

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
| Unit tests for StateMachine | Medium | Medium | Add NUnit tests |
| Screen transition animations | Low | Low | Fade/slide presets |
| Object pooling for rings | High | Medium | Performance critical |
| Addressables memory cleanup | Medium | Medium | Unload unused assets |
| Blueprint system for levels | Medium | High | CSV-based config |

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
| DOTween Pro | - | Check | Manual update |

---

## Release Milestones

| Milestone | Target | Definition of Done |
|-----------|--------|-------------------|
| **Alpha** | TBD | Core gameplay loop playable |
| **Beta** | TBD | All levels, basic UI, no major bugs |
| **RC** | TBD | Monetization, analytics, store assets |
| **Launch** | TBD | Store submission approved |

---

## Notes

- Prioritize core gameplay loop before polish
- Test on low-end devices early (target: 2GB RAM devices)
- Keep APK size < 100MB for better conversion
- Plan for soft launch in test market before global
