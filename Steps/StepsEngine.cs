/*
 * The MIT License (MIT)
 * Copyright (c) 2015 Microsoft
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Sensors;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Lumia.Sense;

using BackgroundTasks.Converters;

namespace Steps
{
    /// <summary>
    /// Platform agnostic Steps Engine interface
    /// This interface is implementd by OSStepsEngine and LumiaStepsEngine.
    /// </summary>
    public interface IStepsEngine
    {
        /// <summary>
        /// Activates the step counter
        /// </summary>
        /// <returns>Asynchronous task</returns>
        Task ActivateAsync();

        /// <summary>
        /// Deactivates the step counter
        /// </summary>
        /// <returns>Asynchronous task</returns>
        Task DeactivateAsync();

        /// <summary>
        /// Returns steps for given day at given resolution
        /// </summary>
        /// <param name="day">Day to fetch data for</param>
        /// <param name="resolution">Resolution in minutes. Minimum resolution is five minutes.</param>
        /// <returns>List of steps counts for the given day at given resolution.</returns>
        Task<List<KeyValuePair<TimeSpan, uint>>> GetStepsCountsForDay(DateTime day, uint resolution);

        /// <summary>
        /// Returns step count for given day
        /// </summary>
        /// <returns>Step count for given day</returns>
        Task<StepCountData> GetTotalStepCountAsync(DateTime day);
    }

    /// <summary>
    /// Factory class for instantiating Step Engines.
    /// If a pedometer is surfaced through Windows.Devices.Sensors, the factory creates an instance of OSStepsEngine.
    /// Otherwise, the factory creates an instance of LumiaStepsEngine.
    /// </summary>
    public static class StepsEngineFactory
    {
        /// <summary>
        /// Static method to get the default steps engine present in the system.
        /// </summary>
        public static async Task<IStepsEngine> GetDefaultAsync()
        {
            IStepsEngine stepsEngine = null;

            try
            {
                // Check if there is a pedometer in the system.
                // This also checks if the user has disabled motion data from Privacy settings
                Pedometer pedometer = await Pedometer.GetDefaultAsync();

                // If there is one then create OSStepsEngine.
                if (pedometer != null)
                {
                    stepsEngine = new OSStepsEngine();
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                // If there is a pedometer but the user has disabled motion data
                // then check if the user wants to open settngs and enable motion data.
                MessageDialog dialog = new MessageDialog("Motion access has been disabled in system settings. Do you want to open settings now?", "Information");
                dialog.Commands.Add(new UICommand("Yes", async cmd => await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-motion"))));
                dialog.Commands.Add(new UICommand("No"));
                await dialog.ShowAsync();
                new System.Threading.ManualResetEvent(false).WaitOne(500);
                return null;
            }

            // No Windows.Devices.Sensors.Pedometer exists, fall back to using Lumia Sensor Core.
            if (stepsEngine == null)
            {
                // Check if all the required settings have been configured correctly
                await LumiaStepsEngine.ValidateSettingsAsync();

                stepsEngine = new LumiaStepsEngine();
            }
            return stepsEngine;
        }
    }

    /// <summary>
    /// Steps engine that wraps the Windows.Devices.Sensors.Pedometer APIs
    /// </summary>
    public class OSStepsEngine : IStepsEngine
    {
        /// <summary>
        /// Constructor that receives a pedometer instance
        /// </summary>
        public OSStepsEngine()
        {
        }

        /// <summary>
        /// Activates the step counter when app goes to foreground
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public Task ActivateAsync()
        {
            // This is where you can subscribe to Pedometer ReadingChanged events if needed.
            // Do nothing here because we are not using events.
            return Task.FromResult(false);
        }

        /// <summary>
        /// Deactivates the step counter when app goes to background
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public Task DeactivateAsync()
        {
            // This is where you can unsubscribe from Pedometer ReadingChanged events if needed.
            // Do nothing here because we are not using events.
            return Task.FromResult(false);
        }

        /// <summary>
        /// Returns steps for given day at given resolution
        /// </summary>
        /// <param name="day">Day to fetch data for</param>
        /// <param name="resolution">Resolution in minutes. Minimum resolution is five minutes.</param>
        /// <returns>List of steps counts for the given day at given resolution.</returns>
        public async Task<List<KeyValuePair<TimeSpan, uint>>> GetStepsCountsForDay(DateTime day, uint resolution)
        {
            List<KeyValuePair<TimeSpan, uint>> steps = new List<KeyValuePair<TimeSpan, uint>>();
            uint numIntervals = (((24 * 60) / resolution) + 1);
            if (day.Date.Equals(DateTime.Today))
            {
                numIntervals = (uint)((DateTime.Now - DateTime.Today).TotalMinutes / resolution) + 1;
            }
 
            uint totalSteps = 0;
            for (uint i = 0; i < numIntervals; i++)
            {
                TimeSpan ts = TimeSpan.FromMinutes(i * resolution);
                DateTime startTime = day.Date + ts;
                if (startTime < DateTime.Now)
                {
                    // Get history from startTime to the resolution duration
                    var readings = await Pedometer.GetSystemHistoryAsync(startTime, TimeSpan.FromMinutes(resolution));

                    // Compute the deltas
                    var stepsDelta = StepCountData.FromPedometerReadings(readings);

                    // Add to the total count
                    totalSteps += stepsDelta.TotalCount;
                    steps.Add(new KeyValuePair<TimeSpan, uint>(ts, totalSteps));
                }
                else
                {
                    break;
                }
            }
            return steps;
        }

        /// <summary>
        /// Returns step count for given day
        /// </summary>
        /// <returns>Step count for given day</returns>
        public async Task<StepCountData> GetTotalStepCountAsync(DateTime day)
        {
            // Get history from 1 day
            var readings = await Pedometer.GetSystemHistoryAsync(day.Date, TimeSpan.FromDays(1));

            return StepCountData.FromPedometerReadings(readings);
        }
    }

    /// <summary>
    /// Steps engine that wraps the Lumia SensorCore StepCounter APIs
    /// </summary>
    public class LumiaStepsEngine : IStepsEngine
    {
        #region Private members
        /// <summary>
        /// Step counter instance
        /// </summary>
        private IStepCounter _stepCounter;

        /// <summary>
        /// Is step counter currently active?
        /// </summary>
        private bool _sensorActive = false;

        /// <summary>
        /// Constructs a new ResourceLoader object.
        /// </summary>
        static protected readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public LumiaStepsEngine()
        {
        }

        /// <summary>
        /// Makes sure necessary settings are enabled in order to use SensorCore
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public static async Task ValidateSettingsAsync()
        {
            if (!await StepCounter.IsSupportedAsync())
            {
                MessageDialog dlg = new MessageDialog(_resourceLoader.GetString("FeatureNotSupported/Message"), _resourceLoader.GetString("FeatureNotSupported/Title"));
                await dlg.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                // Starting from version 2 of Motion data settings Step counter and Acitivity monitor are always available. In earlier versions system
                // location setting and Motion data had to be enabled.
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                if (settings.Version < 2)
                {
                    if (!settings.LocationEnabled)
                    {
                        MessageDialog dlg = new MessageDialog("In order to count steps you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                    if (!settings.PlacesVisited)
                    {
                        MessageDialog dlg = new MessageDialog("In order to count steps you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                }
            }
        }

        /// <summary>
        /// SensorCore needs to be deactivated when app goes to background
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public async Task DeactivateAsync()
        {
            _sensorActive = false;
            if (_stepCounter != null) await _stepCounter.DeactivateAsync();
        }

        /// <summary>
        /// SensorCore needs to be activated when app comes back to foreground
        /// </summary>
        public async Task ActivateAsync()
        {
            if (_sensorActive) return;
            if (_stepCounter != null)
            {
                await _stepCounter.ActivateAsync();
            }
            else
            {
                await InitializeAsync();
            }
            _sensorActive = true;
        }

        /// <summary>
        /// Returns steps for given day at given resolution
        /// </summary>
        /// <param name="day">Day to fetch data for</param>
        /// <param name="resolution">Resolution in minutes. Minimum resolution is five minutes.</param>
        /// <returns>List of steps counts for the given day at given resolution.</returns>
        public async Task<List<KeyValuePair<TimeSpan, uint>>> GetStepsCountsForDay(DateTime day, uint resolution)
        {
            List<KeyValuePair<TimeSpan, uint>> steps = new List<KeyValuePair<TimeSpan, uint>>();
            uint totalSteps = 0;
            uint numIntervals = (((24 * 60) / resolution) + 1);
            if (day.Date.Equals(DateTime.Today))
            {
                numIntervals = (uint)((DateTime.Now - DateTime.Today).TotalMinutes / resolution) + 1;
            }
            for (int i = 0; i < numIntervals; i++)
            {
                TimeSpan ts = TimeSpan.FromMinutes(i * resolution);
                DateTime startTime = day.Date + ts;
                if (startTime < DateTime.Now)
                {
                    try
                    {
                        var stepCount = await _stepCounter.GetStepCountForRangeAsync(startTime, TimeSpan.FromMinutes(resolution));
                        if (stepCount != null)
                        {
                            totalSteps += (stepCount.WalkingStepCount + stepCount.RunningStepCount);
                            steps.Add(new KeyValuePair<TimeSpan, uint>(ts, totalSteps));
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    break;
                }
            }
            return steps;
        }

        /// <summary>
        /// Returns step count for given day
        /// </summary>
        /// <returns>Step count for given day</returns>
        public async Task<StepCountData> GetTotalStepCountAsync(DateTime day)
        {
            if (_stepCounter != null && _sensorActive)
            {
                StepCount steps = await _stepCounter.GetStepCountForRangeAsync(day.Date, TimeSpan.FromDays(1));
                return StepCountData.FromLumiaStepCount(steps);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Initializes simulator if example runs on emulator otherwise initializes StepCounter
        /// </summary>
        private async Task InitializeAsync()
        {
            // Using this method to detect if the application runs in the emulator or on a real device. Later the *Simulator API is used to read fake sense data on emulator. 
            // In production code you do not need this and in fact you should ensure that you do not include the Lumia.Sense.Testing reference in your project.
            EasClientDeviceInformation x = new EasClientDeviceInformation();
            if (x.SystemProductName.StartsWith("Virtual"))
            {
                //await InitializeSimulatorAsync();
            }
            else
            {
                await InitializeSensorAsync();
            }
        }

        /// <summary>
        /// Initializes the step counter
        /// </summary>
        private async Task InitializeSensorAsync()
        {
            if (_stepCounter == null)
            {
                await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounter.GetDefaultAsync(); });
            }
            else
            {
                await _stepCounter.ActivateAsync();
            }
            _sensorActive = true;
        }

        /// <summary>
        /// Initializes StepCounterSimulator (requires Lumia.Sense.Testing)
        /// </summary>
        //public async Task InitializeSimulatorAsync()
        //{
        //    var obj = await SenseRecording.LoadFromFileAsync("Simulations\\short recording.txt");
        //    if (!await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounterSimulator.GetDefaultAsync(obj, DateTime.Now - TimeSpan.FromHours(12)); }))
        //    {
        //        Application.Current.Exit();
        //    }
        //    _sensorActive = true;
        //}

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">Action for which the SensorCore will be activated.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSensorCoreApiAsync(Func<Task> action)
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                failure = e;
            }
            if (failure != null)
            {
                MessageDialog dlg = null;
                switch (SenseHelper.GetSenseError(failure.HResult))
                {
                    case SenseError.LocationDisabled:
                        {
                            dlg = new MessageDialog("Location has been disabled. Do you want to open Location settings now?", "Information");
                            dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                            dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { /* do nothing */ })));
                            await dlg.ShowAsync();
                            new System.Threading.ManualResetEvent(false).WaitOne(500);
                            return false;
                        }
                    case SenseError.SenseDisabled:
                        {
                            dlg = new MessageDialog("Motion data has been disabled. Do you want to open Motion data settings now?", "Information");
                            dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                            dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { /* do nothing */ })));
                            await dlg.ShowAsync();
                            return false;
                        }
                    default:
                        {
                            dlg = new MessageDialog("Failure: " + SenseHelper.GetSenseError(failure.HResult), "");
                            await dlg.ShowAsync();
                            return false;
                        }
                }
            }
            else
            {
                return true;
            }
        }
    }
}
