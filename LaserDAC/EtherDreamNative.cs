/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 09.03.2015
 * Time: 08:59
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherDream
{
    //struct EAD_Pnt_s {
    //    int16_t X;
    //    int16_t Y;
    //    int16_t R;
    //    int16_t G;
    //    int16_t B;
    //    int16_t I;
    //    int16_t AL;
    //    int16_t AR;
    //};
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct EtherDreamPoint
    {
        public short X;
        public short Y;
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort I;
        public ushort AL;
        public ushort AR;
    }
    
    public enum EtherDreamStatus
    {
        Ready = 1,
        Busy = 2
    }
    
    /// <summary>
    /// Wrapper around the EtherDream.dll
    /// </summary>
    public static class EtherDreamNative
    {
        static readonly int SizeOfEtherDreamPoint = Marshal.SizeOf(typeof(EtherDreamPoint));
        
        //J4CDAC_API int __stdcall EtherDreamGetCardNum(void);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamGetCardNum", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCardNum();
        
        //J4CDAC_API void __stdcall EtherDreamGetDeviceName(const int *CardNum, char *buf, int max);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamGetDeviceName", CharSet = CharSet.Ansi)]
        static extern int GetDeviceName(ref int cardNum, StringBuilder name, int nameSize);
        
        public static string GetDeviceName(ref int cardNum)
        {
            var sb = new StringBuilder(128);
            var status = GetDeviceName(ref cardNum, sb, sb.Capacity);           
            return status >= 0 ? sb.ToString() : "";
        }
        
        //J4CDAC_API bool __stdcall EtherDreamOpenDevice(const int *CardNum);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamOpenDevice")]
        public static extern int OpenDevice(ref int cardNum);

        //J4CDAC_API bool __stdcall EtherDreamWriteFrame(const int *CardNum, const struct EAD_Pnt_s* data, int Bytes, uint16_t PPS, uint16_t Reps);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamWriteFrame")]
        static extern int WriteFrame(ref int cardNum, [In] EtherDreamPoint[] points, int bytes, short pps, short reps);
        
        public static int WriteFrame(ref int cardNum, EtherDreamPoint[] points, int pps, int reps)
        {
            return WriteFrame(ref cardNum, points, points.Length * SizeOfEtherDreamPoint, (short) pps, (short) reps);
        }
        
        //J4CDAC_API int __stdcall EtherDreamGetStatus(const int *CardNum);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamGetStatus")]
        static extern int NativeGetStatus(ref int cardNum);
        
        public static EtherDreamStatus GetStatus(ref int cardNum)
        {
            return (EtherDreamStatus)NativeGetStatus(ref cardNum);
        }
        
        //J4CDAC_API bool __stdcall EtherDreamStop(const int *CardNum);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamStop")]
        public static extern int Stop(ref int cardNum);
        
        //J4CDAC_API bool __stdcall EtherDreamCloseDevice(const int *CardNum);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamCloseDevice")]
        public static extern int CloseDevice(ref int cardNum);
        
        //J4CDAC_API bool __stdcall EtherDreamClose(void);
        [DllImport("EtherDream.dll", EntryPoint = "EtherDreamClose")]
        public static extern int Close();
    }
}
