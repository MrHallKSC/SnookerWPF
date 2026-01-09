using SnookerGame.Engine;
using SnookerGame.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SnookerGame
{
    /// <summary>
    /// Main window code-behind for the Snooker Game.
    /// 
    /// This class handles:
    /// - Game initialisation and setup
    /// - Rendering the table and balls on the canvas
    /// - Mouse input for aiming and shooting
    /// - Game loop timing for physics updates
    /// - Integration with PhysicsEngine for ball movement
    /// 
    /// Phase 3: Balls now move with physics simulation including
    /// friction and cushion collisions.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        /// <summary>
        /// Target frames per second for the game loop.
        /// 60 FPS provides smooth animation.
        /// </summary>
        private const int TARGET_FPS = 60;

        /// <summary>
        /// Table dimensions in game units.
        /// These will be scaled to fit the canvas.
        /// Maintaining 2:1 ratio like a real snooker table.
        /// </summary>
        private const double TABLE_WIDTH = 800.0;
        private const double TABLE_HEIGHT = 400.0;

        #endregion

        #region Private Fields

        // Core game objects
        private Table table;
        private CueBall cueBall;
        private List<ColouredBall> colouredBalls;

        // Physics engine for ball movement
        private PhysicsEngine physicsEngine;

        // Game manager for rules and scoring
        private GameManager gameManager;

        // Players
        private Player player1;
        private Player player2;

        // Game loop timer
        private DispatcherTimer gameTimer;
        private DateTime lastUpdateTime;

        // Input state tracking
        private bool isAiming;
        private bool isChargingShot;
        private Vector2D mousePosition;

        // Power meter oscillation
        private bool powerIncreasing;

        // Game state tracking
        private bool ballsMoving;
        private List<Ball> pottedThisShot;
        private bool firstCollisionRecorded;

        // Message overlay
        private string overlayMessage;
        private DateTime overlayMessageTime;
        private double overlayDuration;
        private Color overlayBackgroundColor;

        // Offset for centring table on canvas
        private double canvasOffsetX;
        private double canvasOffsetY;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises the main window and sets up the game.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initialise mouse position tracker
            mousePosition = new Vector2D(0, 0);

            // Set up the game when the window has loaded and canvas has size
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
        }

        #endregion

        #region Initialisation

        /// <summary>
        /// Called when the window has fully loaded.
        /// Initialises all game components.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Window loaded - initialising game...");

            // Create the table
            InitialiseTable();

            // Create all balls in starting positions
            InitialiseBalls();

            // Create players
            InitialisePlayers();

            // Create the physics engine
            InitialisePhysics();

            // Create the game manager
            InitialiseGameManager();

            // Set up the game loop timer
            InitialiseGameLoop();

            // Calculate canvas offset for centring
            CalculateCanvasOffset();

            // Initialise game state
            ballsMoving = false;
            pottedThisShot = new List<Ball>();
            firstCollisionRecorded = false;
            powerIncreasing = true;

            // Initialise overlay message
            overlayMessage = "";
            overlayMessageTime = DateTime.MinValue;
            overlayDuration = 0;
            overlayBackgroundColor = Colors.Black;

            // Update UI to show initial state
            UpdateUI();

            // Perform initial render
            Render();

            Debug.WriteLine("Game initialisation complete.");
            Debug.WriteLine($"Table size: {table.Width} x {table.Height}");
            Debug.WriteLine($"Cue ball position: {cueBall.Position}");
            Debug.WriteLine($"Coloured balls on table: {colouredBalls.Count}");
        }

        /// <summary>
        /// Creates the snooker table with correct dimensions.
        /// </summary>
        private void InitialiseTable()
        {
            table = new Table(TABLE_WIDTH, TABLE_HEIGHT);
            Debug.WriteLine($"Table created with {table.Pockets.Count} pockets");
        }

        /// <summary>
        /// Creates all balls and places them in their starting positions.
        /// 
        /// Ball setup:
        /// - 1 white cue ball (in the D)
        /// - 15 red balls (in triangle formation)
        /// - 6 coloured balls (on their spots)
        /// </summary>
        private void InitialiseBalls()
        {
            colouredBalls = new List<ColouredBall>();

            // Create cue ball in the D
            Vector2D cueBallStart = table.GetDefaultCueBallPosition();
            cueBall = new CueBall(cueBallStart);
            cueBall.SetDefaultPosition(cueBallStart);
            Debug.WriteLine($"Cue ball created at: {cueBallStart}");

            // Create the 6 coloured balls on their spots
            CreateColouredBalls();

            // Create the 15 red balls in triangle formation
            CreateRedBalls();

            Debug.WriteLine($"Total coloured balls created: {colouredBalls.Count}");
        }

        /// <summary>
        /// Creates the 6 coloured balls (yellow, green, brown, blue, pink, black)
        /// and places them on their designated spots.
        /// </summary>
        private void CreateColouredBalls()
        {
            // Yellow - on the right of the D (from player's view)
            ColouredBall yellow = new ColouredBall(
                ColouredBall.BallType.Yellow,
                table.YellowSpot
            );
            colouredBalls.Add(yellow);

            // Green - on the left of the D
            ColouredBall green = new ColouredBall(
                ColouredBall.BallType.Green,
                table.GreenSpot
            );
            colouredBalls.Add(green);

            // Brown - centre of baulk line
            ColouredBall brown = new ColouredBall(
                ColouredBall.BallType.Brown,
                table.BrownSpot
            );
            colouredBalls.Add(brown);

            // Blue - centre of table
            ColouredBall blue = new ColouredBall(
                ColouredBall.BallType.Blue,
                table.BlueSpot
            );
            colouredBalls.Add(blue);

            // Pink - between centre and black
            ColouredBall pink = new ColouredBall(
                ColouredBall.BallType.Pink,
                table.PinkSpot
            );
            colouredBalls.Add(pink);

            // Black - near top of table
            ColouredBall black = new ColouredBall(
                ColouredBall.BallType.Black,
                table.BlackSpot
            );
            colouredBalls.Add(black);

            Debug.WriteLine("6 coloured balls created on spots");
        }

        /// <summary>
        /// Creates the 15 red balls in their triangle formation.
        /// The triangle is positioned with apex behind the pink spot.
        /// </summary>
        private void CreateRedBalls()
        {
            // Get ball radius from cue ball (all balls same size)
            double ballRadius = cueBall.Radius;

            // Get the 15 positions for red balls from table
            List<Vector2D> redPositions = table.GetRedBallPositions(ballRadius);

            // Create a red ball at each position
            foreach (Vector2D position in redPositions)
            {
                ColouredBall red = ColouredBall.CreateRed(position);
                colouredBalls.Add(red);
            }

            Debug.WriteLine($"15 red balls created in triangle formation");
        }

        /// <summary>
        /// Creates and configures the physics engine.
        /// </summary>
        private void InitialisePhysics()
        {
            physicsEngine = new PhysicsEngine();
            Debug.WriteLine("Physics engine initialised");
        }

        /// <summary>
        /// Creates the two players.
        /// </summary>
        private void InitialisePlayers()
        {
            player1 = new Player("Player 1");
            player2 = new Player("Player 2");
            Debug.WriteLine("Players initialised");
        }

        /// <summary>
        /// Creates and configures the game manager.
        /// </summary>
        private void InitialiseGameManager()
        {
            gameManager = new GameManager(player1, player2, table, cueBall, colouredBalls);
            Debug.WriteLine("Game manager initialised");
        }

        /// <summary>
        /// Sets up the game loop timer.
        /// The timer fires at TARGET_FPS to update physics and render.
        /// </summary>
        private void InitialiseGameLoop()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / TARGET_FPS);
            gameTimer.Tick += GameLoop_Tick;

            lastUpdateTime = DateTime.Now;

            // Start the timer
            gameTimer.Start();

            Debug.WriteLine($"Game loop initialised at {TARGET_FPS} FPS");
        }

        /// <summary>
        /// Calculates the offset needed to centre the table on the canvas.
        /// Called on load and when window size changes.
        /// </summary>
        private void CalculateCanvasOffset()
        {
            double canvasWidth = gameCanvas.ActualWidth;
            double canvasHeight = gameCanvas.ActualHeight;

            // Centre the table
            canvasOffsetX = (canvasWidth - TABLE_WIDTH) / 2;
            canvasOffsetY = (canvasHeight - TABLE_HEIGHT) / 2;

            // Ensure non-negative offsets
            if (canvasOffsetX < 0) canvasOffsetX = 0;
            if (canvasOffsetY < 0) canvasOffsetY = 0;

            Debug.WriteLine($"Canvas offset: ({canvasOffsetX}, {canvasOffsetY})");
        }

        /// <summary>
        /// Handles window resize - recalculates canvas offset.
        /// </summary>
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateCanvasOffset();
            Render();
        }

        #endregion

        #region Game Loop

        /// <summary>
        /// Main game loop - called every frame by the timer.
        /// 
        /// Handles:
        /// - Physics updates (ball movement, collisions)
        /// - Aiming updates
        /// - Shot power charging
        /// - Pocket detection
        /// - Rendering
        /// </summary>
        private void GameLoop_Tick(object sender, EventArgs e)
        {
            // Calculate delta time (time since last frame)
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - lastUpdateTime).TotalSeconds;
            lastUpdateTime = currentTime;

            // Cap delta time to prevent huge jumps if game freezes
            if (deltaTime > 0.1) deltaTime = 0.1;

            // Update physics if balls are moving
            if (ballsMoving)
            {
                UpdatePhysics(deltaTime);
            }

            // Update aim direction if player is aiming (and balls not moving)
            if (isAiming && !ballsMoving && !cueBall.IsInHand)
            {
                UpdateAiming();
            }

            // Update shot power if charging
            if (isChargingShot)
            {
                UpdateShotPower(deltaTime);
            }

            // Render the current state
            Render();
        }

        /// <summary>
        /// Updates the physics simulation for all balls.
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdatePhysics(double deltaTime)
        {
            // Create combined list for physics
            List<Ball> allBalls = new List<Ball>();
            allBalls.Add(cueBall);
            allBalls.AddRange(colouredBalls);

            // Check for ball-ball collisions and record first hit
            if (!firstCollisionRecorded)
            {
                CheckFirstBallHit(allBalls);
            }

            // Update ball positions and handle cushion collisions
            bool stillMoving = physicsEngine.Update(cueBall, colouredBalls, table, deltaTime);

            // Check for potted balls
            List<Ball> newlyPotted = physicsEngine.CheckPottedBalls(allBalls, table);

            foreach (Ball ball in newlyPotted)
            {
                pottedThisShot.Add(ball);
                gameManager.RecordBallPotted(ball);
                HandlePottedBall(ball);
            }

            // Check if all balls have stopped
            if (!stillMoving)
            {
                ballsMoving = false;
                OnShotComplete();
            }
        }

        /// <summary>
        /// Checks for the first ball hit by the cue ball.
        /// </summary>
        private void CheckFirstBallHit(List<Ball> allBalls)
        {
            foreach (ColouredBall ball in colouredBalls)
            {
                if (!ball.IsOnTable) continue;

                if (physicsEngine.DetectCollision(cueBall, ball))
                {
                    gameManager.RecordFirstBallHit(ball);
                    firstCollisionRecorded = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a ball being potted.
        /// </summary>
        /// <param name="ball">The ball that was potted</param>
        private void HandlePottedBall(Ball ball)
        {
            if (ball is CueBall)
            {
                Debug.WriteLine("Cue ball potted - FOUL!");
                lblStatus.Text = "Foul! Cue ball potted.";
            }
            else if (ball is ColouredBall colouredBall)
            {
                Debug.WriteLine($"{colouredBall.Type} ball potted - {colouredBall.PointValue} points!");
                lblStatus.Text = $"{colouredBall.Type} potted! ({colouredBall.PointValue} points)";

                // Show potted message with green background
                ShowOverlayMessage($"{colouredBall.Type} Potted!\n+{colouredBall.PointValue} Points",
                    1.5, Color.FromArgb(200, 0, 100, 0));
            }
        }

        /// <summary>
        /// Called when all balls have stopped moving after a shot.
        /// </summary>
        private void OnShotComplete()
        {
            Debug.WriteLine("Shot complete - all balls stopped");

            // Store current player before processing (to detect turn change)
            Player playerBeforeProcessing = gameManager.CurrentPlayer;

            // Let game manager process the shot result
            gameManager.ProcessShot();

            // Update UI based on game state
            UpdateUI();

            // Show foul message if applicable
            if (gameManager.FoulCommitted)
            {
                ShowOverlayMessage($"FOUL!\n{gameManager.FoulReason}",
                    2.5, Color.FromArgb(200, 180, 0, 0));
            }
            // Check if turn changed (player switched)
            else if (gameManager.CurrentPlayer != playerBeforeProcessing)
            {
                ShowOverlayMessage($"{gameManager.CurrentPlayer.Name}'s Turn",
                    2.0, Color.FromArgb(200, 0, 0, 100));
            }

            // Handle game state
            switch (gameManager.State)
            {
                case GameState.PlacingCueBall:
                    if (!cueBall.IsOnTable)
                    {
                        cueBall.Reset();
                    }
                    lblStatus.Text = $"{gameManager.CurrentPlayer.Name}: Place cue ball in the D";
                    break;

                case GameState.Aiming:
                    lblStatus.Text = gameManager.GetStatusMessage();
                    break;

                case GameState.FrameOver:
                    lblStatus.Text = $"Frame over! {gameManager.GetFrameWinner().Name} wins!";
                    ShowOverlayMessage($"Frame Over!\n{gameManager.GetFrameWinner().Name} Wins!",
                        3.0, Color.FromArgb(220, 0, 80, 0));
                    break;

                case GameState.MatchOver:
                    lblStatus.Text = $"Match over! {gameManager.GetMatchWinner().Name} wins the match!";
                    ShowOverlayMessage($"Match Over!\n{gameManager.GetMatchWinner().Name} Wins the Match!",
                        5.0, Color.FromArgb(220, 150, 120, 0));
                    break;
            }

            // Clear potted balls list for next shot
            pottedThisShot.Clear();
            firstCollisionRecorded = false;

            // Re-enable aiming if game continues
            if (gameManager.State == GameState.Aiming || gameManager.State == GameState.PlacingCueBall)
            {
                isAiming = true;
            }
        }

        /// <summary>
        /// Counts the number of red balls still on the table.
        /// </summary>
        private int CountRedsOnTable()
        {
            int count = 0;
            foreach (ColouredBall ball in colouredBalls)
            {
                if (ball.IsRed && ball.IsOnTable && ball.IsActive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Updates the UI to reflect current game state.
        /// </summary>
        private void UpdateUI()
        {
            // Update player names and scores
            lblPlayer1Name.Text = player1.Name;
            lblPlayer2Name.Text = player2.Name;
            lblPlayer1Score.Text = player1.Score.ToString();
            lblPlayer2Score.Text = player2.Score.ToString();
            lblPlayer1Break.Text = player1.CurrentBreak.ToString();
            lblPlayer2Break.Text = player2.CurrentBreak.ToString();

            // Update frame info
            lblFrameInfo.Text = $"Frame {player1.FramesWon + player2.FramesWon + 1}";
            lblCurrentPlayer.Text = $"{gameManager.CurrentPlayer.Name} to play";
            lblTargetBall.Text = $"Target: {gameManager.GetTargetBallDescription()}";

            // Update reds remaining
            lblRedsRemaining.Text = gameManager.RedsRemaining.ToString();
            lblPointsOnTable.Text = $"Points on table: {gameManager.PointsOnTable}";

            // Highlight current player
            if (gameManager.CurrentPlayer == player1)
            {
                lblPlayer1Name.Foreground = new SolidColorBrush(Colors.Yellow);
                lblPlayer2Name.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                lblPlayer1Name.Foreground = new SolidColorBrush(Colors.White);
                lblPlayer2Name.Foreground = new SolidColorBrush(Colors.Yellow);
            }
        }

        /// <summary>
        /// Updates the cue ball's aim direction based on mouse position.
        /// </summary>
        private void UpdateAiming()
        {
            // Convert mouse position to table coordinates
            double tableX = mousePosition.X - canvasOffsetX;
            double tableY = mousePosition.Y - canvasOffsetY;

            // Set aim direction toward mouse
            cueBall.SetAimDirection(tableX, tableY);
        }

        /// <summary>
        /// Increases or decreases shot power while mouse button is held.
        /// Power oscillates between 0 and 100% creating a timing challenge.
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateShotPower(double deltaTime)
        {
            // Charge rate - full power in about 1.5 seconds
            double chargeRate = 0.7;

            if (powerIncreasing)
            {
                cueBall.ChargePower(chargeRate * deltaTime);

                // If reached max, start decreasing
                if (cueBall.ShotPower >= 1.0)
                {
                    powerIncreasing = false;
                }
            }
            else
            {
                cueBall.ChargePower(-chargeRate * deltaTime);

                // If reached min, start increasing
                if (cueBall.ShotPower <= 0.0)
                {
                    powerIncreasing = true;
                }
            }

            // Update power bar UI
            UpdatePowerBarDisplay();
        }

        /// <summary>
        /// Updates the power bar visual display.
        /// </summary>
        private void UpdatePowerBarDisplay()
        {
            double powerPercent = cueBall.ShotPower;

            // Update bar width (max width is 180)
            rectPowerBar.Width = 180 * powerPercent;

            // Update colour (green -> yellow -> red)
            byte red = (byte)(255 * powerPercent);
            byte green = (byte)(255 * (1 - powerPercent));
            rectPowerBar.Fill = new SolidColorBrush(Color.FromRgb(red, green, 0));

            // Update percentage text
            lblPowerPercent.Text = $"{cueBall.ShotPowerPercent}%";
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Main render method - clears canvas and draws all game elements.
        /// </summary>
        private void Render()
        {
            // Don't render if game objects haven't been initialised yet
            // This prevents NullReferenceException when SizeChanged fires before Loaded
            if (table == null || cueBall == null || colouredBalls == null)
            {
                return;
            }

            // Clear the canvas
            gameCanvas.Children.Clear();

            // Draw the table (surface, cushions, pockets, markings)
            DrawTable();

            // Draw all balls
            DrawBalls();

            // Draw aiming line and cue if player is aiming and balls aren't moving
            if (!ballsMoving && !cueBall.IsInHand)
            {
                // Draw aim line first (behind cue)
                if (isAiming)
                {
                    DrawAimLine();
                }

                // Draw the cue stick
                DrawCue();
            }

            // Draw overlay message on top of everything
            DrawOverlayMessage();
        }

        /// <summary>
        /// Draws the table with offset applied.
        /// </summary>
        private void DrawTable()
        {
            // We need to temporarily adjust for offset
            // For now, we'll create a simple approach by adjusting 
            // the table draw position

            // Create a translated canvas for the table
            System.Windows.Controls.Canvas tableCanvas = new System.Windows.Controls.Canvas();
            tableCanvas.Width = TABLE_WIDTH;
            tableCanvas.Height = TABLE_HEIGHT;

            // Draw table onto our temp canvas
            table.Draw(tableCanvas);

            // Position the table canvas with offset
            System.Windows.Controls.Canvas.SetLeft(tableCanvas, canvasOffsetX);
            System.Windows.Controls.Canvas.SetTop(tableCanvas, canvasOffsetY);

            gameCanvas.Children.Add(tableCanvas);
        }

        /// <summary>
        /// Draws all balls on the canvas.
        /// </summary>
        private void DrawBalls()
        {
            // Create a canvas for balls (same offset as table)
            System.Windows.Controls.Canvas ballCanvas = new System.Windows.Controls.Canvas();

            // Draw cue ball
            cueBall.Draw(ballCanvas);

            // Draw all coloured balls
            foreach (ColouredBall ball in colouredBalls)
            {
                ball.Draw(ballCanvas);
            }

            // Position with offset
            System.Windows.Controls.Canvas.SetLeft(ballCanvas, canvasOffsetX);
            System.Windows.Controls.Canvas.SetTop(ballCanvas, canvasOffsetY);

            gameCanvas.Children.Add(ballCanvas);
        }

        /// <summary>
        /// Draws the aiming line from cue ball toward mouse position.
        /// </summary>
        private void DrawAimLine()
        {
            System.Windows.Controls.Canvas aimCanvas = new System.Windows.Controls.Canvas();

            // Draw the aim line (length 200 pixels)
            cueBall.DrawAimLine(aimCanvas, 150);

            // Position with offset
            System.Windows.Controls.Canvas.SetLeft(aimCanvas, canvasOffsetX);
            System.Windows.Controls.Canvas.SetTop(aimCanvas, canvasOffsetY);

            gameCanvas.Children.Add(aimCanvas);
        }

        /// <summary>
        /// Draws the cue stick behind the cue ball.
        /// </summary>
        private void DrawCue()
        {
            System.Windows.Controls.Canvas cueCanvas = new System.Windows.Controls.Canvas();

            // Draw the cue
            cueBall.DrawCue(cueCanvas);

            // Position with offset
            System.Windows.Controls.Canvas.SetLeft(cueCanvas, canvasOffsetX);
            System.Windows.Controls.Canvas.SetTop(cueCanvas, canvasOffsetY);

            gameCanvas.Children.Add(cueCanvas);
        }

        #endregion

        #region Mouse Input Handlers

        /// <summary>
        /// Handles mouse movement over the game canvas.
        /// Updates the aim direction when player is aiming.
        /// </summary>
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Get mouse position relative to canvas
            Point pos = e.GetPosition(gameCanvas);
            mousePosition.Set(pos.X, pos.Y);

            // Always track for aiming (when ball isn't moving)
            isAiming = true;
        }

        /// <summary>
        /// Handles mouse button press - starts charging the shot.
        /// </summary>
        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't allow input while balls are moving
            if (ballsMoving) return;

            // Check game state
            if (gameManager.State == GameState.FrameOver || gameManager.State == GameState.MatchOver)
            {
                // Could add restart logic here
                return;
            }

            // If cue ball is in hand, place it
            if (cueBall.IsInHand)
            {
                PlaceCueBall(e.GetPosition(gameCanvas));
            }
            // Otherwise, start charging a shot
            else if (!cueBall.IsMoving)
            {
                isChargingShot = true;
                powerIncreasing = true;  // Reset to increasing
                cueBall.ResetPower();

                lblStatus.Text = "Charging shot... Release to play";
                Debug.WriteLine("Started charging shot");
            }
        }

        /// <summary>
        /// Handles mouse button release - executes the shot.
        /// </summary>
        private void GameCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isChargingShot && !ballsMoving)
            {
                isChargingShot = false;

                // Only execute shot if there's some power
                if (cueBall.ShotPower > 0.01)
                {
                    // Notify game manager that shot is starting
                    gameManager.OnShotStarted();

                    // Execute the shot
                    cueBall.Strike();

                    // Set balls moving flag
                    ballsMoving = true;
                    isAiming = false;
                    firstCollisionRecorded = false;

                    // Reset power bar
                    rectPowerBar.Width = 0;
                    lblPowerPercent.Text = "0%";

                    lblStatus.Text = "Shot played!";
                    Debug.WriteLine($"Shot executed with power: {cueBall.ShotPowerPercent}%, angle: {cueBall.AimAngle:F2} rad");
                }
                else
                {
                    lblStatus.Text = "Shot too weak - try again";
                    cueBall.ResetPower();
                    rectPowerBar.Width = 0;
                    lblPowerPercent.Text = "0%";
                }
            }
        }

        /// <summary>
        /// Attempts to place the cue ball at the clicked position.
        /// Only valid if position is within the D.
        /// </summary>
        /// <param name="clickPosition">Where the player clicked</param>
        private void PlaceCueBall(Point clickPosition)
        {
            // Convert to table coordinates
            double tableX = clickPosition.X - canvasOffsetX;
            double tableY = clickPosition.Y - canvasOffsetY;
            Vector2D position = new Vector2D(tableX, tableY);

            // Check if position is in the D
            if (table.IsInD(position))
            {
                cueBall.PlaceBall(position);
                cueBall.ConfirmPlacement();
                lblStatus.Text = "Ball placed. Click and hold to charge shot.";
                Debug.WriteLine($"Cue ball placed at: {position}");
            }
            else
            {
                lblStatus.Text = "Invalid position! Ball must be placed in the D.";
                Debug.WriteLine("Attempted to place ball outside D");
            }
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Shows an overlay message in the centre of the table.
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="duration">How long to show in seconds</param>
        /// <param name="backgroundColor">Background colour for the message box</param>
        private void ShowOverlayMessage(string message, double duration, Color backgroundColor)
        {
            overlayMessage = message;
            overlayMessageTime = DateTime.Now;
            overlayDuration = duration;
            overlayBackgroundColor = backgroundColor;
        }

        /// <summary>
        /// Shows an overlay message with default black background.
        /// </summary>
        private void ShowOverlayMessage(string message, double duration)
        {
            ShowOverlayMessage(message, duration, Color.FromArgb(200, 0, 0, 0));
        }

        /// <summary>
        /// Draws the overlay message if one is active.
        /// </summary>
        private void DrawOverlayMessage()
        {
            // Check if message should still be displayed
            if (string.IsNullOrEmpty(overlayMessage)) return;

            double elapsed = (DateTime.Now - overlayMessageTime).TotalSeconds;
            if (elapsed > overlayDuration)
            {
                overlayMessage = "";
                return;
            }

            // Calculate fade out in last 0.5 seconds
            double opacity = 1.0;
            if (elapsed > overlayDuration - 0.5)
            {
                opacity = (overlayDuration - elapsed) / 0.5;
            }

            // Create background rectangle
            double boxWidth = 350;
            double boxHeight = 60;
            double boxX = canvasOffsetX + (TABLE_WIDTH - boxWidth) / 2;
            double boxY = canvasOffsetY + (TABLE_HEIGHT - boxHeight) / 2;

            System.Windows.Shapes.Rectangle background = new System.Windows.Shapes.Rectangle();
            background.Width = boxWidth;
            background.Height = boxHeight;
            background.RadiusX = 10;
            background.RadiusY = 10;
            background.Fill = new SolidColorBrush(Color.FromArgb(
                (byte)(overlayBackgroundColor.A * opacity),
                overlayBackgroundColor.R,
                overlayBackgroundColor.G,
                overlayBackgroundColor.B));
            background.Stroke = new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 255, 255, 255));
            background.StrokeThickness = 2;

            Canvas.SetLeft(background, boxX);
            Canvas.SetTop(background, boxY);
            gameCanvas.Children.Add(background);

            // Create text
            TextBlock text = new TextBlock();
            text.Text = overlayMessage;
            text.FontSize = 20;
            text.FontWeight = FontWeights.Bold;
            text.Foreground = new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 255, 255, 255));
            text.TextAlignment = TextAlignment.Center;
            text.Width = boxWidth;
            text.TextWrapping = TextWrapping.Wrap;

            Canvas.SetLeft(text, boxX);
            Canvas.SetTop(text, boxY + (boxHeight - 28) / 2);
            gameCanvas.Children.Add(text);
        }

        /// <summary>
        /// Updates the score display for both players.
        /// Called by UpdateUI().
        /// </summary>
        public void UpdateScoreDisplay()
        {
            lblPlayer1Score.Text = player1.Score.ToString();
            lblPlayer2Score.Text = player2.Score.ToString();
            lblPlayer1Break.Text = player1.CurrentBreak.ToString();
            lblPlayer2Break.Text = player2.CurrentBreak.ToString();
        }

        /// <summary>
        /// Updates the status message shown to the player.
        /// </summary>
        /// <param name="message">Message to display</param>
        public void UpdateStatus(string message)
        {
            lblStatus.Text = message;
        }

        #endregion
    }
}