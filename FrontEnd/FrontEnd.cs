using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace FrontEnd
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class FrontEnd : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private SpriteBatch targetBatch;
        private RenderTarget2D target;

        private Chip16.Emulator emulator;

        private DynamicSoundEffectInstance audio;

        private Texture2D frameBuffer;
        private Rectangle drawDestination;

        private EmulatorControls emulatorControls;

        private readonly Dictionary<Keys, int> keyMap = new Dictionary<Keys, int>()
        {
            [Keys.Up] = 0,
            [Keys.Down] = 1,
            [Keys.Left] = 2,
            [Keys.Right] = 3,
            [Keys.RightShift] = 4,
            [Keys.Enter] = 5,
            [Keys.Z] = 6,
            [Keys.X] = 7
        };

        private bool[] input;

        public FrontEnd()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //this.emulatorControls = new EmulatorControls(this);
            //this.Components.Add(this.emulatorControls);
            this.Window.Title = "Chip16 Emulator";
            this.targetBatch = new SpriteBatch(GraphicsDevice);
            this.target = new RenderTarget2D(GraphicsDevice, (int)Chip16.Graphics.WIDTH, (int)Chip16.Graphics.HEIGHT);

            this.emulator = new Chip16.Emulator();
            this.emulator.LoadRom("../../../../../Roms/Demos/Mandel.c16");
            this.audio = new DynamicSoundEffectInstance(15360, AudioChannels.Mono);
            this.audio.Play();

            this.input = new bool[8];
            
            this.frameBuffer = new Texture2D(GraphicsDevice, (int)Chip16.Graphics.WIDTH, (int)Chip16.Graphics.HEIGHT);
            this.drawDestination = new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);

            this.IsFixedTimeStep = true;
            this.TargetElapsedTime = System.TimeSpan.FromSeconds(1d / 60d);

            this.IsMouseVisible = true;
            this.Window.AllowUserResizing = true;

            this.graphics.PreferredBackBufferWidth = (int)Chip16.Graphics.WIDTH * 2;
            this.graphics.PreferredBackBufferHeight = (int)Chip16.Graphics.HEIGHT * 2;
            this.graphics.ApplyChanges();
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        int cycles = 0;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            foreach (Keys key in keyMap.Keys)
            {
                bool isDown = Keyboard.GetState().IsKeyDown(key);
                this.input[keyMap[key]] = isDown;
            }

            emulator.SetKeyState(this.input);

            // Execute exactly 1000000 instructions per second (each instruction is 1 cycle)
            while (cycles < 1000000 * gameTime.ElapsedGameTime.TotalSeconds  && !emulator.VBlank)
            {
                // Execute Current frame of CPU cycle
                if (!emulator.VBlank)
                {
                    emulator.ExecuteCPU();
                }
                cycles++;
            }

            cycles = 0;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if (emulator.VBlank)
            {
                emulator.VBlank = false;
            }

            GraphicsDevice.Clear(Color.Black);

            // draw to buffer
            GraphicsDevice.SetRenderTarget(target);

            //nearest neighboor scaling
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Clear currently drawn frame
            GraphicsDevice.Clear(Color.Black);
            graphics.GraphicsDevice.Clear(Color.Black);

            // Write pixel data to texture
            frameBuffer.SetData<uint>(emulator.PixelData);

            // Draw texture frame to spritebatch
            spriteBatch.Draw(frameBuffer, frameBuffer.Bounds, Color.White);

            spriteBatch.End();

            //set rendering back to the back buffer
            GraphicsDevice.SetRenderTarget(null);

            //nearest neighboor scaling
            targetBatch.Begin(samplerState: SamplerState.PointClamp);

            // Account for change in window size
            drawDestination.Width = Window.ClientBounds.Width;
            drawDestination.Height = Window.ClientBounds.Height;

            targetBatch.Draw(target, drawDestination, Color.White);

            targetBatch.End();

            base.Draw(gameTime);
        }
    }
}
