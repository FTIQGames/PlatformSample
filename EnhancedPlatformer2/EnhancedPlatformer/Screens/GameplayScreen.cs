#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Media;
#endregion

namespace EnhancedPlatformer
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        [Serializable]
        public struct SaveGameData
        {
            //public string PlayerName;
            //public Vector2 AvatarPosition;
            //public int Level;
            public int Score;
        }

        ContentManager content;
        SpriteFont gameFont;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D gameoverOverlay;

        private const Buttons ContinueButton = Buttons.A;
        private const int StartingLives = 3;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;
        int totalScore;
        int highScore;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            totalScore = 0;

        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            gameFont = content.Load<SpriteFont>("gamefont");

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch; //new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay = content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = content.Load<Texture2D>("Overlays/you_died");
            gameoverOverlay = content.Load<Texture2D>("Overlays/gameover");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(content.Load<Song>("Sounds/Music"));

            //write highscore to storage
            string scorePath = Path.Combine(StorageContainer.TitleLocation, "Content/score.txt");
            //string Score = File.ReadAllText(scorePath, Encoding.ASCII);

            if (File.Exists(scorePath))
            {

                try
                {
                    // Open the file, creating it if necessary.
                    FileStream stream = File.Open(scorePath, FileMode.Open);
                    // Convert the object to XML data and put it in the stream.
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
                    SaveGameData data = (SaveGameData) serializer.Deserialize(stream);

                    // Close the file.
                    stream.Close();

                    highScore = data.Score;
                }
                catch (Exception e)
                {
                    highScore = 0;
                }
                
                
            }
            else
                highScore = 0;

            LoadNextLevel(StartingLives);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }
        private void LoadNextLevel(int currentLives)
        {
            // Find the path of the next level.
            string levelPath;

            // Loop here so we can try again when we can't find a level.
            while (true)
            {
                // Try to find the next level. They are sequentially numbered txt files.
                levelPath = String.Format("Levels/{0}.txt", ++levelIndex);
                levelPath = Path.Combine(StorageContainer.TitleLocation, "Content/" + levelPath);
                if (File.Exists(levelPath))
                    break;

                // If there isn't even a level 0, something has gone wrong.
                if (levelIndex == 0)
                    throw new Exception("No levels found.");

                // Whenever we can't find a level, start over again at 0.
                // TODO: Put a finish screen with scores here
                levelIndex = -1;
                break;
            }

            if (levelIndex == -1)
            {
                //We've finished, test score for high score and show mainmenu again

                ScreenManager.AddScreen(new FinishedScreen(), ControllingPlayer);

                TestHighScore();

            }
            else
            {
                // Unloads the content for the current level before loading the next one.
                if (level != null)
                {
                    totalScore = level.Score;
                    level.Dispose();
                }

                // Load the level.
                level = new Level(ScreenManager.Game.Services, levelPath, totalScore, levelIndex, currentLives);
            }
        }

        private void TestHighScore()
        {
            if (highScore < level.Score)
            {
                highScore = level.Score;


                //write highscore to storage
                SaveGameData data = new SaveGameData();
                data.Score = highScore;
                try
                {
                    string scorePath = Path.Combine(StorageContainer.TitleLocation, "Content/score.txt");
                    //File.WriteAllText(scorePath,highScore.ToString());

                    // Open the file, creating it if necessary.
                    FileStream stream = File.Open(scorePath, FileMode.OpenOrCreate);

                    // Convert the object to XML data and put it in the stream.
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
                    serializer.Serialize(stream, data);

                    // Close the file.
                    stream.Close();
                }
                catch (Exception e)
                {
                    //noop
                }
            }

        }
        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }

        private void ReloadCurrentLevel(int currentLives)
        {
            --levelIndex;
            LoadNextLevel(currentLives);
        }



        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {       

            if (IsActive)
            {
                //HandleInput();

                level.Update(gameTime);

                base.Update(gameTime, otherScreenHasFocus,coveredByOtherScreen);
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {

                bool continuePressed =
                    keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadState.IsButtonDown(ContinueButton);

                // Perform the appropriate action to advance the game and
                // to get the player back to playing.
                if (!wasContinuePressed && continuePressed)
                {
                    if (level.Player.Lives < 1)
                    {
                        TestHighScore();
                        this.ExitScreen();
                        ScreenManager.AddScreen(new BackgroundScreen(), null);
                        ScreenManager.AddScreen(new MainMenuScreen(), null);
                    }
                    else if (!level.Player.IsAlive)
                    {
                        level.StartNewLife();
                    }
                    else if (level.TimeRemaining == TimeSpan.Zero)
                    {
                        if (level.ReachedExit)
                            LoadNextLevel(level.Player.Lives);
                        else
                            ReloadCurrentLevel(level.Player.Lives);
                    }
                }

                wasContinuePressed = continuePressed;
            }
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            // Our player and enemy are both actually just text strings.
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            //graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            //spriteBatch.Begin();

            level.Draw(gameTime, spriteBatch);

            DrawHud();

            //spriteBatch.End();

            base.Draw(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0)
                ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }

        private void DrawHud()
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.Begin();
            Rectangle titleSafeArea = ScreenManager.GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
            DrawShadowedString(hudFont, "LIVES: " + level.Player.Lives.ToString(), hudLocation + new Vector2(0.0f, (timeHeight * 1.2f)*2), Color.Yellow);
            DrawShadowedString(hudFont, "HIGHSCORE: " + highScore.ToString(), hudLocation + new Vector2(ScreenManager.Game.GraphicsDevice.Viewport.TitleSafeArea.Width - (10 + highScore.ToString().Length)*20, 0.0f), Color.Yellow);
            // Determine the status overlay message to show.
            Texture2D status = null;

            if (level.TimeRemaining == TimeSpan.Zero)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (level.Player.Lives<1)
            {
                status = gameoverOverlay;
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }

            spriteBatch.End();
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
        #endregion
    }
}
