#region usings
using System;
using System.ComponentModel.Composition;

using System.Linq;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

using EtherDream;
using System.Collections.Generic;
using System.Drawing;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "EtherDream",
                Category = "Devices",
                Help = "EtherDream laser controller",
                Tags = "laser",
                Author = "tonfilm",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class EtherDreamDACNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Points")]
        public ISpread<ISpread<Vector2D>> FPointsInput;

        [Input("Colors")]
        public ISpread<RGBAColor> FColorsInput;
        
        [Input("Point Repeat", DefaultValue = 1)]
        public ISpread<int> FPointRepeatInput;
        
        [Input("Interpolation Distance", DefaultValue = 1)]
        public ISpread<double> FPointInterpolationDistanceInput;
        
        [Input("Closed Shape")]
        public ISpread<bool> FClosedShapeInput;
        
        [Input("Start Blanks", DefaultValue = 1)]
        public ISpread<int> FStartBlanksInput;
        
        [Input("End Blanks", DefaultValue = 1)]
        public ISpread<int> FEndBlanksInput;
        
        [Input("Repetitions", IsSingle = true, DefaultValue = -1)]
        public ISpread<int> FRepsInput;

        [Input("Do Send", IsBang = true, IsSingle = true)]
        public ISpread<bool> FDoSendInput;

        [Input("Shutter", IsSingle = true)]
        public ISpread<bool> FShutterInput;
        
        [Input("Points per Second", IsSingle = true, DefaultValue = 15000)]
        public ISpread<int> FPPSInput;

        [Input("Init", IsBang = true, IsSingle = true)]
        public ISpread<bool> FInitInput;
        
        [Input("Output Debug Points", IsSingle = true)]
        public ISpread<bool> FShowDebugInput;

        [Input("Device", EnumName = "EtherDreamMACName", IsSingle = true)]
        public ISpread<EnumEntry> FDeviceIn;

        [Input("Prevent Close HACK", IsSingle = true)]
        public ISpread<bool> FPreventCloseHACKInput;

        [Input("DeviceNumber", IsSingle = true)]
        public ISpread<int> FDeviceNumberInput;

        [Output("Debug Points")]
        public ISpread<Vector2D> FPointsDebugOutput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        object FLaserDAC;
        int FDeviceNumber = 0;

        private IEnumerable<EtherDreamPoint> GetFrame()
        {
            var colIndex = 0;
            var shapeIndex = 0;
            var result = Enumerable.Empty<EtherDreamPoint>();          
            
            foreach(var shape in FPointsInput)
            {
                var isClosed = FClosedShapeInput[shapeIndex];
                
                Vector2D start;
                Vector2D end;
                
                if(isClosed)
                {
                    start = shape[shape.SliceCount - 1];
                    end = start;
                }
                else
                {
                    start = shape[0];
                    end = shape[shape.SliceCount - 1];
                }
                
                //start blanks
                result = result.Concat(Enumerable.Repeat(CreateEtherDreamPoint(start, VColor.Black), FStartBlanksInput[shapeIndex]));
               
                var lastPoint = Vector2D.Zero;
                var doInterpolate = false;
                foreach(var p in shape)
                {
                    var col = FColorsInput[colIndex++];

                    //interpolate from last point
                    if(doInterpolate)
                    {
                        var count = (int)Math.Floor(VMath.Dist(lastPoint, p) / Math.Max(FPointInterpolationDistanceInput[shapeIndex], 0.0001));
                        var factor = 1.0/(count+1);
                        
                        //points in between, need to be caculated directly since linq lazyness would access only the last value in lastPoint
                        result = result.Concat(Enumerable.Range(1, count).Select(index => CreateEtherDreamPoint(VMath.Lerp(lastPoint, p, index*factor), col)).ToArray());
                    }
                    else if (isClosed) // first iteration
                    {
                        var count = (int)Math.Floor(VMath.Dist(end, p) / Math.Max(FPointInterpolationDistanceInput[shapeIndex], 0.0001));
                        var factor = 1.0/(count+1);
                        
                        //points in between, need to be caculated directly since linq lazyness would access only the last value in lastPoint
                        result = result.Concat(Enumerable.Range(1, count).Select(index => CreateEtherDreamPoint(VMath.Lerp(end, p, index*factor), col)).ToArray());
                    }                    
                    

                    //actual point 
                    result = result.Concat(Enumerable.Repeat(CreateEtherDreamPoint(p, col), FPointRepeatInput[shapeIndex]));
                    
                    lastPoint = p;
                    doInterpolate = true;
                }
                              
                //end blanks
                result = result.Concat(Enumerable.Repeat(CreateEtherDreamPoint(end, VColor.Black), FEndBlanksInput[shapeIndex++]));
                
            }

            return result;
        }
        
        EtherDreamPoint CreateEtherDreamPoint(Vector2D pos, RGBAColor col)
        {
            return new EtherDreamPoint {
                X = (short)(pos.x * 32767),
                Y = (short)(pos.y * 32767),
                R = (ushort)(col.R * 32767),
                G = (ushort)(col.G * 32767),
                B = (ushort)(col.B * 32767),
                I = (ushort)(col.A * 32767)
            };
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {

            //init
            if (FInitInput[0])
            {
                Init();
            }

            EtherDreamPoint[] frame = null;

            if (FShowDebugInput[0])
            {
                frame = GetFrame().ToArray();
                FPointsDebugOutput.AssignFrom(frame.Select(edp => new Vector2D(edp.X / 32767.0, edp.Y / 32767.0)));
            }
            else
                FPointsDebugOutput.SliceCount = 0;

            if (FLaserDAC != null)
            {
                try
                {
                    if (FShutterInput[0])
                    {
                        if(FDoSendInput[0] && EtherDreamNative.GetStatus(ref FDeviceNumber) == EtherDreamStatus.Ready)
                    	{
                            if (frame == null)
                                frame = GetFrame().ToArray();

                        	var status = EtherDreamNative.WriteFrame(ref FDeviceNumber, frame, FPPSInput[0], FRepsInput[0]);                        	
                    	}
                    }
                    else
                    {
                        if(EtherDreamNative.GetStatus(ref FDeviceNumber) == EtherDreamStatus.Ready)
                            EtherDreamNative.WriteFrame(ref FDeviceNumber, Enumerable.Repeat(CreateEtherDreamPoint(Vector2D.Zero, VColor.Black), 1).ToArray(), FPPSInput[0], FRepsInput[0]);
                    }
                }
                catch (Exception e)
                {                   
                    Log(e);
                }
            }
            
            


            //FLogger.Log(LogType.Debug, "hi tty!");
        }

        void Init()
        {
            Log("EtheDream Init");

            try
            {
                //if(FLaserDAC != null)
                //{
                //    Dispose();
                //}

                //EnumerateDevices();
                
                //if(!string.IsNullOrWhiteSpace(FDeviceIn[0]))
                {
                    var deviceNumber = FDeviceNumberInput[0];
                    var result = EtherDreamNative.OpenDevice(ref deviceNumber);
                    
                    if (result >= 0)
                    {
                        Log("Opened Device: " + EtherDreamNative.GetDeviceName(ref deviceNumber));
                        FLaserDAC = new object();
                        FDeviceNumber = deviceNumber;
                    }
                }
                
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        void Log(object message)
        {
            FLogger.Log(LogType.Debug, message.ToString());
        }

        public void Dispose()
        {
            try
            {
                if (!FPreventCloseHACKInput[0])
                    EtherDreamNative.CloseDevice(ref FDeviceNumber);
            }
            catch (Exception e)
            {
                Log(e);
            }

            FLaserDAC = null;
        }

        static void EnumerateDevices()
        {
            var cards = EtherDreamNative.GetCardNum();
            if (cards > 0)
            {
                var names = new string[cards];
                for (int i = 0; i < cards; i++)
                {
                    names[i] = EtherDreamNative.GetDeviceName(ref i);
                }

                EnumManager.UpdateEnum("EtherDreamMACName", names[0], names);
            }
        }

        public void OnImportsSatisfied()
        {
            EnumerateDevices();
        }
    }
}
