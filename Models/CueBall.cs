using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents the white cue ball that players strike.
    /// 
    /// This class inherits from Ball and adds specific behaviours unique to the cue ball:
    /// - Can be positioned freely when "in hand" (after fouls or at game start)
    /// - Has aiming functionality (angle and power)
    /// - Can be struck by the player
    /// 
    /// This demonstrates inheritance - CueBall IS-A Ball, with additional capabilities.
    /// </summary>
    public class CueBall : Ball
    {
        #region Constants

        /// <summary>
        /// Maximum shot power allowed (arbitrary units, will be tuned during testing).
        /// Prevents unrealistically powerful shots.
        /// </summary>
        private const double MAX_POWER = 1000.0;

        /// <summary>
        /// Minimum shot power - ensures ball always moves when struck.
        /// </summary>
        private const double MIN_POWER = 50.0;

        /// <summary>
        /// Default position in the D (baulk area) for game start and after being potted.
        /// These values will be adjusted based on actual table dimensions.
        /// </summary>
        private const double DEFAULT_X = 200.0;
        private const double DEFAULT_Y = 300.0;

        #endregion

        #region Private Fields

        private bool isInHand;          // True when player can position the ball freely
        private double aimAngle;        // Current aim direction in radians
        private double shotPower;       // Accumulated power for current shot (0-100%)
        private Vector2D defaultPosition;  // Starting position in the D

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the player can freely position the cue ball.
        /// This occurs at the start of the game, after the cue ball is potted,
        /// or after certain fouls.
        /// 
        /// When true, the player can move the ball anywhere within the D.
        /// </summary>
        public bool IsInHand
        {
            get { return isInHand; }
            set { isInHand = value; }
        }

        /// <summary>
        /// Current aiming direction in radians.
        /// 0 = right, π/2 = down, π = left, -π/2 = up
        /// (Using standard mathematical angle convention)
        /// </summary>
        public double AimAngle
        {
            get { return aimAngle; }
            private set { aimAngle = value; }
        }

        /// <summary>
        /// Current shot power as a percentage (0.0 to 1.0).
        /// Displayed to user as 0-100%.
        /// </summary>
        public double ShotPower
        {
            get { return shotPower; }
            private set
            {
                // Clamp between 0 and 1
                shotPower = Math.Max(0, Math.Min(1, value));
            }
        }

        /// <summary>
        /// Returns the shot power as a percentage for display purposes.
        /// </summary>
        public int ShotPowerPercent
        {
            get { return (int)(shotPower * 100); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new cue ball at the specified starting position.
        /// The cue ball is always white.
        /// </summary>
        /// <param name="startPosition">Initial position (typically in the D)</param>
        public CueBall(Vector2D startPosition)
            : base(startPosition, Colors.White)  // Call base constructor with white colour
        {
            this.defaultPosition = startPosition.Copy();
            this.isInHand = true;   // Start with ball in hand
            this.aimAngle = 0;      // Default aim to the right
            this.shotPower = 0;     // No power accumulated
        }

        /// <summary>
        /// Creates a cue ball at the default position.
        /// Overloaded constructor for convenience.
        /// </summary>
        public CueBall()
            : this(new Vector2D(DEFAULT_X, DEFAULT_Y))
        {
        }

        #endregion

        #region Aiming Methods

        /// <summary>
        /// Updates the aim direction based on target coordinates.
        /// Typically called with mouse position to aim toward cursor.
        /// 
        /// Uses trigonometry: angle = atan2(targetY - ballY, targetX - ballX)
        /// atan2 handles all quadrants correctly, returning angle in range -π to π.
        /// </summary>
        /// <param name="targetX">X coordinate to aim towards</param>
        /// <param name="targetY">Y coordinate to aim towards</param>
        public void SetAimDirection(double targetX, double targetY)
        {
            // Calculate angle from ball to target
            double dx = targetX - Position.X;
            double dy = targetY - Position.Y;
            aimAngle = Math.Atan2(dy, dx);
        }

        /// <summary>
        /// Sets the aim direction using a Vector2D target point.
        /// Overloaded version for convenience.
        /// </summary>
        /// <param name="target">Point to aim towards</param>
        public void SetAimDirection(Vector2D target)
        {
            SetAimDirection(target.X, target.Y);
        }

        /// <summary>
        /// Sets the aim angle directly in radians.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        public void SetAimAngle(double angle)
        {
            aimAngle = angle;
        }

        /// <summary>
        /// Increases shot power while player holds the shot button.
        /// Power increases over time up to maximum.
        /// </summary>
        /// <param name="increment">Amount to add (typically based on time held)</param>
        public void ChargePower(double increment)
        {
            shotPower = Math.Min(1.0, shotPower + increment);
        }

        /// <summary>
        /// Resets shot power to zero.
        /// Called after a shot is taken or when aiming is cancelled.
        /// </summary>
        public void ResetPower()
        {
            shotPower = 0;
        }

        #endregion

        #region Shot Execution

        /// <summary>
        /// Executes a shot with the current aim angle and power.
        /// Converts the polar coordinates (angle + power) into a velocity vector.
        /// 
        /// Physics: velocity = (power × cos(angle), power × sin(angle))
        /// This uses trigonometry to convert angle and magnitude to X,Y components.
        /// </summary>
        public void Strike()
        {
            if (isInHand)
            {
                // Can't strike while in hand - must place first
                return;
            }

            if (shotPower <= 0)
            {
                // No power accumulated - no shot
                return;
            }

            // Calculate actual power value from percentage
            double actualPower = MIN_POWER + (shotPower * (MAX_POWER - MIN_POWER));

            // Convert polar (angle, magnitude) to Cartesian (x, y) velocity
            // Using: x = magnitude × cos(angle), y = magnitude × sin(angle)
            double velocityX = actualPower * Math.Cos(aimAngle);
            double velocityY = actualPower * Math.Sin(aimAngle);

            // Set the velocity
            Velocity = new Vector2D(velocityX, velocityY);

            // Reset power after shot
            shotPower = 0;
        }

        /// <summary>
        /// Executes a shot with specified power and angle.
        /// Overloaded version that sets both parameters before striking.
        /// </summary>
        /// <param name="power">Shot power (0.0 to 1.0)</param>
        /// <param name="angle">Aim angle in radians</param>
        public void Strike(double power, double angle)
        {
            this.shotPower = power;
            this.aimAngle = angle;
            this.isInHand = false;  // Must place ball before striking
            Strike();
        }

        #endregion

        #region Positioning Methods

        /// <summary>
        /// Places the cue ball at a specific position when in hand.
        /// Used when player clicks to position the ball in the D.
        /// </summary>
        /// <param name="newPosition">Position to place the ball</param>
        /// <returns>True if placement was successful</returns>
        public bool PlaceBall(Vector2D newPosition)
        {
            if (!isInHand)
            {
                // Can only place when in hand
                return false;
            }

            Position = newPosition.Copy();
            return true;
        }

        /// <summary>
        /// Confirms the ball placement and exits "in hand" state.
        /// Called when player confirms their ball position.
        /// </summary>
        public void ConfirmPlacement()
        {
            isInHand = false;
        }

        /// <summary>
        /// Resets the cue ball to the D after being potted.
        /// Sets the ball to "in hand" state so player can reposition.
        /// 
        /// Implementation of abstract method from Ball class.
        /// </summary>
        public override void Reset()
        {
            Position = defaultPosition.Copy();
            Velocity = new Vector2D(0, 0);
            isInHand = true;
            shotPower = 0;
            IsOnTable = true;
        }

        /// <summary>
        /// Called when the cue ball is potted (foul in snooker).
        /// Removes from table and prepares for repositioning.
        /// </summary>
        public void Pot()
        {
            Stop();
            IsOnTable = false;
            Reset();  // This also sets IsOnTable back to true and isInHand to true
        }

        /// <summary>
        /// Sets a new default position for the cue ball.
        /// Used during game setup to configure the D position.
        /// </summary>
        /// <param name="position">New default position</param>
        public void SetDefaultPosition(Vector2D position)
        {
            defaultPosition = position.Copy();
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Draws the cue ball on the canvas.
        /// The cue ball is white with a subtle gradient to give it depth.
        /// 
        /// Implementation of abstract method from Ball class.
        /// </summary>
        /// <param name="canvas">WPF Canvas to draw on</param>
        public override void Draw(Canvas canvas)
        {
            if (!IsOnTable) return;

            // Create the ball ellipse
            Ellipse ballVisual = new Ellipse
            {
                Width = Radius * 2,
                Height = Radius * 2,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };

            // Position the ellipse (Canvas uses top-left corner, so offset by radius)
            Canvas.SetLeft(ballVisual, Position.X - Radius);
            Canvas.SetTop(ballVisual, Position.Y - Radius);

            // Add to canvas
            canvas.Children.Add(ballVisual);

            // If in hand, draw a subtle indicator
            if (isInHand)
            {
                Ellipse indicator = new Ellipse
                {
                    Width = Radius * 2.5,
                    Height = Radius * 2.5,
                    Stroke = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0)),
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(indicator, Position.X - Radius * 1.25);
                Canvas.SetTop(indicator, Position.Y - Radius * 1.25);
                canvas.Children.Add(indicator);
            }
        }

        /// <summary>
        /// Draws the aiming line from the cue ball in the aim direction.
        /// Shows the player where their shot will go.
        /// </summary>
        /// <param name="canvas">Canvas to draw on</param>
        /// <param name="lineLength">Length of the aiming line</param>
        public void DrawAimLine(Canvas canvas, double lineLength)
        {
            if (!IsOnTable || IsMoving) return;

            // Calculate end point of aim line
            double endX = Position.X + lineLength * Math.Cos(aimAngle);
            double endY = Position.Y + lineLength * Math.Sin(aimAngle);

            // Create the aim line
            Line aimLine = new Line
            {
                X1 = Position.X,
                Y1 = Position.Y,
                X2 = endX,
                Y2 = endY,
                Stroke = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }  // Dashed line
            };

            canvas.Children.Add(aimLine);
        }

        /// <summary>
        /// Draws a power indicator bar showing current shot power.
        /// </summary>
        /// <param name="canvas">Canvas to draw on</param>
        /// <param name="barX">X position of the power bar</param>
        /// <param name="barY">Y position of the power bar</param>
        /// <param name="barWidth">Width of the power bar</param>
        /// <param name="barHeight">Height of the power bar</param>
        public void DrawPowerBar(Canvas canvas, double barX, double barY,
                                  double barWidth, double barHeight)
        {
            // Background bar (empty)
            System.Windows.Shapes.Rectangle background = new System.Windows.Shapes.Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = Brushes.DarkGray,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(background, barX);
            Canvas.SetTop(background, barY);
            canvas.Children.Add(background);

            // Filled portion based on power
            double filledWidth = barWidth * shotPower;
            if (filledWidth > 0)
            {
                // Colour gradient from green (low power) to red (high power)
                byte red = (byte)(255 * shotPower);
                byte green = (byte)(255 * (1 - shotPower));
                Color powerColour = Color.FromRgb(red, green, 0);

                System.Windows.Shapes.Rectangle filled = new System.Windows.Shapes.Rectangle
                {
                    Width = filledWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush(powerColour)
                };
                Canvas.SetLeft(filled, barX);
                Canvas.SetTop(filled, barY);
                canvas.Children.Add(filled);
            }
        }

        #endregion
    }
}