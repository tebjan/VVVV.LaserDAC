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
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class EtherDreamDACNode : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        [Input("Points")]
        public ISpread<ISpread<Vector2D>> FPointsInput;

        [Input("Colors")]
        public ISpread<RGBAColor> FColorsInput;
        
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

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        object FLaserDAC;
        int FDeviceNumber = 0;

        private IEnumerable<EtherDreamPoint> GetFrame()
        {
            var colIndex = 0;
            var shapeIndex = 0;
            foreach(var shape in FPointsInput)
            {
                var start = shape[0];
                var end = shape[shape.SliceCount - 1];
                
                var blanks = Enumerable.Repeat(CreateEtherDreamPoint(start, VColor.Black), FStartBlanksInput[shapeIndex]);
                
                foreach (var etherPoint in blanks) 
                {
                    yield return etherPoint;
                }
                
                foreach(var p in shape)
                {
                    yield return CreateEtherDreamPoint(p, FColorsInput[colIndex++]);
                }
                
                blanks = Enumerable.Repeat(CreateEtherDreamPoint(end, VColor.Black), FEndBlanksInput[shapeIndex++]);
                
                foreach (var etherPoint in blanks) 
                {
                    yield return etherPoint;
                }
            }
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


            if (FLaserDAC != null)
            {
                try
                {
                    if (FShutterInput[0])
                    {
                    	if(FDoSendInput[0])
                    	{
                        	var status = EtherDreamNative.WriteFrame(ref FDeviceNumber, GetFrame(), FPPSInput[0], FRepsInput[0]);
                        	
                    	}
                    }
                    else
                    {
                        EtherDreamNative.Stop(ref FDeviceNumber);
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
            Log("Init Device");

            try
            {
                var cards = EtherDreamNative.GetCardNum();
                if(cards > 0)
                {
                    var result = EtherDreamNative.OpenDevice(ref FDeviceNumber);
                    
                    if(result >= 0)
                    {
                        Log("Opened Device: " + EtherDreamNative.GetDeviceName(ref FDeviceNumber));
                        FLaserDAC = new object();
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
            EtherDreamNative.Close();
        }
    }
}
