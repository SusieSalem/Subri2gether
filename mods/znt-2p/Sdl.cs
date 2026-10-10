using System;
using System.Runtime.InteropServices;

namespace Znt2P
{
    // Minimal SDL2 game-controller wrapper (x86). Loaded by full path so SDL2.dll can live next to the plugin.
    internal static class Sdl
    {
        public const int AxisLeftX = 0, AxisLeftY = 1, AxisRightX = 2, AxisRightY = 3, AxisTriggerLeft = 4, AxisTriggerRight = 5;
        public const int ButtonA = 0, ButtonB = 1, ButtonX = 2, ButtonY = 3, ButtonBack = 4, ButtonGuide = 5, ButtonStart = 6,
            ButtonLStick = 7, ButtonRStick = 8, ButtonLShoulder = 9, ButtonRShoulder = 10,
            ButtonDpadUp = 11, ButtonDpadDown = 12, ButtonDpadLeft = 13, ButtonDpadRight = 14;
        public const int ButtonCount = 15;

        private const uint InitJoystick = 0x00000200, InitGameController = 0x00002000;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibraryW(string path);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_Init(uint flags);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_Quit();
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_SetHint([MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string value);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_NumJoysticks();
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_IsGameController(int index);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_GameControllerOpen(int index);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_GameControllerClose(IntPtr gc);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_GameControllerGetAttached(IntPtr gc);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_GameControllerUpdate();
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern short SDL_GameControllerGetAxis(IntPtr gc, int axis);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern byte SDL_GameControllerGetButton(IntPtr gc, int button);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_GameControllerName(IntPtr gc);
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_GetError();
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern int SDL_GameControllerRumble(IntPtr gc, ushort low, ushort high, uint ms);

        private static bool initialized;
        private static IntPtr pad = IntPtr.Zero;
        private static float nextScan;

        public static string LastError { get; private set; }
        public static string PadName { get; private set; }
        public static bool Connected { get { return pad != IntPtr.Zero; } }

        public static bool Init(string dllPath)
        {
            if (initialized) return true;
            try
            {
                if (LoadLibraryW(dllPath) == IntPtr.Zero)
                {
                    LastError = "Could not load " + dllPath + " (Win32 error " + Marshal.GetLastWin32Error() + ")";
                    return false;
                }
                SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
                SDL_SetHint("SDL_JOYSTICK_HIDAPI_SWITCH", "1");
                if (SDL_Init(InitJoystick | InitGameController) != 0)
                {
                    LastError = "SDL_Init failed: " + Marshal.PtrToStringAnsi(SDL_GetError());
                    return false;
                }
                initialized = true;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static void Shutdown()
        {
            if (!initialized) return;
            if (pad != IntPtr.Zero) { SDL_GameControllerClose(pad); pad = IntPtr.Zero; }
            SDL_Quit();
            initialized = false;
        }

        // Call once per frame. Rescans for a controller about once a second while none is attached.
        public static void Update(float unscaledTime)
        {
            if (!initialized) return;
            SDL_GameControllerUpdate();
            if (pad != IntPtr.Zero && SDL_GameControllerGetAttached(pad) == 0)
            {
                SDL_GameControllerClose(pad);
                pad = IntPtr.Zero;
                PadName = null;
            }
            if (pad == IntPtr.Zero && unscaledTime >= nextScan)
            {
                nextScan = unscaledTime + 1f;
                int n = SDL_NumJoysticks();
                for (int i = 0; i < n; i++)
                {
                    if (SDL_IsGameController(i) == 0) continue;
                    IntPtr gc = SDL_GameControllerOpen(i);
                    if (gc == IntPtr.Zero) continue;
                    pad = gc;
                    PadName = Marshal.PtrToStringAnsi(SDL_GameControllerName(gc));
                    break;
                }
            }
        }

        // Returns -1..1.
        public static float Axis(int axis)
        {
            if (pad == IntPtr.Zero) return 0f;
            short v = SDL_GameControllerGetAxis(pad, axis);
            return v < 0 ? v / 32768f : v / 32767f;
        }

        // low/high motor strength 0..1. No-op if no pad or the pad has no rumble.
        public static void Rumble(float low, float high, int ms)
        {
            if (pad == IntPtr.Zero) return;
            try
            {
                SDL_GameControllerRumble(pad,
                    (ushort)(Math.Max(0f, Math.Min(1f, low)) * 65535f),
                    (ushort)(Math.Max(0f, Math.Min(1f, high)) * 65535f),
                    (uint)ms);
            }
            catch (EntryPointNotFoundException) { }
        }

        public static bool Button(int button)
        {
            return pad != IntPtr.Zero && SDL_GameControllerGetButton(pad, button) != 0;
        }
    }
}
