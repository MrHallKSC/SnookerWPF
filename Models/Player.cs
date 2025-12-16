using System;
using System.Diagnostics;

namespace SnookerGame.Models
{
    /// <summary>
    /// Represents a human player in the snooker game.
    /// 
    /// This class tracks all player-related statistics:
    /// - Current frame score
    /// - Current break (points in current visit)
    /// - Frames won in the match
    /// - Highest break achieved
    /// 
    /// A "break" in snooker is the number of points scored in one visit
    /// to the table (consecutive pots without missing or fouling).
    /// </summary>
    public class Player
    {
        #region Private Fields

        private string name;
        private int score;
        private int framesWon;
        private int currentBreak;
        private int highestBreak;
        private int foulsCommitted;
        private int totalPointsScored;

        #endregion

        #region Properties

        /// <summary>
        /// Player's display name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value ?? "Player"; }
        }

        /// <summary>
        /// Current score in this frame.
        /// Includes points from pots and points from opponent's fouls.
        /// </summary>
        public int Score
        {
            get { return score; }
            private set { score = Math.Max(0, value); }
        }

        /// <summary>
        /// Number of frames won in this match.
        /// </summary>
        public int FramesWon
        {
            get { return framesWon; }
            private set { framesWon = Math.Max(0, value); }
        }

        /// <summary>
        /// Points scored in the current visit to the table.
        /// Resets to 0 when the player misses, fouls, or turn ends.
        /// </summary>
        public int CurrentBreak
        {
            get { return currentBreak; }
            private set { currentBreak = Math.Max(0, value); }
        }

        /// <summary>
        /// Highest break achieved by this player in the match.
        /// Updated when a break ends if it exceeds the previous best.
        /// </summary>
        public int HighestBreak
        {
            get { return highestBreak; }
            private set { highestBreak = Math.Max(0, value); }
        }

        /// <summary>
        /// Total number of fouls committed by this player.
        /// </summary>
        public int FoulsCommitted
        {
            get { return foulsCommitted; }
            private set { foulsCommitted = Math.Max(0, value); }
        }

        /// <summary>
        /// Total points scored across all frames (for statistics).
        /// </summary>
        public int TotalPointsScored
        {
            get { return totalPointsScored; }
            private set { totalPointsScored = Math.Max(0, value); }
        }

        /// <summary>
        /// Returns true if the player is currently on a break (has potted at least one ball).
        /// </summary>
        public bool IsOnBreak
        {
            get { return currentBreak > 0; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new player with the specified name.
        /// All scores start at zero.
        /// </summary>
        /// <param name="name">Player's display name</param>
        public Player(string name)
        {
            this.name = name ?? "Player";
            this.score = 0;
            this.framesWon = 0;
            this.currentBreak = 0;
            this.highestBreak = 0;
            this.foulsCommitted = 0;
            this.totalPointsScored = 0;
        }

        /// <summary>
        /// Creates a new player with a default name.
        /// </summary>
        public Player() : this("Player")
        {
        }

        #endregion

        #region Scoring Methods

        /// <summary>
        /// Adds points to the player's score and current break.
        /// Called when the player legally pots a ball.
        /// 
        /// Points are added to:
        /// - Frame score (cumulative for this frame)
        /// - Current break (resets on miss/foul)
        /// - Total points (match statistics)
        /// </summary>
        /// <param name="points">Points to add (typically 1-7)</param>
        public void AddPoints(int points)
        {
            if (points <= 0) return;

            score += points;
            currentBreak += points;
            totalPointsScored += points;

            Debug.WriteLine($"{name} scored {points} points. Break: {currentBreak}, Total: {score}");
        }

        /// <summary>
        /// Adds foul points to the player's score.
        /// Called when the opponent commits a foul.
        /// 
        /// Foul points are added to frame score but NOT to the current break,
        /// as the player didn't pot a ball.
        /// </summary>
        /// <param name="points">Foul points awarded (minimum 4 in snooker)</param>
        public void AddFoulPoints(int points)
        {
            if (points <= 0) return;

            score += points;

            Debug.WriteLine($"{name} awarded {points} foul points. Total: {score}");
        }

        /// <summary>
        /// Ends the current break.
        /// Called when the player misses a pot, commits a foul, or the frame ends.
        /// 
        /// Updates the highest break if the current break exceeds it.
        /// Resets current break to zero.
        /// </summary>
        public void EndBreak()
        {
            // Check if this was a new highest break
            if (currentBreak > highestBreak)
            {
                highestBreak = currentBreak;
                Debug.WriteLine($"{name} achieved new highest break: {highestBreak}");
            }

            if (currentBreak > 0)
            {
                Debug.WriteLine($"{name}'s break ended at {currentBreak}");
            }

            currentBreak = 0;
        }

        /// <summary>
        /// Records that the player committed a foul.
        /// </summary>
        public void CommitFoul()
        {
            foulsCommitted++;
            EndBreak();  // Foul ends the break

            Debug.WriteLine($"{name} committed a foul. Total fouls: {foulsCommitted}");
        }

        #endregion

        #region Frame Management

        /// <summary>
        /// Awards a frame win to this player.
        /// Called when the player wins the current frame.
        /// </summary>
        public void WinFrame()
        {
            framesWon++;
            EndBreak();  // Ensure break is recorded

            Debug.WriteLine($"{name} won the frame! Frames won: {framesWon}");
        }

        /// <summary>
        /// Resets the player's score for a new frame.
        /// Preserves frames won, highest break, and match statistics.
        /// </summary>
        public void ResetForNewFrame()
        {
            EndBreak();  // Record any ongoing break
            score = 0;

            Debug.WriteLine($"{name} reset for new frame");
        }

        /// <summary>
        /// Completely resets the player for a new match.
        /// Clears all scores and statistics.
        /// </summary>
        public void ResetForNewMatch()
        {
            score = 0;
            framesWon = 0;
            currentBreak = 0;
            highestBreak = 0;
            foulsCommitted = 0;
            totalPointsScored = 0;

            Debug.WriteLine($"{name} reset for new match");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Returns a formatted string showing the player's current status.
        /// </summary>
        public string GetStatusString()
        {
            return $"{name}: {score} pts (Break: {currentBreak})";
        }

        /// <summary>
        /// Returns a formatted string showing match statistics.
        /// </summary>
        public string GetStatsString()
        {
            return $"{name} - Frames: {framesWon}, Highest Break: {highestBreak}, Total Points: {totalPointsScored}";
        }

        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Player[{name}, Score: {score}, Break: {currentBreak}, Frames: {framesWon}]";
        }

        #endregion
    }
}