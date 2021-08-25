using System;
using System.Runtime.InteropServices;

namespace ytplayer.intelop
{
    public class KeyState
    {
        [DllImport("user32")]
        private static extern Int16 GetAsyncKeyState(int vKey);

        // winuser.hで定義されている
        public const int VK_LBUTTON        = 0x01;
        public const int VK_RBUTTON        = 0x02;
        public const int VK_MBUTTON        = 0x04;   /* NOT contiguous with L & RBUTTON */
        public const int VK_SHIFT          = 0x10;
        public const int VK_CONTROL        = 0x11;
        public const int VK_MENU           = 0x12;

        public static bool CheckSpecialKeyDown()
        {
            return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
        }

        public static bool IsKeyDown(int vkey)
        {
            return (GetAsyncKeyState(vkey) & 0x8000) != 0 ;
        }
    }
}
