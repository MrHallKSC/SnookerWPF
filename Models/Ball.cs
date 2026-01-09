using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Abstract base class for all balls on the snooker table.
    /// 
    /// This class demonstrates the OOP concept of abstraction - we define the common
    /// properties and behaviours that ALL balls share, while leaving specific
    /// implementations to the derived classes (CueBall and ColouredBall).
    /// 
    /// We make this abstract because we never want to create a generic "Ball" object;
    /// every ball must be either a cue ball or a coloured ball with specific properties.
    /// </summary>
    public abstract class Ball
    {
        #region Constants

        /// <summary>
        /// Standard radius for all snooker balls (in game units).
        /// In real snooker, balls are 52.5mm diameter, but we use abstract units
        /// that will be scaled to screen pixels during rendering.
        /// Protected so subclasses can access if needed.
        /// </summary>
        protected const double STANDARD_RADIUS = 10.0;

        /// <summary>
        /// Velocity threshold below which we consider the ball stopped.
        /// This prevents balls from moving infinitely slowly due to floating-point
        /// precision issues with friction calculations.
        /// </summary>
        protected const double VELOCITY_THRESHOLD = 2.0;

        /// <summary>
        /// Standard mass for all snooker balls (equal mass simplifies collision physics).
        /// In reality all snooker balls weigh the same (approximately 140g).
        /// </summary>
        protected const double STANDARD_MASS = 1.0;

        #endregion

        #region Private Fields

        // Encapsulated fields - access only through properties
        private Vector2D position;
        private Vector2D velocity;
        private double radius;
        private double mass;
        private Color colour;
        private bool isOnTable;

        #endregion

        #region Properties

        /// <summary>
        /// Current position of the ball's centre on the table.
        /// Uses Vector2D for X,Y coordinates.
        /// </summary>
        public Vector2D Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Current velocity vector (speed and direction of movement).
        /// When magnitude is 0, the ball is stationary.
        /// </summary>
        public Vector2D Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Physical radius of the ball. Read-only after construction
        /// as ball size doesn't change during gameplay.
        /// </summary>
        public double Radius
        {
            get { return radius; }
            protected set { radius = value; }
        }

        /// <summary>
        /// Mass of the ball, used in momentum calculations during collisions.
        /// All snooker balls have equal mass, which simplifies the physics.
        /// </summary>
        public double Mass
        {
            get { return mass; }
            protected set { mass = value; }
        }

        /// <summary>
        /// Visual colour of the ball for rendering.
        /// </summary>
        public Color Colour
        {
            get { return colour; }
            protected set { colour = value; }
        }

        /// <summary>
        /// Derived property - returns true if the ball has significant velocity.
        /// Uses VELOCITY_THRESHOLD to avoid floating-point comparison issues.
        /// This is calculated on-demand rather than stored.
        /// </summary>
        public bool IsMoving
        {
            get { return velocity.Magnitude > VELOCITY_THRESHOLD; }
        }

        /// <summary>
        /// Indicates whether the ball is currently in play on the table.
        /// False when ball has been potted (and not yet respotted).
        /// </summary>
        public bool IsOnTable
        {
            get { return isOnTable; }
            set { isOnTable = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Protected constructor - can only be called by derived classes.
        /// This enforces that Ball itself cannot be instantiated directly.
        /// 
        /// Initialises all balls with standard radius and mass, and sets
        /// initial velocity to zero (stationary).
        /// </summary>
        /// <param name="startPosition">Initial position on the table</param>
        /// <param name="ballColour">Visual colour for rendering</param>
        protected Ball(Vector2D startPosition, Color ballColour)
        {
            this.position = startPosition;
            this.velocity = new Vector2D(0, 0);  // Start stationary
            this.radius = STANDARD_RADIUS;
            this.mass = STANDARD_MASS;
            this.colour = ballColour;
            this.isOnTable = true;
        }

        #endregion

        #region Movement Methods

        /// <summary>
        /// Updates the ball's position based on its current velocity.
        /// This implements the basic motion equation: position = position + velocity × time
        /// 
        /// Virtual method allows subclasses to override if they need different behaviour.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update (in seconds)</param>
        public virtual void Update(double deltaTime)
        {
            if (IsMoving && isOnTable)
            {
                // Calculate displacement: velocity × time
                Vector2D displacement = velocity.Multiply(deltaTime);

                // Update position by adding displacement
                position = position.Add(displacement);
            }
        }

        /// <summary>
        /// Applies friction to reduce the ball's velocity over time.
        /// This simulates the resistance of the table cloth.
        /// 
        /// The friction model used: velocity_new = velocity × (1 - friction × deltaTime)
        /// This creates exponential decay, which is physically realistic.
        /// 
        /// If velocity drops below threshold, we set it to zero to stop the ball
        /// completely rather than having it creep infinitely slowly.
        /// </summary>
        /// <param name="frictionCoefficient">Rate of velocity reduction (typically 0.5-2.0)</param>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void ApplyFriction(double frictionCoefficient, double deltaTime)
        {
            if (!IsMoving) return;

            // Calculate friction multiplier (value between 0 and 1)
            double frictionMultiplier = 1.0 - (frictionCoefficient * deltaTime);

            // Clamp to prevent negative multiplier if deltaTime is large
            if (frictionMultiplier < 0) frictionMultiplier = 0;

            // Apply friction to velocity
            velocity = velocity.Multiply(frictionMultiplier);

            // Stop the ball if velocity is below threshold
            if (velocity.Magnitude < VELOCITY_THRESHOLD)
            {
                velocity.Set(0, 0);
            }
        }

        /// <summary>
        /// Sets the ball's velocity directly.
        /// Used when applying collision results or initial shot velocity.
        /// </summary>
        /// <param name="newVelocity">New velocity vector</param>
        public void SetVelocity(Vector2D newVelocity)
        {
            velocity = newVelocity.Copy();
        }

        /// <summary>
        /// Stops the ball immediately by setting velocity to zero.
        /// Used when ball is potted or for game reset.
        /// </summary>
        public void Stop()
        {
            velocity.Set(0, 0);
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// Checks if this ball has been potted by any pocket in the provided list.
        /// A ball is considered potted when its centre is within the pocket radius.
        /// 
        /// Returns the pocket that captured the ball, or null if not potted.
        /// This allows the game logic to know which pocket was used.
        /// </summary>
        /// <param name="pockets">List of pockets to check against</param>
        /// <returns>The pocket that captured the ball, or null</returns>
        public Pocket CheckPocketed(System.Collections.Generic.List<Pocket> pockets)
        {
            foreach (Pocket pocket in pockets)
            {
                if (pocket.ContainsBall(this.position, this.radius))
                {
                    return pocket;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if this ball is colliding with another ball.
        /// Two balls collide when the distance between their centres
        /// is less than the sum of their radii.
        /// </summary>
        /// <param name="other">Ball to check collision with</param>
        /// <returns>True if balls are overlapping</returns>
        public bool IsCollidingWith(Ball other)
        {
            double distance = this.position.DistanceTo(other.position);
            double combinedRadii = this.radius + other.radius;
            return distance < combinedRadii;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Renders the ball on the provided canvas.
        /// 
        /// Abstract method - MUST be implemented by all derived classes.
        /// This enforces that every ball type knows how to draw itself,
        /// while allowing different visual representations.
        /// </summary>
        /// <param name="canvas">WPF Canvas to draw on</param>
        public abstract void Draw(Canvas canvas);

        /// <summary>
        /// Resets the ball to its appropriate starting/respawn position.
        /// 
        /// Abstract because cue ball and coloured balls have different
        /// reset behaviours (cue ball goes to D, colours go to spots).
        /// </summary>
        public abstract void Reset();

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates a visual ellipse representing this ball for WPF rendering.
        /// This is a helper method used by Draw implementations.
        /// 
        /// Protected so only derived classes can use it.
        /// </summary>
        /// <returns>Configured Ellipse shape</returns>
        protected Ellipse CreateBallVisual()
        {
            Ellipse ellipse = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(colour),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            return ellipse;
        }

        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Ball[Pos:{position}, Vel:{velocity}, Moving:{IsMoving}]";
        }

        #endregion
    }
}