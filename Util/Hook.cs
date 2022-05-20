using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dwarf_Fortress_Log.Util
{
    public class Hook
    {
        public delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            WindowsServices.SWEH_Events eventType,
            IntPtr hwnd,
            WindowsServices.SWEH_ObjectId idObject,
            long idChild,
            uint dwEventThread,
            uint dwmsEventTime
        );

        public static IntPtr WinEventHookRange(
            WindowsServices.SWEH_Events eventFrom, WindowsServices.SWEH_Events eventTo,
            WinEventDelegate eventDelegate,
            uint idProcess, uint idThread)
        {
            return WindowsServices.SetWinEventHook(
                eventFrom, eventTo,
                IntPtr.Zero, eventDelegate,
                idProcess, idThread,
                WindowsServices.WinEventHookInternalFlags);
        }

        public static IntPtr WinEventHookOne(
            WindowsServices.SWEH_Events eventId,
            WinEventDelegate eventDelegate,
            uint idProcess,
            uint idThread)
        {
            return WindowsServices.SetWinEventHook(
                eventId, eventId,
                IntPtr.Zero, eventDelegate,
                idProcess, idThread,
                WindowsServices.WinEventHookInternalFlags);
        }

        public static bool WinEventUnhook(IntPtr hWinEventHook) =>
            WindowsServices.UnhookWinEvent(hWinEventHook);

        public static uint GetWindowThread(IntPtr hWnd)
        {
            return WindowsServices.GetWindowThreadProcessId(hWnd, IntPtr.Zero);
        }

        public static WindowsServices.RECT GetWindowRectangle(IntPtr hWnd)
        {
            WindowsServices.DwmGetWindowAttribute(hWnd,
                WindowsServices.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                out WindowsServices.RECT rect, Marshal.SizeOf<WindowsServices.RECT>());
            return rect;
        }
    }
}
