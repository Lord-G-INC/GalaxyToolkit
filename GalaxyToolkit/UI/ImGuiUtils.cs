using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace GalaxyToolkit.UI {
    public static class ImGuiUtils {
        public static readonly Vector4 ColorRed = new(1, 0, 0, 1);

        public static Vector4 GetColor(byte r, byte g, byte b, byte a = 255) {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        public static unsafe ImFontPtr LoadFont(string path, float size) {
            var io = ImGui.GetIO();

            var config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->OversampleH = 2;
            config->OversampleV = 2;
            config->RasterizerMultiply = 1f;

            return io.Fonts.AddFontFromFileTTF(path, size, config, io.Fonts.GetGlyphRangesDefault());
        }

        public static unsafe void BuildFonts(ref GL gl) {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out int bytesPerPixel);

            var bytes = new Span<byte>(pixels.ToPointer(), width * height * bytesPerPixel);
            var id = gl.GenTexture();

            gl.BindTexture(TextureTarget.Texture2D, id);
            gl.TexImage2D<byte>(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
                (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, bytes);

            var glParams = (uint)GLEnum.Nearest;
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ref glParams);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ref glParams);

            glParams = (uint)GLEnum.ClampToEdge;
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ref glParams);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ref glParams);

            gl.BindTexture(TextureTarget.Texture2D, 0);
            io.Fonts.SetTexID((nint)id);
            io.Fonts.ClearTexData();
        }

        public static void Spacing(float offset) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offset);
        }

        public static void TextSelectable(string str, Vector2 size, uint color = 0) {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, uint.MinValue);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.InputTextMultiline("###", ref str, (uint)str.Length, size, ImGuiInputTextFlags.ReadOnly);
            ImGui.PopStyleColor(2);
        }
    }
}
