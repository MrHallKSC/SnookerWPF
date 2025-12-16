using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents the physical snooker table and manages the playing surface.
    /// 
    /// A standard snooker table is 12ft x 6ft (3569mm x 1778mm), but we use
    /// abstract game units that scale to screen size.
    /// 
    /// The table includes:
    /// - Playing surface (green baize)
    /// - Six pockets (4 corner, 2 middle)
    /// - Cushions (rails) that balls bounce off
    /// - Baulk line and D (where cue ball is placed)
    /// - Spot positions for coloured balls
    /// </summary>
    public class Table
    {
        #region Constants

        /// <summary>
        /// Coefficient of restitution for cushion bounces.
        /// Value of 0.8 means ball retains 80% of perpendicular velocity.
        /// </summary>
        private const double CUSHION_RESTITUTION = 0.8;

        /// <summary>
        /// Thickness of the cushion (for visual rendering and collision offset).
        /// </summary>
        private const double CUSHION_THICKNESS = 20.0;

        #endregion

        #region Private Fields

        private double width;
        private double height;
        private double baulkLineX;
        private double dRadius;
        private Vector2D dCentre;
        private List<Pocket> pockets;

        // Spot positions for coloured balls
        private Vector2D blackSpot;
        private Vector2D pinkSpot;
        private Vector2D blueSpot;
        private Vector2D brownSpot;
        private Vector2D greenSpot;
        private Vector2D yellowSpot;

        #endregion

        #region Properties

        /// <summary>
        /// Width of the playing surface (excluding cushions).
        /// </summary>
        public double Width
        {
            get { return width; }
        }

        /// <summary>
        /// Height of the playing surface (excluding cushions).
        /// </summary>
        public double Height
        {
            get { return height; }
        }

        /// <summary>
        /// X position of the baulk line (vertical line near bottom of table).
        /// The D is drawn from this line.
        /// </summary>
        public double BaulkLineX
        {
            get { return baulkLineX; }
        }

        /// <summary>
        /// Radius of the D semicircle where cue ball can be placed.
        /// </summary>
        public double DRadius
        {
            get { return dRadius; }
        }

        /// <summary>
        /// Centre point of the D (on the baulk line).
        /// </summary>
        public Vector2D DCentre
        {
            get { return dCentre; }
        }

        /// <summary>
        /// Collection of all six pockets on the table.
        /// </summary>
        public List<Pocket> Pockets
        {
            get { return pockets; }
        }

        /// <summary>
        /// Cushion thickness for collision calculations.
        /// </summary>
        public double CushionThickness
        {
            get { return CUSHION_THICKNESS; }
        }

        // Spot position properties for ball placement
        public Vector2D BlackSpot { get { return blackSpot; } }
        public Vector2D PinkSpot { get { return pinkSpot; } }
        public Vector2D BlueSpot { get { return blueSpot; } }
        public Vector2D BrownSpot { get { return brownSpot; } }
        public Vector2D GreenSpot { get { return greenSpot; } }
        public Vector2D YellowSpot { get { return yellowSpot; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new snooker table with specified dimensions.
        /// Automatically calculates positions for pockets, baulk line, D, and spots.
        /// 
        /// Standard ratio is 2:1 (width:height).
        /// </summary>
        /// <param name="width">Width of playing surface</param>
        /// <param name="height">Height of playing surface</param>
        public Table(double width, double height)
        {
            this.width = width;
            this.height = height;

            // Calculate baulk line position (approximately 1/5 from left)
            this.baulkLineX = width * 0.2;

            // D radius is approximately 1/6 of table height
            this.dRadius = height * 0.16;

            // D centre is on baulk line at table centre height
            this.dCentre = new Vector2D(baulkLineX, height / 2);

            // Initialise pockets
            InitialisePockets();

            // Calculate spot positions
            CalculateSpotPositions();
        }

        #endregion

        #region Initialisation

        /// <summary>
        /// Creates all six pockets at their correct positions.
        /// Corner pockets are at the four corners.
        /// Middle pockets are centred on the long sides.
        /// </summary>
        private void InitialisePockets()
        {
            pockets = new List<Pocket>();

            // Corner pockets (slightly inset from actual corners)
            double cornerOffset = 5.0;

            // Top-left corner
            pockets.Add(new Pocket(
                new Vector2D(cornerOffset, cornerOffset),
                Pocket.PocketType.Corner,
                Pocket.PocketPosition.TopLeft
            ));

            // Top-right corner
            pockets.Add(new Pocket(
                new Vector2D(width - cornerOffset, cornerOffset),
                Pocket.PocketType.Corner,
                Pocket.PocketPosition.TopRight
            ));

            // Bottom-left corner
            pockets.Add(new Pocket(
                new Vector2D(cornerOffset, height - cornerOffset),
                Pocket.PocketType.Corner,
                Pocket.PocketPosition.BottomLeft
            ));

            // Bottom-right corner
            pockets.Add(new Pocket(
                new Vector2D(width - cornerOffset, height - cornerOffset),
                Pocket.PocketType.Corner,
                Pocket.PocketPosition.BottomRight
            ));

            // Middle pockets (centred on long sides)
            // Top middle
            pockets.Add(new Pocket(
                new Vector2D(width / 2, cornerOffset),
                Pocket.PocketType.Middle,
                Pocket.PocketPosition.TopMiddle
            ));

            // Bottom middle
            pockets.Add(new Pocket(
                new Vector2D(width / 2, height - cornerOffset),
                Pocket.PocketType.Middle,
                Pocket.PocketPosition.BottomMiddle
            ));
        }

        /// <summary>
        /// Calculates the spot positions for all coloured balls.
        /// Based on official snooker rules for a 12ft table (playing area 11ft 8.5in x 5ft 10in).
        /// 
        /// Official measurements converted to proportions:
        /// - Baulk line: 29 inches from bottom cushion (≈ 20.7% of length)
        /// - D radius: 11.5 inches (≈ 16.4% of width, i.e., half of table width)
        /// - Blue: Centre of table (50% length, 50% width)
        /// - Pink: 12.75 inches from top cushion (≈ 9.1% from top = 90.9% of length)
        /// - Black: 12.75 inches from top cushion (≈ 9.1% from top = 90.9% of length)
        /// 
        /// Note: Pink and Black have same distance from top, but Pink is the "Pyramid Spot"
        /// which is actually midway between Blue and the top cushion.
        /// 
        /// Layout from baulk (left) to top (right):
        /// Baulk line (Yellow RIGHT, Brown CENTRE, Green LEFT) -> Blue (centre) -> Pink -> Reds -> Black
        /// </summary>
        private void CalculateSpotPositions()
        {
            double centreY = height / 2;

            // Blue spot - exact centre of table
            blueSpot = new Vector2D(width * 0.5, centreY);

            // Pink spot - midway between blue and top cushion (pyramid spot)
            // This is 75% of the way from baulk to top
            pinkSpot = new Vector2D(width * 0.75, centreY);

            // Black spot - 12.75 inches from top cushion on 12ft table
            // Approximately 91% of the way from baulk to top
            blackSpot = new Vector2D(width * 0.91, centreY);

            // Brown spot - on the baulk line, centre (centre of the D)
            brownSpot = new Vector2D(baulkLineX, centreY);

            // Yellow spot - on the baulk line, RIGHT side of the D (top of screen in our orientation)
            // "Right" when facing from baulk towards black
            yellowSpot = new Vector2D(baulkLineX, centreY - dRadius);

            // Green spot - on the baulk line, LEFT side of the D (bottom of screen in our orientation)
            // "Left" when facing from baulk towards black
            greenSpot = new Vector2D(baulkLineX, centreY + dRadius);
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// Checks if a ball is colliding with any cushion and handles the bounce.
        /// 
        /// Cushion collision physics:
        /// - Detect which cushion(s) the ball is touching
        /// - Reflect the velocity component perpendicular to the cushion
        /// - Apply coefficient of restitution (energy loss)
        /// 
        /// Returns true if a collision occurred.
        /// </summary>
        /// <param name="ball">Ball to check and update</param>
        /// <returns>True if ball bounced off a cushion</returns>
        public bool CheckCushionCollision(Ball ball)
        {
            bool collisionOccurred = false;
            Vector2D position = ball.Position;
            Vector2D velocity = ball.Velocity;
            double radius = ball.Radius;

            // Check left cushion
            if (position.X - radius < 0)
            {
                // Ball has hit left cushion
                position.X = radius;  // Prevent ball going through cushion
                velocity = new Vector2D(
                    -velocity.X * CUSHION_RESTITUTION,  // Reverse and reduce X velocity
                    velocity.Y
                );
                collisionOccurred = true;
            }

            // Check right cushion
            if (position.X + radius > width)
            {
                position.X = width - radius;
                velocity = new Vector2D(
                    -velocity.X * CUSHION_RESTITUTION,
                    velocity.Y
                );
                collisionOccurred = true;
            }

            // Check top cushion
            if (position.Y - radius < 0)
            {
                position.Y = radius;
                velocity = new Vector2D(
                    velocity.X,
                    -velocity.Y * CUSHION_RESTITUTION
                );
                collisionOccurred = true;
            }

            // Check bottom cushion
            if (position.Y + radius > height)
            {
                position.Y = height - radius;
                velocity = new Vector2D(
                    velocity.X,
                    -velocity.Y * CUSHION_RESTITUTION
                );
                collisionOccurred = true;
            }

            // Update ball if collision occurred
            if (collisionOccurred)
            {
                ball.Position = position;
                ball.SetVelocity(velocity);
            }

            return collisionOccurred;
        }

        #endregion

        #region D and Baulk Validation

        /// <summary>
        /// Checks if a position is within the D (valid for cue ball placement).
        /// 
        /// The D is a semicircle:
        /// - Centre on the baulk line at table centre
        /// - Opens towards the baulk (left) side
        /// - Position must be within semicircle AND behind baulk line
        /// 
        /// Uses circle equation: (x - cx)² + (y - cy)² less than or equal to r²
        /// Plus constraint: x less than or equal to baulkLineX
        /// </summary>
        /// <param name="position">Position to validate</param>
        /// <returns>True if position is within the D</returns>
        public bool IsInD(Vector2D position)
        {
            // Must be on or behind the baulk line
            if (position.X > baulkLineX)
            {
                return false;
            }

            // Check if within the semicircle
            double distanceFromCentre = position.DistanceTo(dCentre);
            return distanceFromCentre <= dRadius;
        }

        /// <summary>
        /// Checks if a position is behind the baulk line (valid for some situations).
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if position is behind (left of) baulk line</returns>
        public bool IsBehindBaulkLine(Vector2D position)
        {
            return position.X <= baulkLineX;
        }

        /// <summary>
        /// Gets a valid position within the D for cue ball placement.
        /// Returns the centre of the D as a default position.
        /// </summary>
        /// <returns>Default cue ball position in the D</returns>
        public Vector2D GetDefaultCueBallPosition()
        {
            // Return a position slightly behind centre of D
            return new Vector2D(baulkLineX - dRadius * 0.5, height / 2);
        }

        #endregion

        #region Spot Availability

        /// <summary>
        /// Checks if a specific spot position is available (not occupied by another ball).
        /// </summary>
        /// <param name="spotPosition">The spot to check</param>
        /// <param name="balls">List of all balls on the table</param>
        /// <param name="ballRadius">Radius of balls</param>
        /// <returns>True if spot is available</returns>
        public bool IsSpotAvailable(Vector2D spotPosition, List<Ball> balls, double ballRadius)
        {
            foreach (Ball ball in balls)
            {
                if (!ball.IsOnTable) continue;

                double distance = spotPosition.DistanceTo(ball.Position);
                if (distance < ballRadius * 2)
                {
                    return false;  // Spot is occupied
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the position of the triangular rack for red balls.
        /// 
        /// Correct snooker setup:
        /// - The apex ball is placed JUST BEHIND the pink spot (tiny gap)
        /// - The triangle EXPANDS TOWARDS the black ball
        /// - The BASE of the triangle should be close to the black (couple of ball widths)
        /// - 5 rows: 1, 2, 3, 4, 5 balls
        /// 
        /// The triangle must fit between pink (at 75% of table) and black (at 91% of table).
        /// </summary>
        /// <param name="ballRadius">Radius of the balls</param>
        /// <returns>List of 15 positions for red balls in triangle formation</returns>
        public List<Vector2D> GetRedBallPositions(double ballRadius)
        {
            List<Vector2D> positions = new List<Vector2D>();

            // Calculate available space between pink and black
            double availableSpace = blackSpot.X - pinkSpot.X;

            // Ball diameter for spacing
            double diameter = ballRadius * 2;

            // Row spacing for tight triangle packing
            // Using sin(60°) ≈ 0.866 for equilateral triangle formation
            double rowSpacing = diameter * 0.866;

            // Total triangle depth (5 rows, but only 4 gaps between them)
            double triangleDepth = 4 * rowSpacing;

            // Position apex so that:
            // - Small gap after pink (1 ball width)
            // - Base of triangle is about 2 ball widths from black
            double gapAfterPink = diameter;
            double apexX = pinkSpot.X + gapAfterPink + ballRadius;
            double apexY = pinkSpot.Y;

            // 5 rows: 1, 2, 3, 4, 5 balls
            // Triangle extends TOWARDS black (positive X direction)
            for (int row = 0; row < 5; row++)
            {
                int ballsInRow = row + 1;

                // Each row moves towards black (add to X)
                double rowX = apexX + (row * rowSpacing);

                // Centre the row vertically around the apex Y position
                double startY = apexY - (row * ballRadius);

                for (int col = 0; col < ballsInRow; col++)
                {
                    double ballY = startY + (col * diameter);
                    positions.Add(new Vector2D(rowX, ballY));
                }
            }

            return positions;
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Draws the complete table on the canvas.
        /// Order: wooden frame, surface, cushions, pockets (on top), markings, spots.
        /// </summary>
        /// <param name="canvas">WPF Canvas to draw on</param>
        public void Draw(Canvas canvas)
        {
            DrawCushions(canvas);   // Draws frame and surface
            DrawPockets(canvas);    // Pockets drawn on top so they're visible
            DrawBaulkAndD(canvas);
            DrawSpots(canvas);
        }

        /// <summary>
        /// Draws the green baize playing surface.
        /// Note: Now handled within DrawCushions for correct layering.
        /// </summary>
        private void DrawSurface(Canvas canvas)
        {
            // Surface is now drawn as part of DrawCushions
            // This method kept for potential future use
        }

        /// <summary>
        /// Draws the cushions (rails) around the table edge.
        /// Cushions are drawn as separate segments, leaving gaps for the pockets.
        /// </summary>
        private void DrawCushions(Canvas canvas)
        {
            SolidColorBrush cushionBrush = new SolidColorBrush(Color.FromRgb(0, 128, 0)); // Slightly lighter green
            SolidColorBrush railBrush = new SolidColorBrush(Color.FromRgb(101, 67, 33));  // Wood brown

            double pocketGap = 22.0;  // Size of gap for pockets at corners
            double middlePocketGap = 18.0;  // Size of gap for middle pockets
            double cushionDepth = 6.0;  // How thick the cushion strip is

            // Draw wooden rail border (outer frame)
            Rectangle outerFrame = new Rectangle
            {
                Width = width + (CUSHION_THICKNESS * 2),
                Height = height + (CUSHION_THICKNESS * 2),
                Fill = railBrush
            };
            Canvas.SetLeft(outerFrame, -CUSHION_THICKNESS);
            Canvas.SetTop(outerFrame, -CUSHION_THICKNESS);
            canvas.Children.Add(outerFrame);

            // Draw the green playing surface on top (this creates the cushion effect)
            Rectangle surface = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromRgb(0, 100, 0))  // Dark green
            };
            Canvas.SetLeft(surface, 0);
            Canvas.SetTop(surface, 0);
            canvas.Children.Add(surface);

            // Calculate segment lengths for top/bottom cushions
            double horizontalSegmentLength = (width / 2) - pocketGap - middlePocketGap;

            // Draw cushion segments (green strips along the edges, with gaps for pockets)
            // Top cushion - two segments with gap for middle pocket
            DrawCushionSegment(canvas, cushionBrush,
                pocketGap, 0,
                horizontalSegmentLength, cushionDepth);  // Left of middle pocket

            DrawCushionSegment(canvas, cushionBrush,
                (width / 2) + middlePocketGap, 0,
                horizontalSegmentLength, cushionDepth);  // Right of middle pocket

            // Bottom cushion - two segments with gap for middle pocket
            DrawCushionSegment(canvas, cushionBrush,
                pocketGap, height - cushionDepth,
                horizontalSegmentLength, cushionDepth);  // Left of middle pocket

            DrawCushionSegment(canvas, cushionBrush,
                (width / 2) + middlePocketGap, height - cushionDepth,
                horizontalSegmentLength, cushionDepth);  // Right of middle pocket

            // Left cushion - single segment between corner pockets
            DrawCushionSegment(canvas, cushionBrush,
                0, pocketGap,
                cushionDepth, height - (pocketGap * 2));

            // Right cushion - single segment between corner pockets
            DrawCushionSegment(canvas, cushionBrush,
                width - cushionDepth, pocketGap,
                cushionDepth, height - (pocketGap * 2));
        }

        /// <summary>
        /// Draws a single cushion segment.
        /// </summary>
        private void DrawCushionSegment(Canvas canvas, Brush brush,
            double x, double y, double segmentWidth, double segmentHeight)
        {
            Rectangle segment = new Rectangle
            {
                Width = segmentWidth,
                Height = segmentHeight,
                Fill = brush
            };
            Canvas.SetLeft(segment, x);
            Canvas.SetTop(segment, y);
            canvas.Children.Add(segment);
        }

        /// <summary>
        /// Draws all six pockets.
        /// </summary>
        private void DrawPockets(Canvas canvas)
        {
            foreach (Pocket pocket in pockets)
            {
                pocket.Draw(canvas);
            }
        }

        /// <summary>
        /// Draws the baulk line and D semicircle.
        /// </summary>
        private void DrawBaulkAndD(Canvas canvas)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            // Baulk line (vertical line)
            Line baulkLine = new Line
            {
                X1 = baulkLineX,
                Y1 = 0,
                X2 = baulkLineX,
                Y2 = height,
                Stroke = lineBrush,
                StrokeThickness = 2
            };
            canvas.Children.Add(baulkLine);

            // D semicircle (arc opening to the left)
            // Using a Path with an ArcSegment
            PathFigure arcFigure = new PathFigure();
            arcFigure.StartPoint = new Point(baulkLineX, dCentre.Y - dRadius);

            ArcSegment arc = new ArcSegment
            {
                Point = new Point(baulkLineX, dCentre.Y + dRadius),
                Size = new Size(dRadius, dRadius),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = false
            };

            arcFigure.Segments.Add(arc);

            PathGeometry arcGeometry = new PathGeometry();
            arcGeometry.Figures.Add(arcFigure);

            Path dArc = new Path
            {
                Data = arcGeometry,
                Stroke = lineBrush,
                StrokeThickness = 2
            };

            canvas.Children.Add(dArc);
        }

        /// <summary>
        /// Draws small markers at each ball spot position.
        /// </summary>
        private void DrawSpots(Canvas canvas)
        {
            DrawSpot(canvas, blackSpot, Colors.White);
            DrawSpot(canvas, pinkSpot, Colors.White);
            DrawSpot(canvas, blueSpot, Colors.White);
            DrawSpot(canvas, brownSpot, Colors.White);
            DrawSpot(canvas, greenSpot, Colors.White);
            DrawSpot(canvas, yellowSpot, Colors.White);
        }

        /// <summary>
        /// Draws a single spot marker at the specified position.
        /// </summary>
        private void DrawSpot(Canvas canvas, Vector2D position, Color colour)
        {
            Ellipse spot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(colour)
            };

            Canvas.SetLeft(spot, position.X - 3);
            Canvas.SetTop(spot, position.Y - 3);
            canvas.Children.Add(spot);
        }

        #endregion

        #region Utility

        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Table[{width}x{height}, Pockets: {pockets.Count}]";
        }

        #endregion
    }
}