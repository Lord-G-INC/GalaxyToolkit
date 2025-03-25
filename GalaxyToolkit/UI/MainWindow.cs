using DolphinMemory;
using GalaxyToolkit.Commands;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Silk.NET.Maths;

namespace GalaxyToolkit.UI {
    public class MainWindow {
        private readonly IWindow WindowController;

        [AllowNull] private GL GLController;
        [AllowNull] private ImGuiController ImGuiController;
        [AllowNull] private IInputContext InputContext;

        private ImGuiIOPtr GuiIO;
        private ImGuiStylePtr GuiStyle;
        private ImFontPtr DefaultFont;

        private Toolkit Toolkit;
        private Dolphin Dolphin;

        public MainWindow() {
            var options = WindowOptions.Default;
            options.Title = "GalaxyToolkit";
            options.Size = new(1100, 650);
            WindowController = Window.Create(options);

            WindowController.Load += OnLoad;
            WindowController.FramebufferResize += FramebufferResize;
            WindowController.Render += OnRender;
            WindowController.Closing += OnClose;
        }

        public void Run() => WindowController.Run();

        private void OnLoad() {
            GLController = WindowController.CreateOpenGL();
            InputContext = WindowController.CreateInput();
            ImGuiController = new ImGuiController(GLController, WindowController, InputContext, InitConfig);
        }

        private void InitConfig() {
            GuiIO = ImGui.GetIO();
            GuiIO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            GuiStyle = ImGui.GetStyle();
            GuiStyle.AntiAliasedFill = true;
            GuiStyle.ChildRounding = 0;
            GuiStyle.FrameRounding = 2;
            GuiStyle.WindowRounding = 5;

            var colors = GuiStyle.Colors;
            colors[(int)ImGuiCol.DockingEmptyBg] = ImGuiUtils.GetColor(10, 10, 10);

            DefaultFont = ImGuiUtils.LoadFont("UI/NotoSansMono.ttf", 18);
            ImGuiUtils.BuildFonts(ref GLController);
        }

        private void FramebufferResize(Vector2D<int> size) {
            GLController.Viewport(size);
        }

        private void OnRender(double deltaTime) {
            ImGuiController.Update((float)deltaTime);
            ImGui.PushFont(DefaultFont);
            ImGui.DockSpaceOverViewport();            

            if (Toolkit is null) {
                ShowDolphinSelectWindow();
            }
            else {
                ShowConfigWindow();
                ShowCommandsWindow();
            }

            ImGui.PopFont();
            ImGuiController.Render();
        }

        private Process[] DolphinProcesses;
        private int DolphinProcessIndex = 0;
        private string DolphinSelectError = string.Empty;

        private void ShowDolphinSelectWindow() {
            RefreshDolphinProcesses();

            var fullWindowSize = new Vector2(WindowController.Size.X, WindowController.Size.Y);
            var windowSize = fullWindowSize * 0.7f;
            var windowPos = (fullWindowSize - windowSize) / 2.0f;

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

            ImGui.Begin("Select a Dolphin Process##DolphinSelectWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

            if (DolphinProcesses.Length == 0) {
                ImGui.Text("No Dolphin processes were found.");
            }
            else {
                ImGui.Text("Select a Dolphin process.");

                var childSize = new Vector2(0, windowSize.Y - 70);
                var refreshButtonY = ImGui.GetCursorPos().Y + childSize.Y + 5;

                ImGui.BeginChild("ModuleList", childSize);
                for (int i = 0; i < DolphinProcesses.Length; i++) {
                    var process = DolphinProcesses[i];

                    if (ImGui.Selectable($" {process.MainWindowTitle}##DolphinSelect{i}", i == DolphinProcessIndex)) {
                        DolphinProcessIndex = i;
                    }
                }
                ImGui.EndChild();

                ImGui.SetCursorPosY(refreshButtonY);
                if (ImGui.Button("Select Process"))
                    InitToolkit();
                ImGui.SameLine();
                ImGui.TextColored(ImGuiUtils.ColorRed, DolphinSelectError);
            }

            ImGui.End();

            void InitToolkit() {
                if (DolphinProcesses.Length == 0 || DolphinProcessIndex >= DolphinProcesses.Length)
                    return;

                EndToolkit();

                try {
                    Dolphin = new(DolphinProcesses[DolphinProcessIndex]);
                    Toolkit = new(Dolphin);
                    DolphinProcesses = [];

                    DolphinProcessIndex = 0;
                    DolphinSelectError = string.Empty;
                }
                catch (Exception ex) {
                    DolphinSelectError = ex.Message;
                    EndToolkit();
                }
            }

            void EndToolkit() {
                Dolphin?.Dispose();
                Toolkit?.Dispose();
                Dolphin = null;
                Toolkit = null;
            }
        }

        private string GameFilesPath = string.Empty;

        private void ShowConfigWindow() {
            ImGui.SetNextWindowSize(new(650, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("Configuration##ConfigWindow");

            ImGui.SeparatorText("Information");
            ImGui.Text($"Current Region: '{Toolkit.Region}'");
            ImGui.Text($"Message Address: 0x{Toolkit.ToolMessage}");

            ImGuiUtils.Spacing(20);
            ImGui.SeparatorText("Files");
            ImGui.InputText("Game Files Path", ref GameFilesPath, 260);
            ImGui.Button("Reload Symbols");

            ImGuiUtils.Spacing(20);
            ImGui.SeparatorText("Tool Information");
            ImGui.Text(GuiIO.Framerate.ToString("FPS: 000.00"));
            ImGui.Text($"Command Count: {CommandManager.Commands.Count}");

            ImGui.End();
        }

        private string CommandInput = string.Empty;
        private string? CommandInputPredict;

        private CommandResult? CommandResult = null;

        private void ShowCommandsWindow() {
            ImGui.SetNextWindowSize(new(650, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin("Commands##CommandsWindow");

            if (ImGui.Button("Run Command")) {
                ExecuteCommand();
            }

            ImGui.SameLine();

            var commandInputPos = ImGui.GetCursorScreenPos();

            unsafe {
                if (ImGui.InputText("Command##CommandInput", ref CommandInput, 100, ImGuiInputTextFlags.AllowTabInput)) {
                    RefreshPredicts();
                }
            }
            
            if (CommandInputPredict is not null) {
                var drawList = ImGui.GetForegroundDrawList();
                var offset = ImGui.CalcTextSize(CommandInput).X;

                drawList.AddText(new(commandInputPos.X + 4 + offset, commandInputPos.Y + 3), 0xAAFFFFFF, CommandInputPredict);
            }

            ImGuiUtils.Spacing(20);
            ImGui.SeparatorText("Command Result");

            ImGui.BeginChild("CommandOutput");

            if (CommandResult is not null)
                ImGuiUtils.TextSelectable(CommandResult.Message, ImGui.GetWindowSize(), CommandResult.Success ? uint.MaxValue : 0xFF0000FF);

            ImGui.EndChild();
            ImGui.End();
        }

        private void ExecuteCommand() {
            CommandResult = Toolkit.ExecuteCommand(CommandInput);
        }

        private void RefreshDolphinProcesses() {
            DolphinProcesses = Process.GetProcessesByName("dolphin");
        }

        private void RefreshPredicts() {
            if (CommandInput is null || CommandInput == string.Empty) {
                CommandInputPredict = string.Empty;
                return;
            }

            CommandInputPredict = CommandManager.Commands.Keys.FirstOrDefault(k => k.StartsWith(CommandInput));

            if (CommandInputPredict is not null)
                CommandInputPredict = CommandInputPredict[CommandInput.Length..];
        }

        private void OnClose() {
            ImGuiController.Dispose();
            GLController.Dispose();
        }
    }
}
