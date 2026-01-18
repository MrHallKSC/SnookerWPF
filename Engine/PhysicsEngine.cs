using System;
using System.Collections.Generic;
using System.Diagnostics;
using SnookerGame.Models;

namespace SnookerGame.Engine
{
    /// <summary>
    /// Handles all physics calculations for the snooker game.
    /// 
    /// This class is responsible for:
    /// - Updating ball positions based on velocity
    /// - Applying friction to slow balls down
    /// - Detecting and resolving cushion collisions
    /// - Detecting and resolving ball-ball collisions (Phase 4)
    /// 
    /// The physics simulation uses discrete time steps, updating positions
    /// each frame based on velocity and delta time.
    /// 
    /// Key physics concepts demonstrated:
    /// - Kinematics: position = position + velocity × time
    /// - Friction: velocity = velocity × (1 - friction × time)
    /// - Elastic collisions: conservation of momentum and energy
    /// </summary>
    public class PhysicsEngine
    {
        #region Constants

        /// <summary>
        /// Default friction coefficient for the table cloth.
        /// Higher values = balls slow down faster.
        /// Typical range: 0.5 to 2.0
        /// </summary>
        private const double DEFAULT_FRICTION = 0.985;

        /// <summary>
        /// Default coefficient of restitution for ball-ball collisions.
        /// 1.0 = perfectly elastic (no energy loss)
        /// 0.0 = perfectly inelastic (maximum energy loss)
        /// Snooker balls are quite elastic, typically 0.95-0.98
        /// </summary>
        private const double DEFAULT_RESTITUTION = 0.96;

        /// <summary>
        /// Coefficient of restitution for cushion bounces.
        /// Slightly lower than ball-ball as cushions absorb more energy.
        /// </summary>
        private const double CUSHION_RESTITUTION = 0.85;

        /// <summary>
        /// Minimum velocity threshold. Balls moving slower than this are stopped.
        /// Prevents balls from creeping infinitely slowly due to floating-point precision.
        /// </summary>
        private const double MIN_VELOCITY = 2.0;

        /// <summary>
        /// Maximum iterations for collision resolution per frame.
        /// Prevents infinite loops if balls get stuck.
        /// </summary>
        private const int MAX_COLLISION_ITERATIONS = 10;

        #endregion

        #region Private Fields

        private double frictionCoefficient;
        private double coefficientOfRestitution;
        private double gravity;
        private double timeStep;

        #endregion

        #region Properties

        /// <summary>
        /// Friction coefficient for the table surface.
        /// Controls how quickly balls slow down.
        /// </summary>
        public double FrictionCoefficient
        {
            get { return frictionCoefficient; }
            set { frictionCoefficient = Math.Max(0, Math.Min(1, value)); }
        }

        /// <summary>
        /// Coefficient of restitution for ball-ball collisions.
        /// Controls how much energy is retained after collisions.
        /// </summary>
        public double CoefficientOfRestitution
        {
            get { return coefficientOfRestitution; }
            set { coefficientOfRestitution = Math.Max(0, Math.Min(1, value)); }
        }

        /// <summary>
        /// Gravity value (reserved for future enhancements like jump shots).
        /// Not currently used in 2D simulation.
        /// </summary>
        public double Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }

        /// <summary>
        /// Fixed time step for physics calculations.
        /// Using fixed time steps ensures consistent physics regardless of frame rate.
        /// </summary>
        public double TimeStep
        {
            get { return timeStep; }
            set { timeStep = Math.Max(0.001, value); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new PhysicsEngine with default settings.
        /// </summary>
        public PhysicsEngine()
        {
            this.frictionCoefficient = DEFAULT_FRICTION;
            this.coefficientOfRestitution = DEFAULT_RESTITUTION;
            this.gravity = 0;  // Not used in 2D
            this.timeStep = 1.0 / 60.0;  // 60 FPS default
        }

        /// <summary>
        /// Creates a PhysicsEngine with custom friction and restitution values.
        /// </summary>
        /// <param name="friction">Friction coefficient (0-1)</param>
        /// <param name="restitution">Coefficient of restitution (0-1)</param>
        public PhysicsEngine(double friction, double restitution)
        {
            this.frictionCoefficient = Math.Max(0, Math.Min(1, friction));
            this.coefficientOfRestitution = Math.Max(0, Math.Min(1, restitution));
            this.gravity = 0;
            this.timeStep = 1.0 / 60.0;
        }

        #endregion

        #region Main Update Loop

        /// <summary>
        /// Main physics update method. Called each frame to update all ball positions.
        /// 
        /// Process:
        /// 1. Update each ball's position based on velocity
        /// 2. Apply friction to slow balls down
        /// 3. Check and resolve cushion collisions
        /// 4. Check and resolve ball-ball collisions
        /// 5. Check for potted balls
        /// 
        /// </summary>
        /// <param name="balls">List of all balls to update</param>
        /// <param name="table">Table for boundary collisions</param>
        /// <param name="deltaTime">Time elapsed since last update (seconds)</param>
        /// <returns>True if any balls are still moving</returns>
        public bool Update(List<Ball> balls, Table table, double deltaTime)
        {
            // Find the cue ball in the list (needed for collision event)
            CueBall cueBall = null;
            foreach (Ball ball in balls)
            {
                if (ball is CueBall cb)
                {
                    cueBall = cb;
                    break;
                }
            }

            bool anyBallMoving = false;

            // Update each ball
            foreach (Ball ball in balls)
            {
                if (!ball.IsOnTable) continue;

                if (ball.IsMoving)
                {
                    anyBallMoving = true;

                    // Step 1: Update position based on velocity
                    UpdatePosition(ball, deltaTime);

                    // Step 2: Apply friction
                    ApplyFriction(ball, deltaTime);

                    // Step 3: Check cushion collisions
                    CheckCushionCollisions(ball, table);
                }
            }

            // Step 4: Check ball-ball collisions
            CheckBallCollisions(balls, cueBall);

            // Check if any balls are now moving (collision might have started a stationary ball)
            foreach (Ball ball in balls)
            {
                if (ball.IsOnTable && ball.IsMoving)
                {
                    anyBallMoving = true;
                    break;
                }
            }

            return anyBallMoving;
        }

        /// <summary>
        /// Overload that accepts CueBall and ColouredBalls separately.
        /// Combines them into a single list for processing.
        /// </summary>
        public bool Update(CueBall cueBall, List<ColouredBall> colouredBalls,
                          Table table, double deltaTime)
        {
            // Create combined list of all balls
            List<Ball> allBalls = new List<Ball>();
            allBalls.Add(cueBall);
            allBalls.AddRange(colouredBalls);

            bool anyBallMoving = false;

            // Update each ball
            foreach (Ball ball in allBalls)
            {
                if (!ball.IsOnTable) continue;

                if (ball.IsMoving)
                {
                    anyBallMoving = true;

                    // Step 1: Update position based on velocity
                    UpdatePosition(ball, deltaTime);

                    // Step 2: Apply friction
                    ApplyFriction(ball, deltaTime);

                    // Step 3: Check cushion collisions
                    CheckCushionCollisions(ball, table);
                }
            }

            // Step 4: Check ball-ball collisions (pass cueBall for event firing)
            CheckBallCollisions(allBalls, cueBall);

            // Check if any balls are now moving (collision might have started a stationary ball)
            foreach (Ball ball in allBalls)
            {
                if (ball.IsOnTable && ball.IsMoving)
                {
                    anyBallMoving = true;
                    break;
                }
            }

            return anyBallMoving;
        }

        #endregion

        #region Position and Velocity Updates

        /// <summary>
        /// Updates a ball's position based on its current velocity.
        /// 
        /// Uses the kinematic equation: position = position + velocity × time
        /// 
        /// This is Euler integration - simple and sufficient for our purposes.
        /// More complex games might use Verlet integration for better accuracy.
        /// </summary>
        /// <param name="ball">Ball to update</param>
        /// <param name="deltaTime">Time step in seconds</param>
        private void UpdatePosition(Ball ball, double deltaTime)
        {
            // Calculate displacement: velocity × time
            Vector2D displacement = ball.Velocity.Multiply(deltaTime);

            // Update position: position = position + displacement
            ball.Position = ball.Position.Add(displacement);
        }

        /// <summary>
        /// Applies friction to reduce a ball's velocity over time.
        /// 
        /// === PHYSICS EXPLANATION FOR A-LEVEL STUDENTS ===
        /// 
        /// TWO FRICTION MODELS EXPLAINED:
        /// 
        /// 1. EXPONENTIAL DECAY (USED HERE):
        /// velocity_new = velocity_old × friction^(deltaTime × 60)
        /// 
        /// This models rolling resistance and air resistance, which affect the ball
        /// proportional to current velocity. A real ball on cloth experiences decreasing
        /// resistance as it slows down. Mathematically:
        /// 
        /// dv/dt = -k × v  (differential equation)
        /// Solution: v(t) = v₀ × e^(-kt) ≈ v₀ × friction^t
        /// 
        /// Advantages:
        /// - Physically realistic: balls naturally decelerate quickly then slowly
        /// - No terminal velocity problems
        /// - Frame-rate independent when using Math.Pow
        /// 
        /// The "× 60" factor normalizes for 60 FPS. This prevents friction effects
        /// from changing when frame rate varies (important for smooth gameplay).
        /// 
        /// 2. LINEAR DECELERATION (ALTERNATIVE - see ApplyLinearFriction):
        /// velocity_new = velocity_old - (deceleration × deltaTime)
        /// 
        /// This is simpler but less realistic. It assumes constant friction force
        /// regardless of speed, and balls would come to exact rest in finite time.
        /// 
        /// Real snooker friction coefficients: approximately 0.98-0.99 per frame
        /// This means about 98-99% of velocity retained each frame.
        /// </summary>
        /// <param name="ball">Ball to apply friction to</param>
        /// <param name="deltaTime">Time step in seconds</param>
        private void ApplyFriction(Ball ball, double deltaTime)
        {
            if (!ball.IsMoving) return;

            // Calculate friction multiplier using exponential decay
            // Math.Pow(base, exponent) calculates: base ^ exponent
            // frictionCoefficient (0.985) raised to power (deltaTime × 60)
            // At 60 FPS: deltaTime ≈ 0.0167, so exponent ≈ 1.0, giving ≈ 0.985× velocity
            double frictionMultiplier = Math.Pow(frictionCoefficient, deltaTime * 60);

            Vector2D newVelocity = ball.Velocity.Multiply(frictionMultiplier);

            // Velocity threshold check: prevents balls from moving infinitesimally slowly
            // After many frames, floating-point errors could cause tiny velocities.
            // Below 2.0 units/sec, we consider the ball stopped (good enough for gameplay).
            if (newVelocity.Magnitude < MIN_VELOCITY)
            {
                // Complete stop - prevents jitter from floating-point math
                ball.SetVelocity(new Vector2D(0, 0));
            }
            else
            {
                ball.SetVelocity(newVelocity);
            }
        }

        /// <summary>
        /// Alternative friction model using linear deceleration.
        /// Reduces velocity by a fixed amount per second.
        /// 
        /// Not currently used, but provided as an alternative approach.
        /// </summary>
        private void ApplyLinearFriction(Ball ball, double deltaTime, double decelerationRate)
        {
            if (!ball.IsMoving) return;

            double speed = ball.Velocity.Magnitude;
            double newSpeed = speed - (decelerationRate * deltaTime);

            if (newSpeed <= 0)
            {
                ball.SetVelocity(new Vector2D(0, 0));
            }
            else
            {
                // Maintain direction, reduce magnitude
                Vector2D direction = ball.Velocity.Normalised;
                ball.SetVelocity(direction.Multiply(newSpeed));
            }
        }

        #endregion

        #region Cushion Collisions

        /// <summary>
        /// Checks if a ball has collided with any cushion and resolves the collision.
        /// 
        /// Cushion collision physics:
        /// 1. Detect if ball has crossed the table boundary
        /// 2. Reflect the velocity component perpendicular to the cushion
        /// 3. Apply coefficient of restitution (energy loss)
        /// 4. Reposition ball to prevent it going through the cushion
        /// </summary>
        /// <param name="ball">Ball to check</param>
        /// <param name="table">Table with boundaries</param>
        /// <returns>True if a collision occurred</returns>
        private bool CheckCushionCollisions(Ball ball, Table table)
        {
            bool collisionOccurred = false;
            Vector2D position = ball.Position;
            Vector2D velocity = ball.Velocity;
            double radius = ball.Radius;

            // Check left cushion (x = 0)
            if (position.X - radius < 0)
            {
                // Reposition ball to just touch cushion
                position.X = radius;

                // Reflect X velocity and apply restitution
                velocity = new Vector2D(
                    -velocity.X * CUSHION_RESTITUTION,
                    velocity.Y
                );
                collisionOccurred = true;
            }

            // Check right cushion (x = table width)
            if (position.X + radius > table.Width)
            {
                position.X = table.Width - radius;
                velocity = new Vector2D(
                    -velocity.X * CUSHION_RESTITUTION,
                    velocity.Y
                );
                collisionOccurred = true;
            }

            // Check top cushion (y = 0)
            if (position.Y - radius < 0)
            {
                position.Y = radius;
                velocity = new Vector2D(
                    velocity.X,
                    -velocity.Y * CUSHION_RESTITUTION
                );
                collisionOccurred = true;
            }

            // Check bottom cushion (y = table height)
            if (position.Y + radius > table.Height)
            {
                position.Y = table.Height - radius;
                velocity = new Vector2D(
                    velocity.X,
                    -velocity.Y * CUSHION_RESTITUTION
                );
                collisionOccurred = true;
            }

            // Apply changes if collision occurred
            if (collisionOccurred)
            {
                ball.Position = position;
                ball.SetVelocity(velocity);
            }

            return collisionOccurred;
        }

        #endregion

        #region Ball-Ball Collision Detection and Resolution

        /// <summary>
        /// Event that fires when the cue ball hits another ball.
        /// Used by GameManager to track first ball hit for foul detection.
        /// </summary>
        public event Action<Ball> OnCueBallCollision;

        /// <summary>
        /// Detects if two balls are colliding (overlapping).
        /// 
        /// Two circles collide when the distance between their centres
        /// is less than the sum of their radii.
        /// 
        /// Formula: distance(ball1, ball2) < radius1 + radius2
        /// </summary>
        /// <param name="ball1">First ball</param>
        /// <param name="ball2">Second ball</param>
        /// <returns>True if balls are overlapping</returns>
        public bool DetectCollision(Ball ball1, Ball ball2)
        {
            // Don't check balls that aren't on the table
            if (!ball1.IsOnTable || !ball2.IsOnTable) return false;

            // Calculate distance between centres
            double distance = ball1.Position.DistanceTo(ball2.Position);

            // Calculate sum of radii
            double combinedRadii = ball1.Radius + ball2.Radius;

            // Collision if distance < combined radii
            return distance < combinedRadii;
        }

        /// <summary>
        /// Resolves a collision between two balls using elastic collision physics.
        /// 
        /// === PHYSICS EXPLANATION FOR A-LEVEL STUDENTS ===
        /// 
        /// This implements conservation of momentum and kinetic energy, which are fundamental
        /// principles in Newtonian mechanics:
        /// 
        /// 1. CONSERVATION OF MOMENTUM: m₁v₁ + m₂v₂ = m₁v₁' + m₂v₂'
        ///    The total momentum before collision equals total momentum after.
        ///    Since all snooker balls have equal mass (m), this simplifies significantly.
        /// 
        /// 2. COEFFICIENT OF RESTITUTION (e): Determines energy loss
        ///    e = 0 means perfectly inelastic (maximum energy loss, balls stick)
        ///    e = 1 means perfectly elastic (no energy loss)
        ///    Real snooker balls: e ≈ 0.96 (very little energy loss)
        /// 
        /// 3. THE COLLISION NORMAL: The direction from ball1 to ball2
        ///    We only exchange momentum ALONG this direction.
        ///    Velocities perpendicular to collision normal are unchanged.
        /// 
        /// 4. IMPULSE-BASED RESOLUTION: Instead of calculating new velocities directly,
        ///    we calculate an "impulse" (instantaneous force) and apply it to both balls.
        ///    This ensures momentum conservation and correct energy loss via restitution.
        /// 
        /// Formula for equal-mass elastic collision:
        /// j = -(1 + e) × (v₁·n̂ - v₂·n̂) / 2
        /// where:
        /// - j is the impulse magnitude
        /// - n̂ is the normalised collision vector
        /// - · represents dot product (component in direction of collision)
        /// 
        /// Then apply: v₁' = v₁ + j×n̂  and  v₂' = v₂ - j×n̂
        /// </summary>
        /// <param name="ball1">First ball</param>
        /// <param name="ball2">Second ball</param>
        public void ResolveCollision(Ball ball1, Ball ball2)
        {
            // Get positions and velocities
            Vector2D pos1 = ball1.Position;
            Vector2D pos2 = ball2.Position;
            Vector2D vel1 = ball1.Velocity;
            Vector2D vel2 = ball2.Velocity;

            // Step 1: Calculate the vector pointing from ball2 to ball1
            // This becomes our collision normal (direction of force exchange)
            Vector2D delta = pos1.Subtract(pos2);
            double distance = delta.Magnitude;

            // Avoid division by zero if balls are at exact same position
            // (shouldn't happen, but defensive programming prevents crashes)
            if (distance == 0)
            {
                delta = new Vector2D(1, 0);
                distance = 1;
            }

            // Step 2: Normalise the collision vector to create a unit vector
            // This unit vector tells us the direction to separate the balls
            // and also the direction in which momentum exchanges occur
            Vector2D collisionNormal = delta.Normalised;

            // Step 3: Calculate relative velocity (how fast ball1 approaches ball2)
            Vector2D relativeVelocity = vel1.Subtract(vel2);

            // Step 4: Find the component of relative velocity along the collision normal
            // Using dot product: a·b̂ = |a| × cos(θ)
            // This gives us how fast the balls are approaching each other
            // Positive value = approaching, negative = separating
            double velocityAlongNormal = relativeVelocity.DotProduct(collisionNormal);

            // Step 5: Early exit if balls are already separating
            // If velocityAlongNormal > 0, the balls are moving apart.
            // No collision response needed - prevents unnecessary calculations
            // and prevents balls from sticking together after separation
            if (velocityAlongNormal > 0)
            {
                return;
            }

            // Step 6: Calculate the impulse magnitude using coefficient of restitution
            // Formula: j = -(1 + e) × velocityAlongNormal / 2
            // 
            // Why this formula?
            // - (1 + e) incorporates both momentum conservation (the 1) 
            //   and energy loss via restitution (the e)
            // - Dividing by 2 accounts for equal mass distribution
            // - The negative sign corrects the direction
            double impulseScalar = -(1 + coefficientOfRestitution) * velocityAlongNormal / 2;

            // Step 7: Convert impulse scalar into a vector by multiplying by collision normal
            // This vector shows both magnitude and direction of the force exchange
            Vector2D impulse = collisionNormal.Multiply(impulseScalar);

            // Step 8: Apply equal and opposite impulses to both balls
            // Ball1 gains impulse, Ball2 loses it (Newton's Third Law)
            // This ensures total momentum is conserved
            Vector2D newVel1 = vel1.Add(impulse);
            Vector2D newVel2 = vel2.Subtract(impulse);

            // Update ball velocities
            ball1.SetVelocity(newVel1);
            ball2.SetVelocity(newVel2);

            // Step 9: Positional correction - separate overlapping balls
            // Due to discrete time stepping, balls may overlap slightly.
            // This prevents them from sinking into each other.
            SeparateBalls(ball1, ball2, distance);
        }

        /// <summary>
        /// Separates two overlapping balls to prevent them from getting stuck.
        /// 
        /// Moves each ball half the overlap distance along the collision normal.
        /// This is called positional correction and prevents balls from
        /// sinking into each other due to numerical errors.
        /// </summary>
        /// <param name="ball1">First ball</param>
        /// <param name="ball2">Second ball</param>
        /// <param name="currentDistance">Current distance between ball centres</param>
        private void SeparateBalls(Ball ball1, Ball ball2, double currentDistance)
        {
            double combinedRadii = ball1.Radius + ball2.Radius;
            double overlap = combinedRadii - currentDistance;

            // Only separate if there's actual overlap
            if (overlap <= 0) return;

            // Calculate separation vector
            Vector2D delta = ball1.Position.Subtract(ball2.Position);
            Vector2D separationDirection = delta.Normalised;

            // Move each ball half the overlap distance
            double separationAmount = (overlap / 2) + 0.1;  // Small extra to ensure separation

            Vector2D separation = separationDirection.Multiply(separationAmount);

            // Apply separation
            ball1.Position = ball1.Position.Add(separation);
            ball2.Position = ball2.Position.Subtract(separation);
        }

        /// <summary>
        /// Checks and resolves all ball-ball collisions.
        /// 
        /// Uses nested loops to check every pair of balls.
        /// Multiple iterations handle chain reactions where one collision
        /// causes another.
        /// </summary>
        /// <param name="balls">List of all balls</param>
        /// <param name="cueBall">Reference to the cue ball for collision event</param>
        private void CheckBallCollisions(List<Ball> balls, CueBall cueBall)
        {
            // Multiple iterations to handle chain reactions
            for (int iteration = 0; iteration < MAX_COLLISION_ITERATIONS; iteration++)
            {
                bool collisionFound = false;

                // Check every pair of balls
                for (int i = 0; i < balls.Count; i++)
                {
                    for (int j = i + 1; j < balls.Count; j++)
                    {
                        Ball ball1 = balls[i];
                        Ball ball2 = balls[j];

                        // Skip if either ball is not on the table
                        if (!ball1.IsOnTable || !ball2.IsOnTable) continue;

                        // Check for collision
                        if (DetectCollision(ball1, ball2))
                        {
                            // Fire event if cue ball is involved
                            if (ball1 == cueBall && OnCueBallCollision != null)
                            {
                                OnCueBallCollision(ball2);
                            }
                            else if (ball2 == cueBall && OnCueBallCollision != null)
                            {
                                OnCueBallCollision(ball1);
                            }

                            // Resolve the collision
                            ResolveCollision(ball1, ball2);
                            collisionFound = true;
                        }
                    }
                }

                // If no collisions found this iteration, we're done
                if (!collisionFound) break;
            }
        }

        #endregion

        #region Pocket Detection

        /// <summary>
        /// Checks if any balls have been potted (fallen into pockets).
        /// 
        /// </summary>
        /// <param name="balls">List of balls to check</param>
        /// <param name="table">Table containing pockets</param>
        /// <returns>List of balls that were potted</returns>
        public List<Ball> CheckPottedBalls(List<Ball> balls, Table table)
        {
            List<Ball> pottedBalls = new List<Ball>();

            foreach (Ball ball in balls)
            {
                if (!ball.IsOnTable) continue;

                Pocket pocket = ball.CheckPocketed(table.Pockets);
                if (pocket != null)
                {
                    // Ball has been potted
                    ball.IsOnTable = false;
                    ball.Stop();
                    pottedBalls.Add(ball);

                    Debug.WriteLine($"Ball potted in {pocket.TablePosition}!");
                }
            }

            return pottedBalls;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if all balls have stopped moving.
        /// Used to determine when a shot is complete.
        /// </summary>
        /// <param name="balls">List of balls to check</param>
        /// <returns>True if all balls are stationary</returns>
        public bool AllBallsStopped(List<Ball> balls)
        {
            foreach (Ball ball in balls)
            {
                if (ball.IsOnTable && ball.IsMoving)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if all balls have stopped moving.
        /// Overload accepting CueBall and ColouredBalls separately.
        /// </summary>
        public bool AllBallsStopped(CueBall cueBall, List<ColouredBall> colouredBalls)
        {
            if (cueBall.IsOnTable && cueBall.IsMoving) return false;

            foreach (ColouredBall ball in colouredBalls)
            {
                if (ball.IsOnTable && ball.IsMoving)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculates the total kinetic energy of all balls.
        /// Useful for debugging and verifying energy conservation.
        /// 
        /// Kinetic Energy = 0.5 × mass × velocity²
        /// </summary>
        /// <param name="balls">List of balls</param>
        /// <returns>Total kinetic energy</returns>
        public double CalculateTotalKineticEnergy(List<Ball> balls)
        {
            double totalEnergy = 0;

            foreach (Ball ball in balls)
            {
                if (ball.IsOnTable && ball.IsMoving)
                {
                    double speedSquared = ball.Velocity.Magnitude * ball.Velocity.Magnitude;
                    totalEnergy += 0.5 * ball.Mass * speedSquared;
                }
            }

            return totalEnergy;
        }

        #endregion
    }
}