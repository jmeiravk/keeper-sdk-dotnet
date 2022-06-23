using Cli;
using KeeperSecurity.Utils;
using System;

namespace SharedFolderPermissions
{
    internal class Program
    {
        private static readonly InputManager InputManager = new InputManager();

        public static InputManager GetInputManager()
        {
            return InputManager;
        }
        
        static void Main(string[] args)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Utils.Welcome();

            var mainLoop = new MainLoop
            {
                StateContext = new MainMenuCliContext()
            };

            _ = mainLoop.Run(GetInputManager());
            InputManager.Run();
        }
    }
}
