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
            _commands = new(7);

            RegisterCommand(new CrashCommand());
            RegisterCommand(new FreezeCommand());
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

        public static CommandResult ExecuteCommand(Toolkit tookit, string input) {
            var parts = input.Split(' ');

            if (parts.Length == 0)
                return new(false, "Command is empty.");

            if (Commands.TryGetValue(parts[0], out var command)) {
                try {
                    return command.Execute(tookit, parts);
                }
                catch (Exception ex) {
                    return new(false, $"Exception while executing command \"{parts[0]}\": {ex.Message}");
                }
            }

            return new(false, $"Command \"{parts[0]}\" not found.");
        }

        public static CommandResult ExitError(this IToolkitCommand command) {
            return new(false, command.HelpMessage);
        }
    }
}
