using System;

namespace FrontEnd
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Chip16.CPU cpu = new Chip16.CPU(new Chip16.Memory(), new Chip16.Graphics());
            //using (var game = new Game1())
            //    game.Run();
        }
    }
}
