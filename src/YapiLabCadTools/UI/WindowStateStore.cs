using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace YapiLabCadTools.UI
{
    /// <summary>
    /// Remembers the main window's size/position/maximized-state across AutoCAD sessions,
    /// so the user doesn't have to manually resize the palette every time it reopens
    /// (the form itself is recreated from scratch on every NETLOAD/new session).
    /// </summary>
    internal static class WindowStateStore
    {
        private const int MinWidth = 700;
        private const int MinHeight = 500;

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YapiLabCadTools",
            "window-state.json");

        /// <summary>
        /// Applies a previously saved bounds/state to the form, if one exists and still fits a
        /// screen. Returns false (and leaves the form untouched) when there is nothing usable to
        /// restore, so the caller can fall back to a computed default size.
        /// </summary>
        /// <remarks>
        /// The saved rectangle is always clamped into whichever screen it mostly overlaps —
        /// never trusted as-is. A monitor that has since been unplugged, or a bad value saved by
        /// an earlier bug, must never come back distorted or hanging off-screen; this is the
        /// only path that writes the form's Bounds, so fixing it here is enough.
        /// </remarks>
        public static bool TryRestore(Form form)
        {
            State? state = TryLoad();
            if (state is null || state.Width < MinWidth || state.Height < MinHeight)
            {
                return false;
            }

            var bounds = new Rectangle(state.X, state.Y, state.Width, state.Height);
            Screen? screen = BestScreenFor(bounds);
            if (screen is null)
            {
                return false;
            }

            Rectangle working = screen.WorkingArea;
            int width = Math.Min(bounds.Width, working.Width);
            int height = Math.Min(bounds.Height, working.Height);
            int x = Math.Max(working.Left, Math.Min(bounds.X, working.Right - width));
            int y = Math.Max(working.Top, Math.Min(bounds.Y, working.Bottom - height));

            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = new Rectangle(x, y, width, height);
            if (state.Maximized)
            {
                form.WindowState = FormWindowState.Maximized;
            }

            return true;
        }

        /// <summary>Persists the form's current bounds/state for the next time it opens.</summary>
        public static void Save(Form form)
        {
            Rectangle bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
            var state = new State
            {
                X = bounds.X,
                Y = bounds.Y,
                Width = bounds.Width,
                Height = bounds.Height,
                Maximized = form.WindowState == FormWindowState.Maximized
            };

            try
            {
                string? directory = Path.GetDirectoryName(FilePath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(state));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort only: losing the remembered size/position isn't worth surfacing to the user.
            }
        }

        private static State? TryLoad()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<State>(File.ReadAllText(FilePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return null;
            }
        }

        /// <summary>The screen the saved rectangle overlaps the most, or null if it overlaps none meaningfully.</summary>
        private static Screen? BestScreenFor(Rectangle bounds)
        {
            const int minVisiblePixels = 80;
            return Screen.AllScreens
                .Select(screen => (Screen: screen, Overlap: Rectangle.Intersect(screen.WorkingArea, bounds)))
                .Where(t => t.Overlap.Width >= minVisiblePixels && t.Overlap.Height >= minVisiblePixels)
                .OrderByDescending(t => (long)t.Overlap.Width * t.Overlap.Height)
                .Select(t => t.Screen)
                .FirstOrDefault();
        }

        private sealed class State
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool Maximized { get; set; }
        }
    }
}
