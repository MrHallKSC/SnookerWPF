using System;
using System.Collections.Generic;
using System.Diagnostics;
using SnookerGame.Models;

namespace SnookerGame.Engine
{
    /// <summary>
    /// Enumerates the possible states of the game.
    /// Used to control game flow and UI interactions.
    /// 
    /// === STATE MACHINE FOR A-LEVEL STUDENTS ===
    /// 
    /// A STATE MACHINE is a design pattern for managing complex behavior.
    /// Instead of scattered if-else statements, we define:
    /// 1. Discrete STATES the system can be in
    /// 2. Valid TRANSITIONS between states
    /// 3. Actions to take in each state
    /// 
    /// SNOOKER GAME STATES:
    /// 
    /// PlacingCueBall → Aiming → BallsMoving → ProcessingShot → TurnEnd → (back to Aiming or Placing)
    ///                                                              ↓
    ///                                                         FrameOver
    ///                                                              ↓
    ///                                                          MatchOver
    /// 
    /// Each state represents what the game is currently doing:
    /// - PlacingCueBall: Player clicked in D, positioning white ball
    /// - Aiming: Ball is placed, player aims and builds shot power
    /// - BallsMoving: Shot executed, physics running, balls moving
    /// - ProcessingShot: Balls stopped, checking results (fouls, potted balls, scoring)
    /// - TurnEnd: Determining if player continues or turn passes
    /// - FrameOver: All balls potted/tabled, frame result known
    /// - MatchOver: Enough frames won, match complete
    /// 
    /// WHY USE STATE MACHINES?
    /// 1. CLARITY: Code shows exactly what's happening
    /// 2. CORRECTNESS: Impossible to perform invalid actions
    ///    (e.g., can't strike ball while state is FrameOver)
    /// 3. MAINTAINABILITY: Easy to modify rules by changing state logic
    /// 4. DEBUGGING: State name tells you exactly where problem is
    /// </summary>
    public enum GameState
    {
        /// <summary>Player is positioning the cue ball in the D.</summary>
        PlacingCueBall,

        /// <summary>Player is aiming and can take a shot.</summary>
        Aiming,

        /// <summary>Shot has been taken, balls are in motion.</summary>
        BallsMoving,

        /// <summary>Balls have stopped, processing the shot result.</summary>
        ProcessingShot,

        /// <summary>Turn is ending, switching players or continuing break.</summary>
        TurnEnd,

        /// <summary>Frame has ended, showing results.</summary>
        FrameOver,

        /// <summary>Match has ended, showing final results.</summary>
        MatchOver,

        /// <summary>Game is paused.</summary>
        Paused
    }

    /// <summary>
    /// Enumerates what type of ball the player must hit next.
    /// </summary>
    public enum TargetBallType
    {
        /// <summary>Player must hit a red ball.</summary>
        Red,

        /// <summary>Player must hit a colour (after potting a red).</summary>
        Colour,

        /// <summary>Player must hit a specific colour (endgame).</summary>
        Yellow,
        Green,
        Brown,
        Blue,
        Pink,
        Black
    }

    /// <summary>
    /// Manages the overall game flow, rules, and state for the snooker game.
    /// 
    /// === GAME CONTROL ARCHITECTURE FOR A-LEVEL STUDENTS ===
    /// 
    /// RESPONSIBILITIES (Single Responsibility Principle):
    /// This class handles LOGIC and RULES, not graphics or physics.
    /// 
    /// 1. GAME STATE MANAGEMENT:
    ///    Tracks current GameState and enforces valid transitions.
    ///    Only certain actions are valid in each state.
    ///    
    /// 2. PLAYER MANAGEMENT:
    ///    Tracks which player's turn it is, scores, frames won.
    ///    
    /// 3. RULE ENFORCEMENT (Complex algorithm):
    ///    This is the GAME LOGIC - the most complex part:
    ///    - Tracking which ball must be hit (target ball)
    ///    - Detecting fouls (hitting wrong ball, missing, jumping)
    ///    - Detecting potted balls and awarding points
    ///    - Managing respotting (where balls go back)
    ///    - Detecting break end (miss or foul)
    ///    - Determining turn winner
    ///    - Checking frame completion
    ///    - Checking match completion
    ///    
    /// 4. SCORE AND BREAK TRACKING:
    ///    Points from potted balls, penalty points, break streaks.
    ///    
    /// INTERACTION WITH OTHER CLASSES:
    /// PhysicsEngine ← → GameManager ← → Player & UI
    ///    handles                handles
    ///    movements          rules & scoring
    /// 
    /// SNOOKER RULES SIMPLIFIED:
    /// - Player hits cue ball to pot other balls
    /// - Reds (1 pt) then Colours (2-7 pts) alternately until reds gone
    /// - Miss = turn ends
    /// - Foul = opponent gets penalty points, turn might end
    /// - Fouls on reds: minimum 4 points to opponent
    /// - Fouls on colours: loser gets that colour's point value
    /// - Match won by first to win required frames (usually best of 3, 5, 7, etc.)
    /// 
    /// This class acts as the central controller, coordinating between
    /// the physics engine, players, and UI.
    /// </summary>
    public class GameManager
    {
        #region Constants

        /// <summary>
        /// Minimum foul penalty in snooker is 4 points.
        /// </summary>
        private const int MINIMUM_FOUL_POINTS = 4;

        /// <summary>
        /// Number of frames needed to win a match (best of 3 = 2 frames).
        /// </summary>
        private const int FRAMES_TO_WIN = 2;

        #endregion

        #region Private Fields

        private Player[] players;
        private int currentPlayerIndex;
        private GameState gameState;
        private TargetBallType targetBallType;

        private Table table;
        private CueBall cueBall;
        private List<ColouredBall> colouredBalls;

        private int redsRemaining;
        private bool isEndGame;

        // Shot tracking
        private Ball firstBallHit;
        private List<Ball> ballsPottedThisShot;
        private bool cueBallPotted;
        private bool foulCommitted;
        private int foulPoints;
        private string foulReason;

        #endregion

        #region Properties

        /// <summary>
        /// The player whose turn it currently is.
        /// </summary>
        public Player CurrentPlayer
        {
            get { return players[currentPlayerIndex]; }
        }

        /// <summary>
        /// The player who is not currently playing.
        /// </summary>
        public Player OtherPlayer
        {
            get { return players[1 - currentPlayerIndex]; }
        }

        /// <summary>
        /// Array of both players.
        /// </summary>
        public Player[] Players
        {
            get { return players; }
        }

        /// <summary>
        /// Current state of the game.
        /// </summary>
        public GameState State
        {
            get { return gameState; }
        }

        /// <summary>
        /// What type of ball the current player must hit.
        /// </summary>
        public TargetBallType TargetBall
        {
            get { return targetBallType; }
        }

        /// <summary>
        /// Number of red balls still on the table.
        /// </summary>
        public int RedsRemaining
        {
            get { return redsRemaining; }
        }

        /// <summary>
        /// True if all reds have been potted and we're in the endgame
        /// (potting colours in order).
        /// </summary>
        public bool IsEndGame
        {
            get { return isEndGame; }
        }

        /// <summary>
        /// Reference to the table.
        /// </summary>
        public Table Table
        {
            get { return table; }
        }

        /// <summary>
        /// Reference to the cue ball.
        /// </summary>
        public CueBall CueBall
        {
            get { return cueBall; }
        }

        /// <summary>
        /// List of all coloured balls (including reds).
        /// </summary>
        public List<ColouredBall> ColouredBalls
        {
            get { return colouredBalls; }
        }

        /// <summary>
        /// The first ball that was hit by the cue ball this shot.
        /// Used for foul detection.
        /// </summary>
        public Ball FirstBallHit
        {
            get { return firstBallHit; }
        }

        /// <summary>
        /// Whether a foul was committed on the current/last shot.
        /// </summary>
        public bool FoulCommitted
        {
            get { return foulCommitted; }
        }

        /// <summary>
        /// Description of the foul committed.
        /// </summary>
        public string FoulReason
        {
            get { return foulReason; }
        }

        /// <summary>
        /// Points available on the table.
        /// </summary>
        public int PointsOnTable
        {
            get { return CalculatePointsOnTable(); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new GameManager with the specified players, table, and balls.
        /// </summary>
        public GameManager(Player player1, Player player2, Table table,
                          CueBall cueBall, List<ColouredBall> colouredBalls)
        {
            players = new Player[] { player1, player2 };
            currentPlayerIndex = 0;

            this.table = table;
            this.cueBall = cueBall;
            this.colouredBalls = colouredBalls;

            ballsPottedThisShot = new List<Ball>();

            // Initialise game state
            StartNewFrame();
        }

        #endregion

        #region Game Flow Methods

        /// <summary>
        /// Starts a new frame, resetting balls and scores.
        /// </summary>
        public void StartNewFrame()
        {
            Debug.WriteLine("Starting new frame...");

            // Reset player scores for new frame
            players[0].ResetForNewFrame();
            players[1].ResetForNewFrame();

            // Count reds
            redsRemaining = 0;
            foreach (ColouredBall ball in colouredBalls)
            {
                if (ball.IsRed)
                {
                    redsRemaining++;
                }
            }

            isEndGame = false;
            targetBallType = TargetBallType.Red;

            // Reset shot tracking
            ResetShotTracking();

            // Set initial game state
            if (cueBall.IsInHand)
            {
                gameState = GameState.PlacingCueBall;
            }
            else
            {
                gameState = GameState.Aiming;
            }

            Debug.WriteLine($"Frame started. {redsRemaining} reds on table. {CurrentPlayer.Name} to break.");
        }

        /// <summary>
        /// Called when a shot is about to be taken.
        /// Resets shot tracking variables.
        /// </summary>
        public void OnShotStarted()
        {
            ResetShotTracking();
            gameState = GameState.BallsMoving;

            Debug.WriteLine($"{CurrentPlayer.Name} takes a shot. Target: {targetBallType}");
        }

        /// <summary>
        /// Resets variables that track what happened during a shot.
        /// </summary>
        private void ResetShotTracking()
        {
            firstBallHit = null;
            ballsPottedThisShot.Clear();
            cueBallPotted = false;
            foulCommitted = false;
            foulPoints = 0;
            foulReason = "";
        }

        /// <summary>
        /// Records the first ball hit by the cue ball.
        /// Called by physics engine during collision detection.
        /// </summary>
        public void RecordFirstBallHit(Ball ball)
        {
            if (firstBallHit == null && ball != cueBall)
            {
                firstBallHit = ball;
                Debug.WriteLine($"First ball hit: {ball}");
            }
        }

        /// <summary>
        /// Records a ball being potted.
        /// Called when physics engine detects a ball entering a pocket.
        /// </summary>
        public void RecordBallPotted(Ball ball)
        {
            if (ball is CueBall)
            {
                cueBallPotted = true;
                Debug.WriteLine("Cue ball potted!");
            }
            else
            {
                ballsPottedThisShot.Add(ball);
                Debug.WriteLine($"Ball potted: {ball}");
            }
        }

        /// <summary>
        /// Called when all balls have stopped moving.
        /// Processes the shot result and determines next action.
        /// </summary>
        public void ProcessShot()
        {
            gameState = GameState.ProcessingShot;
            Debug.WriteLine("Processing shot result...");

            // Check for fouls
            CheckForFouls();

            if (foulCommitted)
            {
                // Handle foul
                HandleFoul();
            }
            else if (ballsPottedThisShot.Count > 0)
            {
                // Legal pot(s) made
                HandleLegalPots();
            }
            else
            {
                // No balls potted, no foul - just a miss
                HandleMiss();
            }

            // Check if frame is over
            if (CheckFrameEnd())
            {
                EndFrame();
            }
            else
            {
                // Determine next game state
                if (cueBallPotted || cueBall.IsInHand)
                {
                    gameState = GameState.PlacingCueBall;
                }
                else
                {
                    gameState = GameState.Aiming;
                }
            }
        }

        #endregion

        #region Foul Detection

        /// <summary>
        /// Checks if any fouls were committed during the shot.
        /// 
        /// Fouls in snooker include:
        /// - Potting the cue ball
        /// - Failing to hit any ball
        /// - Hitting wrong ball first (e.g., hitting colour when red is target)
        /// - Potting wrong ball
        /// </summary>
        private void CheckForFouls()
        {
            foulCommitted = false;
            foulPoints = MINIMUM_FOUL_POINTS;
            foulReason = "";

            // Foul 1: Potting the cue ball (check this first as it's most obvious)
            if (cueBallPotted)
            {
                foulCommitted = true;
                foulReason = "Cue ball potted";
                foulPoints = Math.Max(foulPoints, GetTargetBallValue());
                Debug.WriteLine("Foul: Cue ball potted");
                return;  // No need to check other fouls
            }

            // Foul 2: Failing to hit any ball
            if (firstBallHit == null)
            {
                foulCommitted = true;
                foulReason = "Failed to hit any ball";
                foulPoints = Math.Max(foulPoints, GetTargetBallValue());
                Debug.WriteLine("Foul: No ball hit");
                return;
            }

            // Foul 3: Hitting wrong ball first
            if (!IsCorrectBallHit(firstBallHit))
            {
                foulCommitted = true;
                string hitBallName = GetBallDescription(firstBallHit);
                string targetName = targetBallType == TargetBallType.Colour ? "a colour" : targetBallType.ToString().ToLower();
                foulReason = $"Hit {hitBallName} first\n(should hit {targetName})";
                foulPoints = Math.Max(foulPoints, GetBallValue(firstBallHit));
                foulPoints = Math.Max(foulPoints, GetTargetBallValue());
                Debug.WriteLine($"Foul: Wrong ball hit first");
                return;
            }

            // Foul 4: Potting wrong ball(s)
            foreach (Ball ball in ballsPottedThisShot)
            {
                if (!IsLegalPot(ball))
                {
                    foulCommitted = true;
                    string pottedBallName = GetBallDescription(ball);
                    foulReason = $"Illegally potted {pottedBallName}";
                    foulPoints = Math.Max(foulPoints, GetBallValue(ball));
                    Debug.WriteLine($"Foul: Illegally potted {pottedBallName}");
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if the correct ball was hit first.
        /// </summary>
        private bool IsCorrectBallHit(Ball ball)
        {
            if (!(ball is ColouredBall colouredBall)) return false;

            if (targetBallType == TargetBallType.Red)
            {
                return colouredBall.IsRed;
            }
            else if (targetBallType == TargetBallType.Colour)
            {
                return !colouredBall.IsRed;
            }
            else
            {
                // Endgame - must hit specific colour
                return GetEndgameTargetType(colouredBall.Type) == targetBallType;
            }
        }

        /// <summary>
        /// Checks if potting this ball is legal.
        /// </summary>
        private bool IsLegalPot(Ball ball)
        {
            if (!(ball is ColouredBall colouredBall)) return false;

            if (targetBallType == TargetBallType.Red)
            {
                return colouredBall.IsRed;
            }
            else if (targetBallType == TargetBallType.Colour)
            {
                return !colouredBall.IsRed;
            }
            else
            {
                // Endgame - must pot specific colour
                return GetEndgameTargetType(colouredBall.Type) == targetBallType;
            }
        }

        /// <summary>
        /// Converts a ColouredBall.BallType to TargetBallType for endgame comparison.
        /// </summary>
        private TargetBallType GetEndgameTargetType(ColouredBall.BallType ballType)
        {
            switch (ballType)
            {
                case ColouredBall.BallType.Yellow: return TargetBallType.Yellow;
                case ColouredBall.BallType.Green: return TargetBallType.Green;
                case ColouredBall.BallType.Brown: return TargetBallType.Brown;
                case ColouredBall.BallType.Blue: return TargetBallType.Blue;
                case ColouredBall.BallType.Pink: return TargetBallType.Pink;
                case ColouredBall.BallType.Black: return TargetBallType.Black;
                default: return TargetBallType.Red;
            }
        }

        /// <summary>
        /// Gets the point value associated with the current target ball.
        /// </summary>
        private int GetTargetBallValue()
        {
            switch (targetBallType)
            {
                case TargetBallType.Red: return 1;
                case TargetBallType.Colour: return 7; // Could be any colour, use max
                case TargetBallType.Yellow: return 2;
                case TargetBallType.Green: return 3;
                case TargetBallType.Brown: return 4;
                case TargetBallType.Blue: return 5;
                case TargetBallType.Pink: return 6;
                case TargetBallType.Black: return 7;
                default: return 4;
            }
        }

        /// <summary>
        /// Gets the point value of a ball.
        /// </summary>
        private int GetBallValue(Ball ball)
        {
            if (ball is ColouredBall colouredBall)
            {
                return colouredBall.PointValue;
            }
            return 4; // Default minimum
        }

        /// <summary>
        /// Gets a description of a ball for foul messages.
        /// </summary>
        private string GetBallDescription(Ball ball)
        {
            if (ball is ColouredBall colouredBall)
            {
                return colouredBall.Type.ToString();
            }
            return "ball";
        }

        /// <summary>
        /// Calculates the foul points to award.
        /// In snooker, foul points are the higher of:
        /// - 4 points (minimum)
        /// - Value of ball hit
        /// - Value of ball potted illegally
        /// - Value of ball "on" (target ball)
        /// </summary>
        private int CalculateFoulPoints()
        {
            return Math.Max(MINIMUM_FOUL_POINTS, foulPoints);
        }

        #endregion

        #region Shot Result Handling

        /// <summary>
        /// Handles the result when a foul is committed.
        /// </summary>
        private void HandleFoul()
        {
            int points = CalculateFoulPoints();

            // Current player committed foul
            CurrentPlayer.CommitFoul();

            // Other player gets the points
            OtherPlayer.AddFoulPoints(points);

            Debug.WriteLine($"Foul by {CurrentPlayer.Name}. {points} points to {OtherPlayer.Name}");

            // Handle any balls that need respotting
            RespotBalls();

            // Switch players
            SwitchPlayer();

            // Reset target to red (unless in endgame)
            if (!isEndGame)
            {
                targetBallType = TargetBallType.Red;
            }
        }

        /// <summary>
        /// Handles legal pots made during the shot.
        /// </summary>
        private void HandleLegalPots()
        {
            int totalPoints = 0;

            foreach (Ball ball in ballsPottedThisShot)
            {
                if (ball is ColouredBall colouredBall)
                {
                    totalPoints += colouredBall.PointValue;

                    if (colouredBall.IsRed)
                    {
                        // Red is permanently removed
                        redsRemaining--;
                        colouredBall.Remove();
                    }
                    else if (!isEndGame)
                    {
                        // Colour needs respotting (unless endgame)
                        RespotColour(colouredBall);
                    }
                    else
                    {
                        // Endgame - colour is removed
                        colouredBall.Remove();
                    }
                }
            }

            // Add points to current player
            CurrentPlayer.AddPoints(totalPoints);

            Debug.WriteLine($"{CurrentPlayer.Name} scored {totalPoints} points");

            // Update target ball
            UpdateTargetBall();

            // Check if we've entered endgame
            CheckEndGame();

            // Player continues (same player, doesn't switch)
        }

        /// <summary>
        /// Handles when the player misses (no pot, no foul).
        /// </summary>
        private void HandleMiss()
        {
            Debug.WriteLine($"{CurrentPlayer.Name} missed");

            // End the break
            CurrentPlayer.EndBreak();

            // Switch players
            SwitchPlayer();

            // Target stays the same (unless we need to reset to red)
            if (!isEndGame && targetBallType == TargetBallType.Colour)
            {
                // If player was on a colour and missed, next player is on red
                targetBallType = TargetBallType.Red;
            }
        }

        /// <summary>
        /// Updates the target ball type based on what was just potted.
        /// </summary>
        private void UpdateTargetBall()
        {
            if (isEndGame)
            {
                // In endgame, move to next colour in sequence
                AdvanceEndgameTarget();
            }
            else if (targetBallType == TargetBallType.Red)
            {
                // After potting red, target is any colour
                targetBallType = TargetBallType.Colour;
            }
            else
            {
                // After potting colour, target is red (if any remain)
                if (redsRemaining > 0)
                {
                    targetBallType = TargetBallType.Red;
                }
                else
                {
                    CheckEndGame();
                }
            }
        }

        /// <summary>
        /// Checks if the game has entered the endgame phase.
        /// Endgame begins when all reds are potted and the final colour is potted.
        /// </summary>
        private void CheckEndGame()
        {
            if (!isEndGame && redsRemaining == 0)
            {
                // Check if player just potted the colour after the last red
                if (targetBallType == TargetBallType.Red)
                {
                    // Last red was just potted, still need to pot a colour
                    targetBallType = TargetBallType.Colour;
                }
                else if (targetBallType == TargetBallType.Colour)
                {
                    // Colour after last red was potted, enter endgame
                    isEndGame = true;
                    targetBallType = TargetBallType.Yellow;
                    Debug.WriteLine("Entering endgame - colours must be potted in order");
                }
            }
        }

        /// <summary>
        /// Advances to the next colour in the endgame sequence.
        /// </summary>
        private void AdvanceEndgameTarget()
        {
            switch (targetBallType)
            {
                case TargetBallType.Yellow:
                    targetBallType = TargetBallType.Green;
                    break;
                case TargetBallType.Green:
                    targetBallType = TargetBallType.Brown;
                    break;
                case TargetBallType.Brown:
                    targetBallType = TargetBallType.Blue;
                    break;
                case TargetBallType.Blue:
                    targetBallType = TargetBallType.Pink;
                    break;
                case TargetBallType.Pink:
                    targetBallType = TargetBallType.Black;
                    break;
                case TargetBallType.Black:
                    // Black was potted - frame should end
                    break;
            }
        }

        #endregion

        #region Ball Management

        /// <summary>
        /// Respots any colours that were potted (during normal play, not endgame).
        /// </summary>
        private void RespotBalls()
        {
            foreach (Ball ball in ballsPottedThisShot)
            {
                if (ball is ColouredBall colouredBall && !colouredBall.IsRed)
                {
                    if (!isEndGame)
                    {
                        RespotColour(colouredBall);
                    }
                }
            }
        }

        /// <summary>
        /// Respots a coloured ball on its designated spot.
        /// If the spot is occupied, finds the highest available spot.
        /// </summary>
        private void RespotColour(ColouredBall ball)
        {
            // Try the ball's own spot first
            Vector2D spotPosition = GetSpotForColour(ball.Type);

            if (IsSpotClear(spotPosition, ball))
            {
                ball.RespotAt(spotPosition);
                Debug.WriteLine($"{ball.Type} respotted on its spot");
                return;
            }

            // Spot is occupied, try higher value spots
            Vector2D[] spots = new Vector2D[]
            {
                table.BlackSpot,
                table.PinkSpot,
                table.BlueSpot,
                table.BrownSpot,
                table.GreenSpot,
                table.YellowSpot
            };

            foreach (Vector2D spot in spots)
            {
                if (IsSpotClear(spot, ball))
                {
                    ball.RespotAt(spot);
                    Debug.WriteLine($"{ball.Type} respotted on alternative spot");
                    return;
                }
            }

            // All spots occupied - place as close to own spot as possible
            ball.RespotAt(spotPosition);
            Debug.WriteLine($"{ball.Type} respotted near its spot (all spots occupied)");
        }

        /// <summary>
        /// Gets the designated spot position for a colour.
        /// </summary>
        private Vector2D GetSpotForColour(ColouredBall.BallType type)
        {
            switch (type)
            {
                case ColouredBall.BallType.Yellow: return table.YellowSpot;
                case ColouredBall.BallType.Green: return table.GreenSpot;
                case ColouredBall.BallType.Brown: return table.BrownSpot;
                case ColouredBall.BallType.Blue: return table.BlueSpot;
                case ColouredBall.BallType.Pink: return table.PinkSpot;
                case ColouredBall.BallType.Black: return table.BlackSpot;
                default: return table.BlueSpot;
            }
        }

        /// <summary>
        /// Checks if a spot is clear for respotting a ball.
        /// </summary>
        private bool IsSpotClear(Vector2D spot, Ball ballToRespot)
        {
            double minDistance = ballToRespot.Radius * 2;

            // Check against cue ball
            if (cueBall.IsOnTable)
            {
                if (cueBall.Position.DistanceTo(spot) < minDistance)
                {
                    return false;
                }
            }

            // Check against all coloured balls
            foreach (ColouredBall ball in colouredBalls)
            {
                if (ball == ballToRespot) continue;
                if (!ball.IsOnTable) continue;

                if (ball.Position.DistanceTo(spot) < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Turn and Player Management

        /// <summary>
        /// Switches to the other player.
        /// </summary>
        public void SwitchPlayer()
        {
            CurrentPlayer.EndBreak();
            currentPlayerIndex = 1 - currentPlayerIndex;

            Debug.WriteLine($"Turn switches to {CurrentPlayer.Name}");
        }

        #endregion

        #region Frame and Match Management

        /// <summary>
        /// Checks if the current frame has ended.
        /// Frame ends when:
        /// - All balls have been potted
        /// - Only black remains and one player is more than 7 points ahead
        /// - Black is potted in endgame
        /// </summary>
        public bool CheckFrameEnd()
        {
            // Check if black was potted in endgame
            if (isEndGame && targetBallType == TargetBallType.Black)
            {
                // Check if black was potted this shot
                foreach (Ball ball in ballsPottedThisShot)
                {
                    if (ball is ColouredBall cb && cb.Type == ColouredBall.BallType.Black)
                    {
                        return true;
                    }
                }
            }

            // Check if all balls are potted
            int ballsOnTable = 0;
            foreach (ColouredBall ball in colouredBalls)
            {
                if (ball.IsOnTable && ball.IsActive)
                {
                    ballsOnTable++;
                }
            }

            if (ballsOnTable == 0)
            {
                return true;
            }

            // Check if one player has an unassailable lead
            int pointsAvailable = CalculatePointsOnTable();
            int scoreDifference = Math.Abs(players[0].Score - players[1].Score);

            if (scoreDifference > pointsAvailable)
            {
                Debug.WriteLine("Frame ended - unassailable lead");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the maximum points still available on the table.
        /// </summary>
        private int CalculatePointsOnTable()
        {
            int points = 0;

            // Each red is worth 1 + 7 (if followed by black) = 8 potential
            points += redsRemaining * 8;

            // Add colour values (27 = 2+3+4+5+6+7)
            if (!isEndGame)
            {
                points += 27;
            }
            else
            {
                // In endgame, only count remaining colours
                foreach (ColouredBall ball in colouredBalls)
                {
                    if (!ball.IsRed && ball.IsOnTable && ball.IsActive)
                    {
                        points += ball.PointValue;
                    }
                }
            }

            return points;
        }

        /// <summary>
        /// Ends the current frame and determines the winner.
        /// </summary>
        private void EndFrame()
        {
            gameState = GameState.FrameOver;

            // Determine frame winner
            Player frameWinner;
            if (players[0].Score > players[1].Score)
            {
                frameWinner = players[0];
            }
            else if (players[1].Score > players[0].Score)
            {
                frameWinner = players[1];
            }
            else
            {
                // Tie - re-spot black and play for it
                // For simplicity, we'll give it to current player
                frameWinner = CurrentPlayer;
            }

            frameWinner.WinFrame();
            Debug.WriteLine($"Frame won by {frameWinner.Name}! Score: {players[0].Score}-{players[1].Score}");

            // Check if match is over
            if (frameWinner.FramesWon >= FRAMES_TO_WIN)
            {
                gameState = GameState.MatchOver;
                Debug.WriteLine($"Match won by {frameWinner.Name}!");
            }
        }

        /// <summary>
        /// Gets a string describing the current game status.
        /// </summary>
        public string GetStatusMessage()
        {
            switch (gameState)
            {
                case GameState.PlacingCueBall:
                    return $"{CurrentPlayer.Name}: Place the cue ball in the D";

                case GameState.Aiming:
                    string target = targetBallType == TargetBallType.Colour ? "any colour" : targetBallType.ToString();
                    return $"{CurrentPlayer.Name} to play - Target: {target}";

                case GameState.BallsMoving:
                    return "Shot in progress...";

                case GameState.ProcessingShot:
                    return "Processing...";

                case GameState.FrameOver:
                    return $"Frame over! {GetFrameWinner().Name} wins the frame";

                case GameState.MatchOver:
                    return $"Match over! {GetMatchWinner().Name} wins the match!";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Gets the winner of the current frame (by score).
        /// </summary>
        public Player GetFrameWinner()
        {
            return players[0].Score >= players[1].Score ? players[0] : players[1];
        }

        /// <summary>
        /// Gets the winner of the match (by frames).
        /// </summary>
        public Player GetMatchWinner()
        {
            return players[0].FramesWon >= players[1].FramesWon ? players[0] : players[1];
        }

        /// <summary>
        /// Gets the target ball description for UI display.
        /// </summary>
        public string GetTargetBallDescription()
        {
            if (targetBallType == TargetBallType.Colour)
            {
                return "Any Colour";
            }
            return targetBallType.ToString();
        }

        #endregion
    }
}