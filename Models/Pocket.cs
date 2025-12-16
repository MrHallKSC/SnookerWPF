using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents one of the six pockets on a snooker table.
    /// 
    /// Snooker tables have:
    /// - 4 corner pockets (slightly larger)
    /// - 2 middle pockets (on the long sides)
    /// 
    /// A ball is "potted" when its centre enters the pocket radius.
    /// This class handles detection of potted balls and visual rendering.
    /// </summary>
    public class Pocket
    {
        #region Enums

        /// <summary>
        /// Identifies the type of pocket.
        /// Corner pockets are slightly larger than middle pockets in real snooker.
        /// </summary>
        public enum PocketType
        {
            Corner,
            Middle
        }

        /// <summary>
        /// Identifies the specific position of each pocket on the table.
        /// Useful for game logic that needs to know which pocket was used.
        /// </summary>
        public enum PocketPosition
        {
            TopLeft,
            TopMiddle,
            TopRight,
            BottomLeft,
            BottomMiddle,
            BottomRight
        }

        #endregion

        #region Constants

        /// <summary>
        /// Default radius for corner pockets (in game units).
        /// Corner pockets are larger to accommodate balls coming from various angles.
        /// </summary>
        private const double CORNER_POCKET_RADIUS = 18.0;

        /// <summary>
        /// Default radius for middle pockets (in game units).
        /// Middle pockets are slightly smaller as balls approach more directly.
        /// </summary>
        private const double MIDDLE_POCKET_RADIUS = 16.0;

        #endregion

        #region Private Fields

        private Vector2D position;
        private double radius;
        private PocketType type;
        private PocketPosition pocketPosition;

        #endregion

        #region Properties

        /// <summary>
        /// Centre position of the pocket on the table.
        /// </summary>
        public Vector2D Position
        {
            get { return position; }
        }

        /// <summary>
        /// Radius of the pocket opening.
        /// A ball is potted when its centre is within this distance from the pocket centre.
        /// </summary>
        public double Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// Type of pocket (Corner or Middle).
        /// </summary>
        public PocketType Type
        {
            get { return type; }
        }

        /// <summary>
        /// Specific position of this pocket on the table.
        /// </summary>
        public PocketPosition TablePosition
        {
            get { return pocketPosition; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pocket at the specified position with automatic radius based on type.
        /// </summary>
        /// <param name="position">Centre position of the pocket</param>
        /// <param name="type">Type of pocket (determines radius)</param>
        /// <param name="tablePosition">Which pocket position this represents</param>
        public Pocket(Vector2D position, PocketType type, PocketPosition tablePosition)
        {
            this.position = position.Copy();
            this.type = type;
            this.pocketPosition = tablePosition;

            // Set radius based on pocket type
            this.radius = (type == PocketType.Corner)
                ? CORNER_POCKET_RADIUS
                : MIDDLE_POCKET_RADIUS;
        }

        /// <summary>
        /// Creates a pocket with a custom radius.
        /// Useful for testing or adjusting difficulty.
        /// </summary>
        /// <param name="position">Centre position of the pocket</param>
        /// <param name="radius">Custom pocket radius</param>
        /// <param name="type">Type of pocket</param>
        /// <param name="tablePosition">Which pocket position this represents</param>
        public Pocket(Vector2D position, double radius, PocketType type,
                      PocketPosition tablePosition)
        {
            this.position = position.Copy();
            this.radius = radius;
            this.type = type;
            this.pocketPosition = tablePosition;
        }

        #endregion

        #region Ball Detection

        /// <summary>
        /// Determines if a ball has been potted in this pocket.
        /// 
        /// A ball is considered potted when the distance from the ball's centre
        /// to the pocket's centre is less than the pocket radius.
        /// 
        /// We don't require the entire ball to be inside - just the centre point.
        /// This creates more forgiving pocket behaviour and is standard in snooker games.
        /// 
        /// Detection formula: distance(ballCentre, pocketCentre) less than pocketRadius
        /// </summary>
        /// <param name="ballPosition">Centre position of the ball</param>
        /// <param name="ballRadius">Radius of the ball (not used in basic detection)</param>
        /// <returns>True if the ball has been potted</returns>
        public bool ContainsBall(Vector2D ballPosition, double ballRadius)
        {
            // Calculate distance from ball centre to pocket centre
            double distance = position.DistanceTo(ballPosition);

            // Ball is potted if its centre is within the pocket radius
            return distance < radius;
        }

        /// <summary>
        /// Checks if a ball is approaching this pocket (within detection range).
        /// Useful for predictive calculations or visual feedback.
        /// </summary>
        /// <param name="ballPosition">Centre position of the ball</param>
        /// <param name="detectionRange">Distance at which to detect approach</param>
        /// <returns>True if ball is within detection range</returns>
        public bool IsBallApproaching(Vector2D ballPosition, double detectionRange)
        {
            double distance = position.DistanceTo(ballPosition);
            return distance < (radius + detectionRange);
        }

        /// <summary>
        /// Calculates the distance from a point to the pocket centre.
        /// </summary>
        /// <param name="point">Point to measure from</param>
        /// <returns>Distance to pocket centre</returns>
        public double DistanceFrom(Vector2D point)
        {
            return position.DistanceTo(point);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Draws the pocket on the canvas.
        /// Pockets are rendered as dark circles that balls fall into.
        /// </summary>
        /// <param name="canvas">WPF Canvas to draw on</param>
        public void Draw(Canvas canvas)
        {
            // Draw the pocket as a dark circle
            Ellipse pocketVisual = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.Black,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };

            // Position the ellipse (offset by radius for centre positioning)
            Canvas.SetLeft(pocketVisual, position.X - radius);
            Canvas.SetTop(pocketVisual, position.Y - radius);

            // Add to canvas
            canvas.Children.Add(pocketVisual);
        }

        /// <summary>
        /// Draws a debug visualisation showing the pocket's detection area.
        /// Useful during development to verify pocket sizes and positions.
        /// </summary>
        /// <param name="canvas">Canvas to draw on</param>
        public void DrawDebug(Canvas canvas)
        {
            // Draw outer detection boundary
            Ellipse debugCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };

            Canvas.SetLeft(debugCircle, position.X - radius);
            Canvas.SetTop(debugCircle, position.Y - radius);
            canvas.Children.Add(debugCircle);

            // Draw centre point
            Ellipse centrePoint = new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(centrePoint, position.X - 2);
            Canvas.SetTop(centrePoint, position.Y - 2);
            canvas.Children.Add(centrePoint);
        }

        #endregion

        #region Utility

        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Pocket[{pocketPosition}, {type}, Position: {position}, Radius: {radius}]";
        }

        #endregion
    }
}