using System;
using System.Collections.Generic;

namespace GalaxyToolkit.Commands {
    using CommandList = Dictionary<string, IToolkitCommand>;

    public static class CommandManager {
        private static readonly CommandList _commands;

        public static CommandList Commands {
            get => _commands;
        }

        static CommandManager() {
            _commands = new(8);

            RegisterCommand(new CrashCommand());
            RegisterCommand(new FreezeCommand());
            RegisterCommand(new HelpCommand());
            RegisterCommand(new LogCommand());
            // RegisterCommand(new ObjectCommand());
            RegisterCommand(new StageCommand());
            RegisterCommand(new WarpCommand());
            RegisterCommand(new ReadCommand());
            RegisterCommand(new WriteCommand());
        }

        public static void RegisterCommand<T>(T command) where T : IToolkitCommand {
            _commands.Add(command.Keyword, command);
        }

        public static bool ExecuteCommand(Toolkit tookit, string input) {
            var parts = input.Split(' ');

            if (parts.Length == 0)
                return false;

            if (Commands.TryGetValue(parts[0], out var command)) {
                bool result;

                try {
                    result = command.Execute(tookit, parts);
                }
                catch (Exception ex) {
                    result = false;
                    Console.WriteLine(ex.ToString());
                }

                if (!result)
                    Utils.WriteLineColor($"Error while executing command \"{parts[0]}\".", ConsoleColor.Red);

                return result;
            }

            Utils.WriteLineColor($"Command \"{parts[0]}\" not found.", ConsoleColor.Red);
            return false;
        }

        public static bool ExitError(this IToolkitCommand command, string? message = null) {
            Console.WriteLine(message ?? command.HelpMessage);
            return false;
        }
    }
}
