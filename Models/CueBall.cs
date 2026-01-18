using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents the white cue ball that players strike.
    /// 
    /// === INHERITANCE FOR A-LEVEL STUDENTS ===
    /// 
    /// This class demonstrates INHERITANCE - a fundamental OOP principle.
    /// 
    /// INHERITANCE DEFINITION:
    /// CueBall : Ball means "CueBall inherits from Ball" or "CueBall extends Ball"
    /// 
    /// WHAT DOES CUEBALL INHERIT?
    /// CueBall automatically gets these from Ball class:
    /// - All properties: Position, Velocity, Radius, Mass, Colour, IsOnTable, IsMoving
    /// - All methods: Move(), SetVelocity(), GetRadius(), etc.
    /// - All protected fields: Can access what Ball allows
    /// 
    /// WHAT IS DIFFERENT ABOUT CUEBALL?
    /// CueBall adds these NEW capabilities unique to the white ball:
    /// - isInHand: Can be freely positioned by player
    /// - aimAngle: Direction player wants to shoot
    /// - shotPower: Power accumulated for the shot
    /// - Strike() method: Converts aim angle + power into velocity
    /// - SetAimDirection() method: Updates aim based on mouse position
    /// - PlaceBall() method: Positions ball when in hand
    /// 
    /// WHY IS THIS USEFUL?
    /// 
    /// 1. CODE REUSE:
    ///    CueBall doesn't rewrite position/velocity code - it inherits it.
    ///    This prevents duplication and bugs.
    /// 
    /// 2. SPECIALIZATION:
    ///    CueBall can do everything Ball does, PLUS more.
    ///    It's a SPECIALIZED type of Ball.
    /// 
    /// 3. IS-A RELATIONSHIP:
    ///    CueBall IS-A Ball - it can be used anywhere Ball is expected
    ///    Example: List&lt;Ball&gt; balls = new List&lt;Ball&gt;();
    ///             balls.Add(cueBall);  // Works! CueBall is-a Ball
    /// 
    /// 4. POLYMORPHISM:
    ///    Different ball types can behave differently:
    ///    cueBall.Strike();          // CueBall-specific method
    ///    redBall.IsMoving;          // Inherited property from Ball
    ///    - Same property/method name can do different things per type
    /// 
    /// DESIGN QUESTION: WHY NOT JUST ADD FIELDS TO BALL?
    /// If we put aiming and striking in the base Ball class,
    /// ColouredBalls would have unused aiming code they'll never use.
    /// This violates the Single Responsibility Principle.
    /// Better design: Put shared code in Ball, specialized code in each subclass.
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
        /// === INVERSE TRIGONOMETRY FOR A-LEVEL STUDENTS ===
        /// 
        /// THE PROBLEM:
        /// Player has: a target point (mouse position)
        /// We need: the angle from the ball to that point
        /// 
        /// SOLUTION: Use inverse tangent function (arctan/atan)
        /// 
        /// BASIC ARCTAN LIMITATION:
        /// The basic arctan function only covers two quadrants (-π/2 to π/2).
        /// If you only know dx and calculated angle = arctan(dy/dx),
        /// you can't distinguish between opposite directions.
        /// 
        /// Example:
        /// arctan(1) = π/4 (northeast)
        /// arctan(-1) = -π/4 (could be southeast OR southwest!)
        /// 
        /// SOLUTION: Use atan2(y, x) - the two-argument arctangent
        /// 
        /// ATAN2 MAGIC:
        /// atan2(dy, dx) examines BOTH components to return the correct angle.
        /// It uses the SIGNS of dy and dx to determine the quadrant:
        /// 
        /// atan2(+, +) → Q1: 0 to π/2       (northeast)
        /// atan2(+, -) → Q2: π/2 to π       (northwest)
        /// atan2(-, -) → Q3: -π to -π/2     (southwest)
        /// atan2(-, +) → Q4: -π/2 to 0      (southeast)
        /// 
        /// Return range: -π to π (covers all 360° directions)
        /// 
        /// EXAMPLE:
        /// Ball at (100, 100), target at (150, 150)
        /// dx = 150 - 100 = 50
        /// dy = 150 - 100 = 50
        /// atan2(50, 50) ≈ 0.785 rad ≈ 45° (northeast) ✓
        /// 
        /// Ball at (100, 100), target at (50, 150)
        /// dx = 50 - 100 = -50
        /// dy = 150 - 100 = 50
        /// atan2(50, -50) ≈ 2.356 rad ≈ 135° (northwest) ✓
        /// 
        /// This is a FUNDAMENTAL technique for converting 2D positions into angles,
        /// used in game development, robotics, and physics simulations worldwide.
        /// </summary>
        /// <param name="targetX">X coordinate to aim towards</param>
        /// <param name="targetY">Y coordinate to aim towards</param>
        public void SetAimDirection(double targetX, double targetY)
        {
            // Calculate displacement vector from ball to target
            double dx = targetX - Position.X;
            double dy = targetY - Position.Y;

            // Use atan2 to get angle from ball to target
            // This is the inverse trigonometric operation: given X and Y components,
            // find the angle they represent. atan2 handles all four quadrants correctly.
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
        /// Adjusts shot power while player holds the shot button.
        /// Power can increase or decrease for oscillating power meter.
        /// </summary>
        /// <param name="increment">Amount to add (can be negative)</param>
        public void ChargePower(double increment)
        {
            shotPower = Math.Max(0, Math.Min(1.0, shotPower + increment));
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
        /// === TRIGONOMETRY FOR A-LEVEL STUDENTS ===
        /// 
        /// COORDINATE SYSTEMS:
        /// - POLAR: Uses angle (θ) and magnitude (r) to describe a vector
        ///   Example: "shoot at 45° with power 500"
        /// - CARTESIAN: Uses x and y coordinates
        ///   Example: "velocity = (353.6, 353.6)"
        /// 
        /// CONVERSION FROM POLAR TO CARTESIAN:
        /// The Pythagorean identity and basic trigonometry give us:
        /// 
        /// x = r × cos(θ)
        /// y = r × sin(θ)
        /// 
        /// Where:
        /// - r is the magnitude (shot power)
        /// - θ is the angle in radians (aimAngle)
        /// - cos and sin convert angular direction to linear components
        /// 
        /// EXAMPLE at 45°:
        /// r = 500, θ = π/4 (45° in radians)
        /// x = 500 × cos(π/4) = 500 × 0.707 ≈ 353.6
        /// y = 500 × sin(π/4) = 500 × 0.707 ≈ 353.6
        /// 
        /// WHY RADIANS?
        /// Trigonometric functions in C# (Math.Cos, Math.Sin) use RADIANS, not degrees.
        /// 1 radian ≈ 57.3°, or π radians = 180°
        /// Conversion: radians = degrees × π/180
        /// 
        /// ANGLE CONVENTION IN THIS CODE:
        /// - 0 radians = rightward (positive X)
        /// - π/2 radians = downward (positive Y)
        /// - π radians = leftward (negative X)
        /// - -π/2 radians = upward (negative Y)
        /// This follows the standard mathematical convention.
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

            // Map normalized power (0-1) to actual power range (MIN_POWER to MAX_POWER)
            // This allows flexible adjustment of game difficulty without changing formulas
            double actualPower = MIN_POWER + (shotPower * (MAX_POWER - MIN_POWER));

            // Convert from POLAR coordinates (angle, magnitude) to CARTESIAN (x, y)
            // This is the fundamental trigonometric conversion needed for physics
            // x = magnitude × cos(angle)
            // y = magnitude × sin(angle)
            double velocityX = actualPower * Math.Cos(aimAngle);
            double velocityY = actualPower * Math.Sin(aimAngle);

            // Apply the calculated velocity vector to the ball
            Velocity = new Vector2D(velocityX, velocityY);

            // Reset power after shot (prevents multiple strikes)
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
        /// Draws the cue stick behind the cue ball.
        /// The cue is positioned opposite to the aim direction and
        /// pulls back as shot power increases.
        /// </summary>
        /// <param name="canvas">Canvas to draw on</param>
        public void DrawCue(Canvas canvas)
        {
            if (!IsOnTable || IsMoving || isInHand) return;

            // Cue dimensions
            double cueLength = 250.0;
            double cueTipWidth = 3.0;
            double cueButtWidth = 8.0;
            double maxPullback = 80.0;
            double tipGap = 5.0;

            // Calculate pullback distance based on shot power
            double pullback = tipGap + (shotPower * maxPullback);

            // Calculate the angle opposite to aim direction (cue is behind the ball)
            double cueAngle = aimAngle + Math.PI;

            // Calculate tip position (closest to ball, pulled back)
            double tipX = Position.X + Math.Cos(cueAngle) * pullback;
            double tipY = Position.Y + Math.Sin(cueAngle) * pullback;

            // Calculate butt position (end of cue, further from ball)
            double buttX = tipX + Math.Cos(cueAngle) * cueLength;
            double buttY = tipY + Math.Sin(cueAngle) * cueLength;

            // Calculate perpendicular angle for cue width
            double perpAngle = cueAngle + (Math.PI / 2);

            // Calculate the four corners of the tapered cue body
            double tipLeftX = tipX + Math.Cos(perpAngle) * (cueTipWidth / 2);
            double tipLeftY = tipY + Math.Sin(perpAngle) * (cueTipWidth / 2);

            double tipRightX = tipX - Math.Cos(perpAngle) * (cueTipWidth / 2);
            double tipRightY = tipY - Math.Sin(perpAngle) * (cueTipWidth / 2);

            double buttLeftX = buttX + Math.Cos(perpAngle) * (cueButtWidth / 2);
            double buttLeftY = buttY + Math.Sin(perpAngle) * (cueButtWidth / 2);

            double buttRightX = buttX - Math.Cos(perpAngle) * (cueButtWidth / 2);
            double buttRightY = buttY - Math.Sin(perpAngle) * (cueButtWidth / 2);

            // Create the main cue body (wooden part)
            Polygon cueBody = new Polygon();
            cueBody.Points = new PointCollection();
            cueBody.Points.Add(new Point(tipLeftX, tipLeftY));
            cueBody.Points.Add(new Point(buttLeftX, buttLeftY));
            cueBody.Points.Add(new Point(buttRightX, buttRightY));
            cueBody.Points.Add(new Point(tipRightX, tipRightY));
            cueBody.Fill = new SolidColorBrush(Color.FromRgb(139, 90, 43));
            cueBody.Stroke = new SolidColorBrush(Color.FromRgb(101, 67, 33));
            cueBody.StrokeThickness = 1;
            canvas.Children.Add(cueBody);

            // Draw the ferrule (white band near tip)
            double ferruleLength = 8.0;
            double ferruleEndX = tipX + Math.Cos(cueAngle) * ferruleLength;
            double ferruleEndY = tipY + Math.Sin(cueAngle) * ferruleLength;

            double ferruleLeftX = ferruleEndX + Math.Cos(perpAngle) * (cueTipWidth / 2);
            double ferruleLeftY = ferruleEndY + Math.Sin(perpAngle) * (cueTipWidth / 2);

            double ferruleRightX = ferruleEndX - Math.Cos(perpAngle) * (cueTipWidth / 2);
            double ferruleRightY = ferruleEndY - Math.Sin(perpAngle) * (cueTipWidth / 2);

            Polygon ferrule = new Polygon();
            ferrule.Points = new PointCollection();
            ferrule.Points.Add(new Point(tipLeftX, tipLeftY));
            ferrule.Points.Add(new Point(ferruleLeftX, ferruleLeftY));
            ferrule.Points.Add(new Point(ferruleRightX, ferruleRightY));
            ferrule.Points.Add(new Point(tipRightX, tipRightY));
            ferrule.Fill = new SolidColorBrush(Colors.Ivory);
            ferrule.Stroke = new SolidColorBrush(Colors.LightGray);
            ferrule.StrokeThickness = 0.5;
            canvas.Children.Add(ferrule);

            // Draw the tip (blue chalk) as a small circle
            Ellipse tip = new Ellipse();
            tip.Width = cueTipWidth + 1;
            tip.Height = cueTipWidth + 1;
            tip.Fill = new SolidColorBrush(Color.FromRgb(70, 130, 180));
            Canvas.SetLeft(tip, tipX - (cueTipWidth + 1) / 2);
            Canvas.SetTop(tip, tipY - (cueTipWidth + 1) / 2);
            canvas.Children.Add(tip);

            // Draw butt cap (rubber end)
            double capLength = 15.0;
            double capStartX = buttX - Math.Cos(cueAngle) * capLength;
            double capStartY = buttY - Math.Sin(cueAngle) * capLength;

            double capStartLeftX = capStartX + Math.Cos(perpAngle) * (cueButtWidth / 2);
            double capStartLeftY = capStartY + Math.Sin(perpAngle) * (cueButtWidth / 2);

            double capStartRightX = capStartX - Math.Cos(perpAngle) * (cueButtWidth / 2);
            double capStartRightY = capStartY - Math.Sin(perpAngle) * (cueButtWidth / 2);

            Polygon buttCap = new Polygon();
            buttCap.Points = new PointCollection();
            buttCap.Points.Add(new Point(capStartLeftX, capStartLeftY));
            buttCap.Points.Add(new Point(buttLeftX, buttLeftY));
            buttCap.Points.Add(new Point(buttRightX, buttRightY));
            buttCap.Points.Add(new Point(capStartRightX, capStartRightY));
            buttCap.Fill = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            buttCap.Stroke = new SolidColorBrush(Colors.Black);
            buttCap.StrokeThickness = 0.5;
            canvas.Children.Add(buttCap);
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