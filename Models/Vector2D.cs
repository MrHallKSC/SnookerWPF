using System;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents a two-dimensional vector for position and velocity calculations.
    /// This class is fundamental to the physics simulation, providing all necessary
    /// vector mathematics operations.
    /// </summary>
    public class Vector2D
    {
        // Private backing fields for encapsulation
        private double x;
        private double y;

        #region Properties

        /// <summary>
        /// Horizontal component of the vector.
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Vertical component of the vector.
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Calculates the length (magnitude) of the vector using Pythagorean theorem.
        /// Formula: √(x² + y²)
        /// This is a read-only derived property.
        /// </summary>
        public double Magnitude
        {
            get { return Math.Sqrt(x * x + y * y); }
        }

        /// <summary>
        /// Returns a unit vector (length 1) in the same direction.
        /// Used when we need direction without magnitude, such as collision normals.
        /// Returns zero vector if magnitude is zero to avoid division by zero.
        /// </summary>
        public Vector2D Normalised
        {
            get
            {
                double mag = Magnitude;
                if (mag == 0)
                {
                    return new Vector2D(0, 0);
                }
                return new Vector2D(x / mag, y / mag);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor - creates a zero vector.
        /// </summary>
        public Vector2D()
        {
            x = 0;
            y = 0;
        }

        /// <summary>
        /// Parameterised constructor - creates vector with specified components.
        /// </summary>
        /// <param name="x">Horizontal component</param>
        /// <param name="y">Vertical component</param>
        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Copy constructor - creates a new vector from an existing one.
        /// Important for avoiding reference issues when we need independent copies.
        /// </summary>
        /// <param name="other">Vector to copy</param>
        public Vector2D(Vector2D other)
        {
            this.x = other.x;
            this.y = other.y;
        }

        #endregion

        #region Vector Operations

        /// <summary>
        /// Adds another vector to this one and returns the result.
        /// Used for combining velocities or updating positions.
        /// Formula: (x₁ + x₂, y₁ + y₂)
        /// </summary>
        /// <param name="other">Vector to add</param>
        /// <returns>New vector representing the sum</returns>
        public Vector2D Add(Vector2D other)
        {
            return new Vector2D(this.x + other.x, this.y + other.y);
        }

        /// <summary>
        /// Subtracts another vector from this one.
        /// Used for finding the vector between two points.
        /// Formula: (x₁ - x₂, y₁ - y₂)
        /// </summary>
        /// <param name="other">Vector to subtract</param>
        /// <returns>New vector representing the difference</returns>
        public Vector2D Subtract(Vector2D other)
        {
            return new Vector2D(this.x - other.x, this.y - other.y);
        }

        /// <summary>
        /// Multiplies the vector by a scalar value.
        /// Used for scaling velocity (e.g., applying friction or shot power).
        /// Formula: (x × scalar, y × scalar)
        /// </summary>
        /// <param name="scalar">Value to multiply by</param>
        /// <returns>New scaled vector</returns>
        public Vector2D Multiply(double scalar)
        {
            return new Vector2D(this.x * scalar, this.y * scalar);
        }

        /// <summary>
        /// Calculates the dot product of this vector with another.
        /// Returns a scalar value used in collision calculations.
        /// The dot product tells us how much two vectors point in the same direction.
        /// Formula: (x₁ × x₂) + (y₁ × y₂)
        /// </summary>
        /// <param name="other">Vector to calculate dot product with</param>
        /// <returns>Scalar dot product value</returns>
        public double DotProduct(Vector2D other)
        {
            return (this.x * other.x) + (this.y * other.y);
        }

        /// <summary>
        /// Calculates the Euclidean distance from this point to another.
        /// Used for collision detection between balls.
        /// Formula: √((x₂-x₁)² + (y₂-y₁)²)
        /// </summary>
        /// <param name="other">Point to measure distance to</param>
        /// <returns>Distance as a positive double</returns>
        public double DistanceTo(Vector2D other)
        {
            double dx = other.x - this.x;
            double dy = other.y - this.y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Calculates the angle from this vector/point to another in radians.
        /// Used for aiming calculations.
        /// Returns angle in range -π to π using atan2 for correct quadrant handling.
        /// </summary>
        /// <param name="other">Target vector/point</param>
        /// <returns>Angle in radians</returns>
        public double AngleTo(Vector2D other)
        {
            double dx = other.x - this.x;
            double dy = other.y - this.y;
            return Math.Atan2(dy, dx);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates a new vector from an angle and magnitude.
        /// Useful for converting shot power and direction into velocity.
        /// Uses trigonometry: x = magnitude × cos(angle), y = magnitude × sin(angle)
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <param name="magnitude">Length of the resulting vector</param>
        /// <returns>New vector with specified angle and magnitude</returns>
        public static Vector2D FromAngle(double angle, double magnitude)
        {
            double x = magnitude * Math.Cos(angle);
            double y = magnitude * Math.Sin(angle);
            return new Vector2D(x, y);
        }

        /// <summary>
        /// Returns a copy of this vector - prevents unintended reference sharing.
        /// </summary>
        /// <returns>New vector with same values</returns>
        public Vector2D Copy()
        {
            return new Vector2D(this.x, this.y);
        }

        /// <summary>
        /// Sets both components at once - useful for resetting or repositioning.
        /// </summary>
        /// <param name="x">New X value</param>
        /// <param name="y">New Y value</param>
        public void Set(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// String representation for debugging purposes.
        /// </summary>
        /// <returns>Formatted string showing X and Y values</returns>
        public override string ToString()
        {
            return $"Vector2D({x:F2}, {y:F2})";
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Allows using + operator for vector addition.
        /// Example: Vector2D result = v1 + v2;
        /// </summary>
        public static Vector2D operator +(Vector2D a, Vector2D b)
        {
            return a.Add(b);
        }

        /// <summary>
        /// Allows using - operator for vector subtraction.
        /// Example: Vector2D result = v1 - v2;
        /// </summary>
        public static Vector2D operator -(Vector2D a, Vector2D b)
        {
            return a.Subtract(b);
        }

        /// <summary>
        /// Allows using * operator for scalar multiplication.
        /// Example: Vector2D result = v1 * 2.0;
        /// </summary>
        public static Vector2D operator *(Vector2D v, double scalar)
        {
            return v.Multiply(scalar);
        }

        /// <summary>
        /// Allows scalar multiplication with scalar first.
        /// Example: Vector2D result = 2.0 * v1;
        /// </summary>
        public static Vector2D operator *(double scalar, Vector2D v)
        {
            return v.Multiply(scalar);
        }

        #endregion
    }
}
