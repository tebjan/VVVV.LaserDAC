#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

using Laser;
using System.Collections.Generic;
using System.Drawing;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "LaserDAC",
                Category = "Devices",
                Help = "Universal laser controller",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class Template : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        [Input("Points")]
        public ISpread<Vector2D> FPointsInput;

        [Input("Colors")]
        public ISpread<RGBAColor> FColorsInput;

        [Input("Do Send", IsBang = true, IsSingle = true)]
        public ISpread<bool> FDoSendInput;

        [Input("Shutter", IsSingle = true)]
        public ISpread<bool> FShutterInput;

        [Input("Init", IsBang = true, IsSingle = true)]
        public ISpread<bool> FInitInput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        DAC FLaserDAC;
        IDisposable FShutter;


        private IEnumerable<LaserPoint> GetFrame()
        {
            var i = 0;
            foreach(var p in FPointsInput)
            {
                var col = FColorsInput[i].Color;
                var pos = new Point((int)(p.x * 32700), (int)(p.y * 32700));
                yield return new LaserPoint(pos, col, col.A > 0);
            }
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {

            //init
            if (FInitInput[0])
            {
                Init(ControllerType.EtherDream);
            }


            if (FLaserDAC != null)
            {
                try
                {
                    if (FShutterInput[0])
                    {
                        //open shutter
                        if (FShutter == null)
                        {
                            FShutter = FLaserDAC.OpenShutter();
                        }

                        if (FLaserDAC.IsShutterOpen && FDoSendInput[0])
                        {
                            FLaserDAC.WriteFrame(GetFrame());
                        }

                    }
                    else
                    {
                        //close shutter
                        if (FShutter != null)
                        {
                            FShutter.Dispose();
                            FShutter = null;
                        }
                    }
                }
                catch (Exception e)
                {                   
                    Log(e);
                }
            }

            //FLogger.Log(LogType.Debug, "hi tty!");
        }

        void Init(ControllerType type)
        {
            Log("Init Device: " + type.ToString());

            try
            {
                if (FLaserDAC != null)
                    FLaserDAC.Dispose();

                FLaserDAC = DAC.Initialize(ControllerTypes.All, type);
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
            if (FLaserDAC != null)
                FLaserDAC.Dispose();
        }
    }
}
