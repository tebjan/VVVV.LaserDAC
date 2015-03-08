using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;

namespace Laser
{
    public sealed class DAC : IDisposable
    {
        #region Fields

        static DAC instance;
        readonly ControllerType type;
        readonly string name;
        readonly uint device;
        bool disposed;
        readonly ReferenceCounter shutter;

        #endregion

        #region Singleton

        /// <summary>
        /// Gets the singleton instance. Use <see cref="DAC.Initialize(Laser.ControllerType,Laser.DeviceChooser)"/> 
        /// to initialize this singleton.
        /// </summary>
        public DAC Instance
        {
            get
            {
                if (instance == null)
                    throw new InvalidOperationException("DAC not initialized. Call DAC.TryInitialize(...) first.");

                return instance;
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DAC"/> class from being created.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name. Can be empty or null.</param>
        /// <param name="device">The device number of this <see cref="DAC"/>.</param>
        DAC(ControllerType type, string name, uint device)
        {
            if (instance != null)
                throw new InvalidOperationException("DAC not initialized. Call DAC.TryInitialize(...) first.");

            this.type = type;
            this.name = name;
            this.device = device;

            this.shutter = new ReferenceCounter(OnCloseShutter, OnOpenShutter);

            instance = this;
        }

        #endregion

        #region Methods

        #region Demo

        public void RenderDemoFrames(uint frameCount = 400)
        {
            using (OpenShutter())
            {
                CreateDemoFrames(WriteFrame, frameCount);
            }
        }

        public bool WriteFrame(IEnumerable<LaserPoint> frame)
        {
            if (frame == null || !frame.Any())
                return false;

            using (IsShutterOpen ? null : OpenShutter())
            {
                uint nTimeout = 300;
                while (NativeMethods.LDL_DAC_Status(device) != NativeConstants.DAC_READY && nTimeout > 0)
                {
                    Thread.Sleep(1);
                    nTimeout--;
                }

                // timed out
                if (nTimeout == 0)
                    return false;

                laser_point[] realFrame = frame.Select(p => (laser_point) p).ToArray();

                const int PointRate = 15000;
                if (NativeMethods.LDL_DAC_Write_Frame(device, realFrame, (uint) realFrame.Length, PointRate, NativeConstants.REPEAT_MODE) != NativeConstants.DAC_OK)
                    return false;

                return true;
            }
        }

        static IEnumerable<LaserPoint[]> CreateDemoFrames()
        {
            const uint frameCount = 400;
            var frames = new List<LaserPoint[]>((int) frameCount);
            var func = new Func<LaserPoint[], bool>(delegate(LaserPoint[] frame)
            {
                frames.Add(frame);
                return true;
            });

            CreateDemoFrames(func, frameCount);
            return frames;
        }

        static void CreateDemoFrames(Func<LaserPoint[], bool> onFrameCreated, uint frameCount = 400)
        {
            if (onFrameCreated == null)
                throw new ArgumentNullException("onFrameCreated");

            const int nPointCnt = 300;

            // create a set of frames
            for (int nFrames = 0; nFrames < frameCount; nFrames++)
			{
                var frame = new LaserPoint[nPointCnt];
                //draw some nice basic circle into the frame
			    for (int i = 0; i < nPointCnt; i++)
			    {
                    var location = new Point(
                        (int)(Math.Sin(i*2.0f*Math.PI/nPointCnt)*32700),
                        (int) (Math.Cos(i*2.0f*Math.PI/nPointCnt)*32700)
                        );

                    var color = Color.FromArgb(
                        (int)(Math.Sin(i*2.0f*Math.PI/nPointCnt + nFrames/50.0f)*127 + 127),
                        (int)(Math.Cos(i*2.0f*Math.PI/nPointCnt - nFrames/50.0f)*127 + 127),
                        (int)(Math.Cos(i*2.0f*Math.PI/nPointCnt + Math.PI/4 + nFrames/50.0f)*127 + 127)
                        );

                    frame[i] = new LaserPoint(location, color, true);
			    }

                onFrameCreated(frame);
			}
        }

        #endregion

        /// <summary>
        /// Opens the shutter. Call <see cref="IDisposable.Dispose"/> to close the shutter.
        /// </summary>
        /// <returns></returns>
        public IDisposable OpenShutter()
        {
            return shutter.Suspend();
        }

        void OnOpenShutter()
        {
            if (NativeMethods.LDL_DAC_Shutter(device, NativeConstants.SHUTTER_OPEN) != NativeConstants.DAC_OK)
                throw new InvalidOperationException(string.Format("Could not open shutter for '{0}' on device {1}.", type, device));
        }

        void OnCloseShutter()
        {
            if (NativeMethods.LDL_DAC_Shutter(device, NativeConstants.SHUTTER_CLOSE) != NativeConstants.DAC_OK)
                throw new InvalidOperationException(string.Format("Could not close shutter for '{0}' on device {1}.", type, device));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of the controller.
        /// </summary>
        public ControllerType ControllerType
        {
            get { return type; }
        }

        /// <summary>
        /// Gets the name of the controller.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets a value indicating whether the shutter is currently open.
        /// </summary>
        public bool IsShutterOpen { get { return shutter.IsReferenced; } }

        #endregion

        #region Static initialize methods

        /// <summary>
        /// Retrieves all available controllers using the specified <see cref="ControllerTypes"/> mask.
        /// </summary>
        /// <param name="types">The mask that specified the types of controllers to use.</param>
        /// <param name="deviceCount">The number of devices that have been found.</param>
        /// <returns>
        ///   <c>True</c> if <see cref="DAC.Instance"/> was successfully initialized. <c>False</c> else.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">If <see cref="DAC.Instance"/> has been initialized.</exception>
        /// <exception cref="T:System.ArgumentException">If any error occurred while retrieving device information.</exception>
        public static bool TryGetControllers(ControllerTypes types, out uint deviceCount)
        {
            if (instance != null)
                throw new InvalidOperationException(string.Format("DAC already initialized with '{0}'. Shutdown/Dispose DAC before retrieving new information.", instance.ControllerType));

            try
            {
                deviceCount = 0;
                if (NativeMethods.LDL_Init((uint) ConvertToNativeMask(types), ref deviceCount) != NativeConstants.DAC_OK)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not retrieve controllers for '{0}'.", types), ex);
            }
            finally
            {
                NativeMethods.LDL_Close();
            }
        }

        /// <summary>
        /// Tries to initialize the <see cref="DAC.Instance"/> using the specified <see cref="ControllerType"/> mask.
        /// </summary>
        /// <param name="types">The types of controllers that should be used during initialization.</param>
        /// <param name="type">The type of controllers to use.</param>
        /// <param name="deviceChooser">The device chooser. Once the overall number of devices has been retrieved, this
        /// callback is asked which device should actually be used. Can be null. If null, the first device that has been
        /// found will be used.</param>
        /// <exception cref="T:System.InvalidOperationException">If <see cref="DAC.Instance"/> has been initialized.</exception>
        ///   
        /// <exception cref="T:System.ArgumentException">If any error occurred while initializing the device.</exception>
        public static DAC Initialize(ControllerTypes types, ControllerType type, DeviceChooser deviceChooser = null)
        {
            if (instance != null)
                throw new InvalidOperationException(string.Format("DAC already initialized with '{0}'. Shutdown/Dispose DAC before retrieving new information.", instance.ControllerType));

            try
            {
                uint deviceCount = 0;
                if (NativeMethods.LDL_Init((uint) ConvertToNativeMask(types), ref deviceCount) != NativeConstants.DAC_OK)
                    throw new ArgumentException(string.Format("Could not intialize DAC with '{0}'", type));

                uint device = deviceChooser == null ? 0 : deviceChooser(deviceCount);

                uint tType = 0;
                uint tEnum = 0;
                var sName = new StringBuilder(128);
                if (NativeMethods.LDL_GetDACInfo(device, sName, (uint) sName.Length, ref tType, ref tEnum) != NativeConstants.DAC_OK)
                    throw new ArgumentException(string.Format("Could retriev name of DAC device number {0} with '{1}'.", device, type));

                if (NativeMethods.LDL_DAC_Init(device) != NativeConstants.DAC_OK)
                    throw new ArgumentException(string.Format("Could not intialize DAC with '{0}' using device number {1}", type, device));

                instance = new DAC(type, sName.ToString(), device);
                
                return instance;
            }
            catch (Exception ex)
            {
                NativeMethods.LDL_Close();
                throw new ArgumentException(string.Format("Could not initialize DAC.Instance with '{0}'.", type), ex);
            }
        }

        #endregion

        #region Helper methods

        static int ConvertToNativeType(ControllerType type)
        {
            switch (type)
            {
                case ControllerType.EtherDream:
                    return NativeConstants.TYPE_EZAUDDAC;
                case ControllerType.EasyLase:
                    return NativeConstants.TYPE_EASYLASE;
                case ControllerType.RiyaUSB:
                    return NativeConstants.TYPE_RIYAUSB;
                case ControllerType.QM2000:
                    return NativeConstants.TYPE_QM2000;
                case ControllerType.ShowTacle:
                    return NativeConstants.TYPE_SHOWTACLE;
                case ControllerType.Lumax:
                    return NativeConstants.TYPE_LUMAX;
                case ControllerType.Netlase:
                    return NativeConstants.TYPE_NETLASE;
                case ControllerType.Medialas:
                    return NativeConstants.TYPE_MEDIALAS;
                case ControllerType.LDS:
                    return NativeConstants.TYPE_LDS;
                case ControllerType.OpenLaserShowDAC:
                    return NativeConstants.TYPE_OLSD;
                default:
                    throw new NotImplementedException(string.Format("There is no implementation for '{0}'", type));
            }
        }

        static int ConvertToNativeMask(ControllerTypes types)
        {
            switch (types)
            {
                case ControllerTypes.All:
                    return NativeConstants.MASK_ALL;
                case ControllerTypes.EZAUDDAC:
                    return NativeConstants.MASK_EZAUDDAC;
                case ControllerTypes.EasyLase:
                    return NativeConstants.MASK_EASYLASE;
                case ControllerTypes.RiyaUSB:
                    return NativeConstants.MASK_RIYAUSB;
                case ControllerTypes.QM2000:
                    return NativeConstants.MASK_QM2000;
                case ControllerTypes.Moncha:
                    return NativeConstants.MASK_MONCHA;
                case ControllerTypes.Fiesta:
                    return NativeConstants.MASK_FIESTA;
                case ControllerTypes.Lumax:
                    return NativeConstants.MASK_LUMAX;
                case ControllerTypes.Netlase:
                    return NativeConstants.MASK_NETLASE;
                case ControllerTypes.Medialas:
                    return NativeConstants.MASK_MEDIALAS;
                case ControllerTypes.LDS:
                    return NativeConstants.MASK_LDS;
                case ControllerTypes.OLSD:
                    return NativeConstants.MASK_OLSD;
                default:
                    throw new NotImplementedException(string.Format("There is no implementation for '{0}'", types));
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed { get { return disposed; } }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="DAC"/> is reclaimed by garbage collection.
        /// </summary>
        ~DAC()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            try
            {
                if (!disposing || disposed)
                    return;

                NativeMethods.LDL_DAC_Stop(device);
                NativeMethods.LDL_Close();
            }
            finally
            {
                if (instance == this)
                    instance = null;

                disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Once the overall number of devices has been retrieved, this callback is asked which device should actually be used.
    /// </summary>
    /// <param name="devices">The number of devices that have been found.</param>
    /// <returns>The device that will be used.</returns>
    public delegate uint DeviceChooser(uint devices);

    /// <summary>
    /// Defines the type of the <see cref="DAC"/>.
    /// </summary>
    public enum ControllerType
    {
        /// <summary>
        /// Ether Dream
        /// </summary>
        EtherDream = 1,

        /// <summary>
        /// JM-Laser EasyLase USB
        /// </summary>
        EasyLase = 2,

        /// <summary>
        /// Riya USB
        /// </summary>
        RiyaUSB = 3,

        /// <summary>
        /// Pangolin QM2000 and QM2000.NET
        /// </summary>
        QM2000 = 4,

        /// <summary>
        /// Showtacle
        /// </summary>
        ShowTacle = 5,

        /// <summary>
        /// Lumax
        /// </summary>
        Lumax = 6,

        /// <summary>
        /// JM-Laser Netlase and EasyLase USB II
        /// </summary>
        Netlase = 7,

        /// <summary>
        /// Medialas
        /// </summary>
        Medialas = 8,

        /// <summary>
        /// HE-Laser
        /// </summary>
        LDS = 9,

        /// <summary>
        /// Open Laser Show DAC
        /// </summary>
        OpenLaserShowDAC = 10
    }

    /// <summary>
    /// Defines a mask of different vendors to look for.
    /// </summary>
    [Flags]
    public enum ControllerTypes
    {
        /// <summary>
        /// Includes all vendors of a <see cref="DAC"/> that is supported by this software.
        /// </summary>
        All = ~0x0,

        /// <summary>
        /// Ether Dream
        /// </summary>
        EZAUDDAC = 1,

        /// <summary>
        /// JM-Laser EasyLase USB
        /// </summary>
        EasyLase = 2,

        /// <summary>
        /// Riya USB
        /// </summary>
        RiyaUSB = 4,

        /// <summary>
        /// Pangolin QM2000 and QM2000.NET
        /// </summary>
        QM2000 = 8,

        /// <summary>
        /// Moncha and Moncha.NET
        /// </summary>
        Moncha = 16,

        /// <summary>
        /// Fiesta and Fiesta.NET by Moncha
        /// </summary>
        Fiesta = 32,

        /// <summary>
        /// Lumax
        /// </summary>
        Lumax = 64,

        /// <summary>
        /// JM-Laser Netlase and EasyLase USB II
        /// </summary>
        Netlase = 128,

        /// <summary>
        /// Medialas
        /// </summary>
        Medialas = 256,

        /// <summary>
        /// HE-Laser
        /// </summary>
        LDS = 512,

        /// <summary>
        /// ???
        /// </summary>
        OLSD = 10
    }
}
