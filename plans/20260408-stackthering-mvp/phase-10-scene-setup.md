# Phase 10: Scene Setup

**Effort:** M (2-3 days)
**Dependencies:** Phase 1-9
**Blocks:** None (Final phase)

## Objective

Create prefabs, setup scene hierarchy, and integrate all components.

## Tasks

### 1. Create Prefabs

#### Ball Prefab
```
Ball (GameObject)
├── Model (3D Sphere or custom mesh)
│   └── MeshRenderer (for color)
├── Collider (SphereCollider, isTrigger=false)
└── Ball.cs component
    └── Assign: ballRenderer → Model's MeshRenderer
```

**Settings:**
- Scale: (0.15, 0.15, 0.15)
- Layer: Default

#### RowBall Prefab
```
RowBall (GameObject)
├── SpawnRoot (empty Transform for ball positions)
├── PathFollower.cs (added at runtime)
└── RowBall.cs component
    ├── Assign: spawnRoot → SpawnRoot
    └── Assign: ballPrefab → Ball prefab
```

**Settings:**
- Scale: (1, 1, 1)

#### Bucket Prefab
```
Bucket (GameObject)
├── BucketModel (3D mesh)
│   └── MeshRenderer[] (for color)
├── Cover (hidden by default)
├── ProgressLabel (TextMeshPro 3D)
├── Collider (BoxCollider)
└── Bucket.cs component
    ├── Assign: meshRenderers → BucketModel renderers
    ├── Assign: progressLabel → ProgressLabel
    ├── Assign: coverNode → Cover
    └── Assign: bucketCollider → Collider
```

**Settings:**
- Scale: (1, 1, 1)
- Layer: Bucket (create new layer)

#### CollectArea Prefab
```
CollectArea (GameObject)
├── Visual (optional platform/highlight)
├── BucketSlot (empty Transform)
└── CollectArea.cs component
    └── Assign: bucketSlot → BucketSlot
```

### 2. Scene Hierarchy

```
1.MainScene
├── --- CORE ---
├── GameLifetimeScope (existing)
├── MainSceneScope (existing)
├── StackTheRingSceneScope (NEW)
│   └── Assign all serialized fields
│
├── --- CAMERAS ---
├── Main Camera
│   └── Tag: MainCamera
│
├── --- GAME OBJECTS ---
├── ConveyorSystem
│   ├── MainConveyor
│   │   ├── ConveyorController.cs
│   │   ├── PathNodes (children: Node0, Node1, Node2...)
│   │   ├── EntryNodes (children: Entry0, Entry1...)
│   │   └── SpawnPoint
│   └── ConveyorVisual (belt mesh, optional)
│
├── CollectAreas
│   ├── CollectArea_0
│   ├── CollectArea_1
│   └── CollectArea_2
│
├── BucketSpawnArea (staging area for buckets)
│
├── --- INPUT ---
├── InputHandler
│   └── InputHandler.cs
│       ├── Assign: mainCamera
│       └── Assign: bucketLayer
│
├── --- UI ---
├── UICanvas (existing from GameFoundation)
│   └── Screens loaded via Addressables
│
└── --- LIGHTING ---
    ├── Directional Light
    └── (other lights as needed)
```

### 3. StackTheRingSceneScope Configuration

Inspector assignments:

| Field | Value |
|-------|-------|
| Config | StackTheRingConfig asset |
| Ball Prefab | Ball.prefab |
| Row Ball Prefab | RowBall.prefab |
| Bucket Prefab | Bucket.prefab |
| Main Conveyor | ConveyorController in scene |
| Collect Area Container | CollectAreas parent |
| Collect Areas | List of CollectArea components |
| Input Handler | InputHandler component |
| Main Camera | Main Camera |

### 4. Layer Setup

1. **Edit → Project Settings → Tags and Layers**
2. Add layer: `Bucket` (e.g., layer 8)
3. Assign to Bucket prefab colliders
4. Set InputHandler.bucketLayer to "Bucket"

### 5. Addressables Setup

Add to Addressables:
- `Ball` → Ball.prefab
- `RowBall` → RowBall.prefab
- `Bucket` → Bucket.prefab
- `GameHUDScreenView` → GameHUDScreenView.prefab
- `GameWinScreenView` → GameWinScreenView.prefab
- `StackTheRingConfig` → StackTheRingConfig.asset

### 6. Create Config Asset

1. **Assets → Create → StackTheRing → Config**
2. Name: `StackTheRingConfig`
3. Set default values:
   - BallsPerRow: 5
   - BallJumpHeight: 1
   - BallJumpDuration: 0.2
   - ConveyorSpeed: 1
   - DefaultTargetBallCount: 10 (for quick MVP testing)

### 7. Conveyor Path Setup

For MVP ellipse path:

```
PathNodes positions (example ellipse):
- Node0: (0, 0, -5)
- Node1: (3, 0, -3)
- Node2: (4, 0, 0)
- Node3: (3, 0, 3)
- Node4: (0, 0, 5)
- Node5: (-3, 0, 3)
- Node6: (-4, 0, 0)
- Node7: (-3, 0, -3)
- (loops back to Node0)

EntryNodes positions (collection points):
- Entry0: (4, 0, 0)   # Right side
- Entry1: (-4, 0, 0)  # Left side

SpawnPoint:
- Position: (0, 0, -5) # Top of ellipse
```

## Verification Checklist

### Prefabs
- [ ] Ball prefab: has Ball.cs, MeshRenderer, Collider
- [ ] RowBall prefab: has RowBall.cs, SpawnRoot, Ball prefab reference
- [ ] Bucket prefab: has Bucket.cs, all serialized fields assigned, Bucket layer
- [ ] CollectArea prefab: has CollectArea.cs, BucketSlot transform

### Scene
- [ ] StackTheRingSceneScope: all fields assigned in Inspector
- [ ] ConveyorController: PathNodes and EntryNodes populated
- [ ] 3 CollectAreas positioned correctly
- [ ] InputHandler: camera and layer mask assigned
- [ ] Main Camera tagged as MainCamera

### Addressables
- [ ] All prefabs added to Addressables
- [ ] All UI prefabs named correctly (match View class names)
- [ ] Config asset in Addressables

### Runtime Test
- [ ] Scene loads without errors
- [ ] Balls spawn on conveyor
- [ ] Conveyor moves RowBalls along path
- [ ] Tapping bucket makes balls jump
- [ ] Bucket completion triggers animation
- [ ] All buckets complete → Win state

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| Balls don't spawn | Missing prefab reference | Check LevelLoader prefab assignments |
| Conveyor doesn't move | PathNodes empty | Add path nodes in Inspector |
| Tap doesn't work | Wrong layer | Check Bucket layer and InputHandler mask |
| Signals not firing | Not declared in scope | Check StackTheRingSceneScope signals |
| Screen not opening | Addressable key mismatch | Verify prefab name = View class name |

## Notes

- Test with reduced TargetBallCount (10) for faster iteration
- Use Gizmos to visualize path in Scene view
- Enable Debug.Log in StackTheRingController for signal tracking
- Camera position: adjust for best view of conveyor + collect areas
