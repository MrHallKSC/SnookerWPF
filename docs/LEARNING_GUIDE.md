# SnookerWPF - A-Level Learning Guide

## Quick Navigation: Where to Find Complex Concepts

### 🎯 Physics & Mathematics

#### Vector Mathematics
- **File**: `Models/Vector2D.cs`
- **What to learn**: Vector addition, subtraction, dot product, magnitude
- **Key method**: `DotProduct()`, `Magnitude`, `Normalised`
- **Why it matters**: Every physics calculation depends on vectors

#### Trigonometry (Polar → Cartesian Conversion)
- **File**: `Models/CueBall.cs` → `Strike()` method (line ~220)
- **What to learn**: sin/cos for converting angle+power to x,y velocity
- **Formula**: `x = magnitude × cos(angle)`, `y = magnitude × sin(angle)`
- **Real-world use**: Converting mouse aim direction into ball velocity

#### Inverse Trigonometry (atan2)
- **File**: `Models/CueBall.cs` → `SetAimDirection()` method (line ~140)
- **What to learn**: Why atan2 is better than atan for angle calculations
- **Why it matters**: Handles all four quadrants correctly
- **Key insight**: atan2(y, x) returns angle in correct direction

#### Collision Physics
- **File**: `Engine/PhysicsEngine.cs` → `ResolveCollision()` method (line ~495)
- **What to learn**: 
  - Conservation of momentum: m₁v₁ + m₂v₂ = m₁v₁' + m₂v₂'
  - Coefficient of restitution (energy loss in collisions)
  - Impulse-based collision resolution
- **Key concept**: Equal mass balls exchange velocities along collision normal
- **Why it matters**: Makes collisions realistic, not just bounces

#### Friction (Exponential Decay)
- **File**: `Engine/PhysicsEngine.cs` → `ApplyFriction()` method (line ~310)
- **What to learn**: Exponential decay vs. linear deceleration
- **Formula**: `v_new = v_old × friction^(deltaTime × 60)`
- **Why exponential?**: Realistic - balls slow faster when moving fast, slower when nearly stopped
- **Comparison**: Alternative `ApplyLinearFriction()` shows simpler but less realistic model

---

### 🏗️ Object-Oriented Programming

#### Abstract Classes & Inheritance
- **Files**: `Models/Ball.cs` (base), `Models/CueBall.cs`, `Models/ColouredBall.cs`
- **Inheritance Chain**: Ball (abstract) → CueBall & ColouredBall
- **What to learn**:
  - Why make Ball abstract? (Prevents creating generic balls)
  - What gets inherited? (Position, Velocity, Radius, Mass, Colour, IsOnTable)
  - What gets specialized? (CueBall adds aiming, ColouredBall adds scoring)
- **Key principle**: Inheritance = code reuse + specialization

#### Polymorphism in Action
- **Example**: `List<Ball> allBalls` can contain both CueBall and ColouredBall
- **Why it works**: Both ARE-A Ball, so both fit in a Ball list
- **Benefit**: Physics engine processes all balls same way, regardless of type

#### Encapsulation
- **All classes**: Properties with private backing fields
- **Example**: `Position` property with validation
- **Why it matters**: Objects control their own state, prevent invalid values

#### Enums for Type Safety
- **File**: `Models/ColouredBall.cs` → `BallType` enum (line ~30)
- **Values**: Red(1), Yellow(2), Green(3), Brown(4), Blue(5), Pink(6), Black(7)
- **Benefit**: Both type name AND point value in one definition
- **Why not just integers?**: Enums prevent assigning invalid values (no 99 as ball type)

---

### 🎮 Game Architecture & Algorithms

#### State Machine Pattern
- **File**: `Engine/GameManager.cs` → `GameState` enum (line ~15)
- **States**: PlacingCueBall → Aiming → BallsMoving → ProcessingShot → TurnEnd → FrameOver
- **Why it works**: Each state defines what's allowed (can't strike ball in FrameOver state)
- **Benefit**: Prevents impossible game states, code mirrors real-world flow

#### Rule Enforcement Algorithm
- **File**: `Engine/GameManager.cs` → Various Check methods
- **Complex logic**: Detecting fouls, awarding points, respotting balls, ending turns
- **Why it's complex**: Snooker has many rules, some context-dependent

#### Collision Detection Algorithm
- **File**: `Engine/PhysicsEngine.cs` → `CheckBallCollisions()` (checks all pairs)
- **Complexity**: O(n²) - for n balls, check n×(n-1)/2 pairs
- **Optimization opportunity**: Spatial partitioning for larger scenes

#### Ball Movement Loop (Game Loop)
- **File**: `Engine/PhysicsEngine.cs` → `Update()` method (line ~170)
- **Steps Each Frame**:
  1. Update positions (position = position + velocity × time)
  2. Apply friction (reduce velocity)
  3. Check cushion collisions (bounce off walls)
  4. Check ball-ball collisions (bounce off other balls)
- **Frame rate**: ~60 FPS, so this loop runs ~60 times per second

---

## Concept Difficulty Progression

### Start Here (GCSE Level)
1. ✅ Read `Models/Vector2D.cs` - understand what vectors are
2. ✅ Read `Models/Ball.cs` - understand inheritance basics
3. ✅ Read `Models/CueBall.cs` - see how subclasses extend base class
4. ✅ Trace through a simple shot: click to aim → click to shoot → see ball move

### Then Move To (Early A-Level)
5. ✅ Study `CueBall.Strike()` - understand cos/sin for angle→velocity
6. ✅ Study `CueBall.SetAimDirection()` - understand atan2 for velocity→angle
7. ✅ Study `Ball` class - understand abstract classes and why they're useful
8. ✅ Understand `GameState` enum - see state machine pattern

### Advanced Topics (Strong A-Level)
9. ✅ Study collision resolution in `PhysicsEngine.ResolveCollision()` 
   - Understand momentum conservation
   - Learn impulse-based physics
   - See how dot product extracts collision-direction velocity
10. ✅ Study `PhysicsEngine.ApplyFriction()` - exponential decay mathematics
11. ✅ Analyze `GameManager` rule enforcement - complex conditional logic
12. ✅ Consider optimizations: could collision detection be faster?

---

## Key Formulas & Their Meanings

### Vector Math
```
Magnitude (length): |v| = √(x² + y²)
Dot Product:        v₁·v₂ = v₁ₓ×v₂ₓ + v₁ᵧ×v₂ᵧ  (tells how aligned vectors are)
Normalise:          v̂ = v / |v|  (create unit vector)
```

### Trigonometry
```
Polar → Cartesian:  x = r×cos(θ),  y = r×sin(θ)  (angle to components)
Cartesian → Polar:  θ = atan2(y, x)  (components to angle)
```

### Physics
```
Position Update:    p_new = p_old + v × Δt  (Euler integration)
Friction:           v_new = v_old × friction^(Δt×60)  (exponential decay)
Momentum:           m₁v₁ + m₂v₂ = m₁v₁' + m₂v₂'  (conserved in collisions)
Impulse:            j = -(1+e) × (v₁·n̂ - v₂·n̂) / 2  (equal mass collision)
```

---

## Questions to Ask Yourself

### Understanding
- [ ] Why is Ball abstract instead of concrete?
- [ ] How does trigonometry convert a mouse position into ball velocity?
- [ ] Why use atan2 instead of atan for aiming?
- [ ] What does coefficient of restitution (0.96) mean?

### Application
- [ ] Can I trace a ball from shot through collision to stop?
- [ ] Could I add a "Jump Shot" by modifying friction?
- [ ] How would I make balls lose more energy in collisions?
- [ ] What would break if I removed the velocity threshold?

### Analysis
- [ ] Is the collision detection O(n²) - how could it be faster?
- [ ] Why does friction use Math.Pow instead of simple multiplication?
- [ ] How does the state machine prevent invalid operations?
- [ ] Why separate physics logic from game rule logic?

### Extension
- [ ] Could I add spin to the cue ball?
- [ ] How would you implement "bank shots" off cushions?
- [ ] What would a 3D version look like?
- [ ] Could you add special ball types with different physics?

---

## Common Code Patterns Used

### Pattern 1: Template Method (Physics Updates)
```csharp
Update() {
    foreach ball:
        UpdatePosition()
        ApplyFriction()
        CheckCushionCollisions()
    CheckBallCollisions()
}
```
**Why**: Consistent flow each frame

### Pattern 2: State Machine (Game Flow)
```csharp
if (gameState == GameState.Aiming)
    AllowShooting();
else if (gameState == GameState.BallsMoving)
    AllowPausing();
```
**Why**: Different actions in different states

### Pattern 3: Polymorphism (Ball Types)
```csharp
List<Ball> allBalls;  // Can hold CueBall or ColouredBall
foreach (Ball b in allBalls)
    b.IsMoving;  // Works for all types
```
**Why**: Same code works for different types

### Pattern 4: Encapsulation (Data Protection)
```csharp
private Vector2D velocity;
public Vector2D Velocity {
    get { return velocity; }
    set { velocity = value; }  // Could add validation
}
```
**Why**: Object controls its own data

---

## Project Structure Overview

```
SnookerWPF/
├── Models/              ← Data & Basic Behavior
│   ├── Vector2D.cs      ← Vector mathematics
│   ├── Ball.cs          ← Abstract base class
│   ├── CueBall.cs       ← Cue ball (with aiming)
│   ├── ColouredBall.cs  ← Red & coloured balls
│   ├── Player.cs        ← Player stats
│   ├── Pocket.cs        ← Pocket detection
│   └── Table.cs         ← Table layout
├── Engine/              ← Logic & Simulation
│   ├── PhysicsEngine.cs ← Movement & collisions
│   └── GameManager.cs   ← Rules & game state
└── UI/                  ← Display
    ├── MainWindow.xaml  ← Interface layout
    └── MainWindow.xaml.cs ← Input & rendering
```

---

## Recommended Study Order

1. **Week 1**: Understand structure
   - Read project overview
   - Understand class hierarchy
   - See what each file does

2. **Week 2**: OOP concepts
   - Why Ball is abstract
   - How CueBall/ColouredBall inherit
   - Polymorphism with List<Ball>

3. **Week 3**: Physics basics
   - Vector basics (add, subtract, magnitude)
   - Position update each frame
   - Simple friction

4. **Week 4**: Advanced physics
   - Collision detection (distance check)
   - Collision response (momentum conservation)
   - Elastic collision formula

5. **Week 5**: Mathematics
   - Trigonometry (sin, cos)
   - Converting angle→velocity (Strike method)
   - Converting position→angle (SetAimDirection with atan2)

6. **Week 6**: Game logic
   - State machine concept
   - Rule enforcement
   - Score tracking

---

## Extension Challenges

### Beginner
- [ ] Change ball colour (modify ColouredBall.cs)
- [ ] Adjust friction coefficient (make balls faster/slower)
- [ ] Modify table size (change Table.cs dimensions)

### Intermediate
- [ ] Add a new ball type to the enum
- [ ] Change collision restitution (make bounces bouncier)
- [ ] Add a power meter UI

### Advanced
- [ ] Implement "bank shot" physics (reflect off cushions)
- [ ] Add ball spin/English
- [ ] Implement shot prediction (show where ball will go)
- [ ] Optimize collision detection with spatial partitioning

---

## Quick Reference: Where to Find Things

| Concept | File | Method/Line | Notes |
|---------|------|-------------|-------|
| Vectors | Vector2D.cs | DotProduct(), Magnitude | Foundation for all physics |
| Inheritance | Ball.cs | Class definition | Abstract base class |
| Aiming (sin/cos) | CueBall.cs | Strike() ~220 | Angle→Velocity |
| Aiming (atan2) | CueBall.cs | SetAimDirection() ~140 | Velocity→Angle |
| Friction | PhysicsEngine.cs | ApplyFriction() ~310 | Exponential decay |
| Collisions | PhysicsEngine.cs | ResolveCollision() ~495 | Momentum conservation |
| State Machine | GameManager.cs | GameState enum ~15 | Game flow control |
| Ball Physics | PhysicsEngine.cs | Update() ~170 | Main game loop |

---

**Happy learning! The key is understanding WHY each design choice was made, not just WHAT the code does.**

---

Generated: January 2026  
For: A-Level Computer Science Students  
Focus: Understanding Complex Concepts Through Working Code
