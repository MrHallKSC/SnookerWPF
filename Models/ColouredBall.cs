using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents all coloured balls on the snooker table (reds and colours).
    /// 
    /// This class inherits from Ball and adds:
    /// - Point values for scoring
    /// - Distinction between reds and colours
    /// - Respotting behaviour for colours
    /// - Permanent removal for reds
    /// 
    /// In snooker:
    /// - 15 red balls worth 1 point each (not respotted after potting)
    /// - 6 coloured balls worth 2-7 points (respotted until all reds are gone)
    /// </summary>
    public class ColouredBall : Ball
    {
        #region Enums

        /// <summary>
        /// Enumeration of all ball types with their point values.
        /// Using an enum makes the code more readable and type-safe.
        /// </summary>
        public enum BallType
        {
            Red = 1,        // 15 on table, not respotted
            Yellow = 2,     // Respotted until endgame
            Green = 3,
            Brown = 4,
            Blue = 5,
            Pink = 6,
            Black = 7
        }

        #endregion

        #region Static Colour Definitions

        /// <summary>
        /// Static method to get the correct colour for each ball type.
        /// Keeps colour definitions in one place for consistency.
        /// </summary>
        public static Color GetColourForType(BallType type)
        {
            switch (type)
            {
                case BallType.Red:
                    return Colors.Red;
                case BallType.Yellow:
                    return Colors.Yellow;
                case BallType.Green:
                    return Colors.Green;
                case BallType.Brown:
                    return Color.FromRgb(139, 69, 19);  // Saddle brown
                case BallType.Blue:
                    return Colors.Blue;
                case BallType.Pink:
                    return Colors.Pink;
                case BallType.Black:
                    return Colors.Black;
                default:
                    return Colors.Gray;
            }
        }

        #endregion

        #region Private Fields

        private BallType ballType;
        private int pointValue;
        private bool isRed;
        private Vector2D originalPosition;  // For respotting
        private bool isActive;              // Whether ball is in play

        #endregion

        #region Properties

        /// <summary>
        /// The type of this ball (Red, Yellow, Green, etc.).
        /// Determines point value and respotting behaviour.
        /// </summary>
        public BallType Type
        {
            get { return ballType; }
        }

        /// <summary>
        /// Point value when this ball is potted.
        /// Red = 1, Yellow = 2, Green = 3, Brown = 4, Blue = 5, Pink = 6, Black = 7
        /// </summary>
        public int PointValue
        {
            get { return pointValue; }
        }

        /// <summary>
        /// True if this is a red ball.
        /// Reds behave differently: they are removed when potted, not respotted.
        /// </summary>
        public bool IsRed
        {
            get { return isRed; }
        }

        /// <summary>
        /// The starting position where this ball should be respotted.
        /// Only used for coloured balls (non-reds).
        /// </summary>
        public Vector2D OriginalPosition
        {
            get { return originalPosition; }
        }

        /// <summary>
        /// Whether this ball is currently in play.
        /// False when a red has been potted (permanently removed)
        /// or when a colour is temporarily off the table awaiting respot.
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new coloured ball of the specified type at the given position.
        /// The point value and colour are automatically determined from the type.
        /// </summary>
        /// <param name="type">Type of ball (determines colour and points)</param>
        /// <param name="position">Starting position on the table</param>
        public ColouredBall(BallType type, Vector2D position)
            : base(position, GetColourForType(type))
        {
            this.ballType = type;
            this.pointValue = (int)type;  // Enum value equals point value
            this.isRed = (type == BallType.Red);
            this.originalPosition = position.Copy();
            this.isActive = true;
        }

        /// <summary>
        /// Creates a red ball at the specified position.
        /// Convenience constructor for the common case of creating reds.
        /// </summary>
        /// <param name="position">Position in the triangle</param>
        /// <returns>A new red ball</returns>
        public static ColouredBall CreateRed(Vector2D position)
        {
            return new ColouredBall(BallType.Red, position);
        }

        #endregion

        #region Potting and Respotting

        /// <summary>
        /// Called when this ball is legally potted.
        /// 
        /// For reds: Ball is permanently removed from the game.
        /// For colours: Ball is marked inactive and must be respotted
        ///              (unless we're in the endgame where colours are removed in order).
        /// </summary>
        /// <param name="isEndGame">True if all reds have been potted</param>
        /// <returns>Point value of the potted ball</returns>
        public int Pot(bool isEndGame)
        {
            Stop();
            IsOnTable = false;

            if (isRed || isEndGame)
            {
                // Red balls are always permanently removed
                // Coloured balls are removed in endgame
                isActive = false;
            }
            else
            {
                // Colour needs respotting
                isActive = false;  // Temporarily inactive
            }

            return pointValue;
        }

        /// <summary>
        /// Returns this ball to its original spot position.
        /// Only valid for coloured balls (non-reds) during normal play.
        /// 
        /// If the original spot is occupied, the ball should be placed
        /// on the highest available spot (this is handled by GameManager).
        /// </summary>
        public void Respot()
        {
            if (isRed)
            {
                // Reds cannot be respotted - this shouldn't be called
                throw new InvalidOperationException("Red balls cannot be respotted.");
            }

            Position = originalPosition.Copy();
            Velocity = new Vector2D(0, 0);
            IsOnTable = true;
            isActive = true;
        }

        /// <summary>
        /// Respots the ball at a specific position.
        /// Used when the original spot is occupied by another ball.
        /// </summary>
        /// <param name="position">Alternative position to place the ball</param>
        public void RespotAt(Vector2D position)
        {
            Position = position.Copy();
            Velocity = new Vector2D(0, 0);
            IsOnTable = true;
            isActive = true;
        }

        /// <summary>
        /// Permanently removes the ball from play.
        /// Used for reds when potted, or colours during endgame.
        /// </summary>
        public void Remove()
        {
            Stop();
            IsOnTable = false;
            isActive = false;
        }

        /// <summary>
        /// Resets the ball to its starting position.
        /// Used when starting a new frame.
        /// 
        /// Implementation of abstract method from Ball class.
        /// </summary>
        public override void Reset()
        {
            Position = originalPosition.Copy();
            Velocity = new Vector2D(0, 0);
            IsOnTable = true;
            isActive = true;
        }

        #endregion

        #region Spot Checking

        /// <summary>
        /// Checks if another ball is occupying this ball's original spot.
        /// Used to determine if respotting needs to find an alternative position.
        /// </summary>
        /// <param name="otherBall">Ball to check against</param>
        /// <returns>True if the spot is blocked</returns>
        public bool IsSpotOccupied(Ball otherBall)
        {
            if (otherBall == this) return false;
            if (!otherBall.IsOnTable) return false;

            double distance = originalPosition.DistanceTo(otherBall.Position);
            return distance < (this.Radius + otherBall.Radius);
        }

        /// <summary>
        /// Updates the original position (spot) for this ball.
        /// Used during game setup to configure spot positions.
        /// </summary>
        /// <param name="spotPosition">New spot position</param>
        public void SetSpotPosition(Vector2D spotPosition)
        {
            originalPosition = spotPosition.Copy();
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Draws the coloured ball on the canvas.
        /// Each ball type has its distinctive colour.
        /// 
        /// Implementation of abstract method from Ball class.
        /// </summary>
        /// <param name="canvas">WPF Canvas to draw on</param>
        public override void Draw(Canvas canvas)
        {
            if (!IsOnTable || !isActive) return;

            // Create the ball ellipse with appropriate colour
            Ellipse ballVisual = new Ellipse
            {
                Width = Radius * 2,
                Height = Radius * 2,
                Fill = new SolidColorBrush(Colour)
            };

            // Add outline - white for dark balls, black for light balls
            if (ballType == BallType.Black || ballType == BallType.Blue ||
                ballType == BallType.Green || ballType == BallType.Brown)
            {
                ballVisual.Stroke = Brushes.White;
            }
            else
            {
                ballVisual.Stroke = Brushes.Black;
            }
            ballVisual.StrokeThickness = 1;

            // Position the ellipse (Canvas uses top-left, so offset by radius)
            Canvas.SetLeft(ballVisual, Position.X - Radius);
            Canvas.SetTop(ballVisual, Position.Y - Radius);

            // Add to canvas
            canvas.Children.Add(ballVisual);
        }

        /// <summary>
        /// Draws a marker showing where this ball's spot position is.
        /// Useful for debugging and showing respotting positions.
        /// </summary>
        /// <param name="canvas">Canvas to draw on</param>
        public void DrawSpotMarker(Canvas canvas)
        {
            if (isRed) return;  // Reds don't have individual spots

            Ellipse spotMarker = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Color.FromArgb(100, Colour.R, Colour.G, Colour.B)),
                Stroke = new SolidColorBrush(Colour),
                StrokeThickness = 1
            };

            Canvas.SetLeft(spotMarker, originalPosition.X - 3);
            Canvas.SetTop(spotMarker, originalPosition.Y - 3);
            canvas.Children.Add(spotMarker);
        }

        #endregion

        #region Utility

        /// <summary>
        /// String representation including ball type and point value.
        /// </summary>
        public override string ToString()
        {
            return $"{ballType} Ball (Points: {pointValue}, Active: {isActive}, Position: {Position})";
        }

        #endregion
    }
}