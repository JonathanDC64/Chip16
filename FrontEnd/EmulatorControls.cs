using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FrontEnd
{
    internal class EmulatorControls : GameComponent
    {
        Form windowsGameForm;
        IGraphicsDeviceService graphicsService;
        GraphicsDevice graphics;
        public EmulatorControls(Game game) : base(game)
        {
            graphicsService = game.Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            graphics = graphicsService.GraphicsDevice;
        }

        Panel RenderPanel;
        MenuStrip MainMenu;

        public override void  Initialize()
        {
            windowsGameForm = Control.FromHandle(this.Game.Window.Handle) as Form;
            MainMenu = new MenuStrip();
            RenderPanel = new Panel();
            MainMenu.SuspendLayout();
            windowsGameForm.SuspendLayout();
            MainMenu.Location = new System.Drawing.Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Size = new Size(741, 24);
            MainMenu.TabIndex = 0;
            RenderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            RenderPanel.Location = new System.Drawing.Point(0, 49);
            RenderPanel.Name = "RenderPanel";
            RenderPanel.Size = new Size(800, 600);
            RenderPanel.TabIndex = 2;
            windowsGameForm.Controls.Add(MainMenu);
            windowsGameForm.Controls.Add(RenderPanel);
            MainMenu.ResumeLayout(false);
            MainMenu.PerformLayout();
            windowsGameForm.ResumeLayout(false);
            windowsGameForm.PerformLayout();

            // In Initialize() :
            graphicsService.DeviceResetting += new EventHandler<EventArgs>(OnDeviceReset);
            graphicsService.DeviceCreated += new EventHandler<EventArgs>(OnDeviceCreated);
            graphics.Reset();

            base.Initialize();
        }

        void OnDeviceCreated(object sender, EventArgs e)
        {
            graphics = graphicsService.GraphicsDevice;
            graphics.Reset();
        }

        void OnDeviceReset(object sender, EventArgs e)
        {
            graphics.PresentationParameters.DeviceWindowHandle = RenderPanel.Handle;
            graphics.PresentationParameters.BackBufferWidth = RenderPanel.Width;
            graphics.PresentationParameters.BackBufferHeight = RenderPanel.Height;
        }
    }
}
