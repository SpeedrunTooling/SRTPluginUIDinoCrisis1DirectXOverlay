using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderDinoCrisis1;
using SRTPluginProviderDinoCrisis1.Structs.GameStructs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SRTPluginUIDinoCrisis1DirectXOverlay
{
    public class SRTPluginUIDinoCrisis1DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderDinoCrisis1";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryDC1C gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("DINO")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        //STUFF
        SolidBrush HPBarColor;
        SolidBrush TextColor;

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont("Consolas", 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 100);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 172, 172);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 150);
            _lightgreen = _graphics?.CreateSolidBrush(150, 255, 150);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);
            HPBarColor = _grey;
            TextColor = _white;

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _red?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryDC1C)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                else
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);

                DrawOverlay();
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private void SetColors()
        {
            if (gameMemory.Player.HealthState == PlayerStatus.Fine) // Fine
            {
                HPBarColor = _darkgreen;
                TextColor = _lightgreen;
                return;
            }
            else if (gameMemory.Player.HealthState == PlayerStatus.FineToo) // Caution (Yellow)
            {
                HPBarColor = _darkyellow;
                TextColor = _lightyellow;
                return;
            }
            else if (gameMemory.Player.HealthState == PlayerStatus.Caution) // Caution (Yellow)
            {
                HPBarColor = _darkyellow;
                TextColor = _lightyellow; 
                return;
            }
            else if (gameMemory.Player.HealthState == PlayerStatus.Danger) // Danger (Red)
            {
                HPBarColor = _darkred; 
                 TextColor = _lightred;
                return;
            }
            else
            {
                HPBarColor = _greydarker;
                TextColor = _white;
                return;
            }
        }

        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            SetColors();

            float textOffsetX = 0f;
            //if (config.Debug)
            //{
            //}

            if (config.ShowIGT)
            {
                DrawStat(ref textOffsetX, ref statsYOffset, config.IGTString, gameMemory.IGTFormattedString);
            }

            if (config.ShowHPBars)
            {
                DrawHealthBar(ref statsXOffset, ref statsYOffset, "Regina: ", gameMemory.Player.CurrentHP, gameMemory.Player.MaxHP, gameMemory.Player.Percentage);
            }
            else
            {
                string perc = float.IsNaN(gameMemory.Player.Percentage) ? "0%" : string.Format("{0:P1}", gameMemory.Player.Percentage);
                _graphics?.DrawText(_consolasBold, 20f, _red, statsXOffset, statsYOffset += 24, "Player HP");
                _graphics?.DrawText(_consolasBold, 20f, TextColor, statsXOffset + 10f, statsYOffset += 24, string.Format("{0}{1} / {2} {3:P1}", "Regina: ", gameMemory.Player.CurrentHP, gameMemory.Player.MaxHP, perc));
            }

            DrawStat(ref textOffsetX, ref statsYOffset, "Room:", gameMemory.Stats.RoomName);
            DrawStat(ref textOffsetX, ref statsYOffset, "Saves:", gameMemory.Stats.SaveCount.ToString());
            DrawStat(ref textOffsetX, ref statsYOffset, "Continues:", gameMemory.Stats.Continues.ToString());

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            _graphics?.DrawText(_consolasBold, 20f, _red, xOffset, yOffset += 24f, config.EnemyString);
            //foreach (GameEnemy enemy in gameMemory.EnemyHealth.Where(a => a.IsAlive).OrderBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
            if (gameMemory.EnemyHealth.IsAlive)
            {
                var enemy = gameMemory.EnemyHealth;
                if (config.ShowHPBars)
                {
                    DrawEnemyHealthBar(ref xOffset, ref yOffset, enemy.CurrentHP, enemy.MaxHP, enemy.Percentage);
                }
                else
                {
                    _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset += 28f, string.Format("{0} / {1} {2:P1}", enemy.CurrentHP, enemy.MaxHP, enemy.Percentage));
                }
            }
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private void DrawStat(ref float dx, ref float dy, string label, string val)
        {
            _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, dy += 24, label);
            dx = config.PositionX + 15f + GetStringSize(label) + 10f;
            _graphics?.DrawText(_consolasBold, 20f, _lawngreen, dx, dy, val); //110f
        }

        private void DrawEnemyHealthBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 420f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 420f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 418f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (418f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, _lightred, xOffset + 10f, yOffset - 2f, string.Format("{0} / {1}", chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, _lightred, endOfBar, yOffset - 2f, perc);
        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 420f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 420f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 418f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (418f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, TextColor, xOffset + 10f, yOffset - 2f, string.Format("{0}{1} / {2}", name, chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, TextColor, endOfBar, yOffset - 2f, perc);
        }
    }
}
