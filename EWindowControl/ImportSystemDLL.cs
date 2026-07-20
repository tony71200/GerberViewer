using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EWindowControl
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportSystemDLL
    {
        /// <summary>
        /// hot key modifier key
        /// </summary>
        public enum KeyModifiers
        {
            /// <summary>
            /// no modifier key
            /// </summary>
            None = 0,
            /// <summary>
            /// Alt modifier key
            /// </summary>
            Alt = 1,
            /// <summary>
            /// Ctrl modifier key
            /// </summary>
            Control = 2,
            /// <summary>
            /// Shift modifier key
            /// </summary>
            Shift = 4,
            /// <summary>
            /// Win modifier key
            /// </summary>
            Windows = 8

        }
        /// <summary>
        /// hot key
        /// </summary>
        public enum eHotKey
        {
            /// <summary>
            /// None
            /// </summary>
            None = 0,
            /// <summary>
            /// SemiTest
            /// </summary>
            SemiTest = 500,
            /// <summary>
            /// LevelDeveloper
            /// </summary>
            LevelDeveloper = 600,
        }
        /// <summary>
        /// register hotkey
        /// </summary>
        /// <param name="hWnd">handle to window</param>
        /// <param name="id">hot key identifier</param>
        /// <param name="fsModifiers">key-modifier options</param>
        /// <param name="vk">virtual-key code</param>
        /// <returns></returns>
        [DllImport("user32.dll")] //declare API function
        public static extern bool RegisterHotKey(
             IntPtr hWnd, // handle to window
             int id, // hot key identifier
             uint fsModifiers, // key-modifier options
             Keys vk // virtual-key code
            );
        /// <summary>
        /// unregister hotkey
        /// </summary>
        /// <param name="hWnd">handle to window</param>
        /// <param name="id">hot key identifier</param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")] //declare API function
        public static extern bool UnregisterHotKey(
                IntPtr hWnd, // handle to window
                int id // hot key identifier
            );
    }
}
