using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace Laser
{
    public class NativeConstants
    {
        /// TYPE_EZAUDDAC -> 1
        public const int TYPE_EZAUDDAC = 1;

        /// TYPE_EASYLASE -> 2
        public const int TYPE_EASYLASE = 2;

        /// TYPE_RIYAUSB -> 3
        public const int TYPE_RIYAUSB = 3;

        /// TYPE_QM2000 -> 4
        public const int TYPE_QM2000 = 4;

        /// TYPE_SHOWTACLE -> 5
        public const int TYPE_SHOWTACLE = 5;

        /// TYPE_LUMAX -> 6
        public const int TYPE_LUMAX = 6;

        /// TYPE_NETLASE -> 7
        public const int TYPE_NETLASE = 7;

        /// TYPE_MEDIALAS -> 8
        public const int TYPE_MEDIALAS = 8;

        /// TYPE_LDS -> 9
        public const int TYPE_LDS = 9;

        /// TYPE_OLSD -> 10
        public const int TYPE_OLSD = 10;

        /// MASK_ALL -> ~0x0
        /// Error generating expression: Expression is not parsable.  Treating value as a raw string
        public const int MASK_ALL = ~0x0;

        /// MASK_EZAUDDAC -> 1
        public const int MASK_EZAUDDAC = 1;

        /// MASK_EASYLASE -> 2
        public const int MASK_EASYLASE = 2;

        /// MASK_RIYAUSB -> 4
        public const int MASK_RIYAUSB = 4;

        /// MASK_QM2000 -> 8
        public const int MASK_QM2000 = 8;

        /// MASK_MONCHA -> 16
        public const int MASK_MONCHA = 16;

        /// MASK_FIESTA -> 32
        public const int MASK_FIESTA = 32;

        /// MASK_LUMAX -> 64
        public const int MASK_LUMAX = 64;

        /// MASK_NETLASE -> 128
        public const int MASK_NETLASE = 128;

        /// MASK_MEDIALAS -> 256
        public const int MASK_MEDIALAS = 256;

        /// MASK_LDS -> 512
        public const int MASK_LDS = 512;

        /// MASK_OLSD -> 1024
        public const int MASK_OLSD = 1024;

        /// DAC_ERROR -> -1
        public const int DAC_ERROR = -1;

        /// DAC_OK -> 0
        public const int DAC_OK = 0;

        /// DAC_READY -> 1
        public const int DAC_READY = 1;

        /// DAC_WAIT -> 2
        public const int DAC_WAIT = 2;

        /// DAC_INVERT_X -> 1
        public const int DAC_INVERT_X = 1;

        /// DAC_INVERT_Y -> 2
        public const int DAC_INVERT_Y = 2;

        /// DAC_SWAPXY -> 512
        public const int DAC_SWAPXY = 512;

        /// REPEAT_MODE -> 0
        public const int REPEAT_MODE = 0;

        /// SINGLE_MODE -> 1
        public const int SINGLE_MODE = 1;

        /// SHUTTER_OPEN -> 1
        public const int SHUTTER_OPEN = 1;

        /// SHUTTER_CLOSE -> 0
        public const int SHUTTER_CLOSE = 0;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct laser_point
    {
        /// short
        public short x;

        /// short
        public short y;

        /// unsigned char
        public byte r;

        /// unsigned char
        public byte g;

        /// unsigned char
        public byte b;

        /// unsigned char
        public byte k;

        public static implicit operator LaserPoint(laser_point point)
        {
            return new LaserPoint
            (
                new Point(point.x, point.y),
                Color.FromArgb(point.r, point.g, point.b),
                (point.k == 0) ? true : false
            );
        }

        public static implicit operator laser_point(LaserPoint point)
        {
            bool draw = point.Draw;
            if (!draw)
                return new laser_point
                {
                    x = (short)point.Location.X,
                    y = (short)point.Location.Y,
                    k = 1
                };

            return new laser_point
            {
                x = (short)point.Location.X,
                y = (short)point.Location.Y,
                r = point.Color.R,
                g = point.Color.G,
                b = point.Color.B,
                k = 0
            };
        }
    }

    class NativeMethods
    {
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Write_Frame", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Write_Frame(uint nIndex, laser_point[] pPoints, uint nPoints, int nPPS, int nMode, float fScale = 1.0f, uint nInvert = 0, float fBright = 1.0f);

        /// Return Type: int
        ///nDeviceMask: unsigned int
        ///pNDevices: unsigned int*
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_Init", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_Init(uint nDeviceMask, ref uint pNDevices);


        /// Return Type: int
        ///nDeviceMask: unsigned int
        ///pNDevices: unsigned int*
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_Rescan", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_Rescan(uint nDeviceMask, ref uint pNDevices);

        /// Return Type: int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_Close", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_Close();

        /// Return Type: int
        ///nIndex: unsigned int
        ///sName: char*
        ///nNameSz: unsigned int
        ///nType: unsigned int*
        ///nTypeNum: unsigned int*
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_GetDACInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        //public static extern  int LDL_GetDACInfo(uint nIndex, System.IntPtr sName, uint nNameSz, ref uint nType, ref uint nTypeNum) ;
        public static extern int LDL_GetDACInfo(uint nIndex, StringBuilder sName, uint nNameSz, ref uint nType, ref uint nTypeNum);

        /// Return Type: int
        ///nIndex: unsigned int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Init", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Init(uint nIndex);

        /// Return Type: int
        ///nIndex: unsigned int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Close", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Close(uint nIndex);

        /// Return Type: int
        ///nIndex: unsigned int
        ///nCommand: int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Shutter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Shutter(uint nIndex, int nCommand);

        /// Return Type: int
        ///nIndex: unsigned int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Status", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Status(uint nIndex);

        /// Return Type: int
        ///nIndex: unsigned int
        [System.Runtime.InteropServices.DllImportAttribute("Laser_DAC_Library.dll", EntryPoint = "LDL_DAC_Stop", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int LDL_DAC_Stop(uint nIndex);
    }
}