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
using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Steps
{
    /// <summary>
    /// Steps engine for the application
    /// </summary>
    public class StepsEngine
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
        #endregion

        /// <summary>
        /// constructor  
        /// </summary>
        public StepsEngine()
        {
        }

        /// <summary>
        /// Makes sure necessary settings are enabled in order to use SensorCore
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public async Task ValidateSettingsAsync()
        {
            if( !await StepCounter.IsSupportedAsync() )
            {
                //MessageBoxResult dlg = MessageBox.Show( "Unfortunately this device does not support step counting" );
                //Application.Current.Terminate();
            }
            else
            {
                // Starting from version 2 of Motion data settings Step counter and Acitivity monitor are always available. In earlier versions system
                // location setting and Motion data had to be enabled.
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                if( settings.Version < 2 )
                {
                    if( !settings.LocationEnabled )
                    {
                        //MessageBoxResult dlg = MessageBox.Show( "In order to count steps you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information", MessageBoxButton.OKCancel );
                        //if( dlg == MessageBoxResult.OK )
                        //{
                        //    await SenseHelper.LaunchLocationSettingsAsync();
                        //}
                        //else
                        //{
                        //    Application.Current.Terminate();
                        //}
                    }
                    if( !settings.PlacesVisited )
                    {
                        //MessageBoxResult rc = MessageBox.Show( "In order to count steps you need to enable Motion data collection in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information", MessageBoxButton.OKCancel );
                        //if( rc == MessageBoxResult.OK )
                        //{
                        //    await SenseHelper.LaunchSenseSettingsAsync();
                        //}
                        //else
                        //{
                        //    Application.Current.Terminate();
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// SensorCore needs to be deactivated when app goes to background  
        /// </summary>
        public async Task DeactivateAsync()
        {
            _sensorActive = false;
            if( _stepCounter != null ) await _stepCounter.DeactivateAsync();
        }

        /// <summary>
        /// SensorCore needs to be activated when app comes back to foreground  
        /// </summary>
        public async Task ActivateAsync()
        {
            if( _sensorActive ) return;
            if( _stepCounter != null )
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
        public async Task<List<KeyValuePair<TimeSpan, uint>>> GetStepsCountsForDay( DateTime day, uint resolution )
        {
            List<KeyValuePair<TimeSpan, uint>> steps = new List<KeyValuePair<TimeSpan, uint>>();
            uint totalSteps = 0;
            uint numIntervals = ( ( ( 24 * 60 ) / resolution ) + 1 );
            if( day.Date.Equals( DateTime.Today ) )
            {
                numIntervals = (uint)( ( DateTime.Now - DateTime.Today ).TotalMinutes / resolution ) + 1;
            }
            for( int i = 0; i < numIntervals; i++ )
            {
                TimeSpan ts = TimeSpan.FromMinutes( i * resolution );
                DateTime startTime = day.Date + ts;
                if( startTime < DateTime.Now )
                {
                    try
                    {
                        var stepCount = await _stepCounter.GetStepCountForRangeAsync( startTime, TimeSpan.FromMinutes( resolution ) );
                        if( stepCount != null )
                        {
                            totalSteps += ( stepCount.WalkingStepCount + stepCount.RunningStepCount );
                            steps.Add( new KeyValuePair<TimeSpan, uint>( ts, totalSteps ) );
                        }
                    }
                    catch( Exception )
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
        public async Task<StepCount> GetTotalStepCountAsync( DateTime day )
        {
            if( _stepCounter != null && _sensorActive )
            {
                return await _stepCounter.GetStepCountForRangeAsync( day.Date, TimeSpan.FromDays( 1 ) );
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Initializes simulator if example runs on emulator otherwise initializes StepCounter   
        /// </summary>
        public async Task InitializeAsync()
        {
            //if( Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator )
            //{
            //    await InitializeSimulatorAsync();
            //}
            //else
            //{
            //    await InitializeSensorAsync();
            //}
        }

        /// <summary>
        /// Initializes StepCounter  
        /// </summary>
        private async Task InitializeSensorAsync()
        {
            if( _stepCounter == null )
            {
                await CallSensorCoreApiAsync( async () => { _stepCounter = await StepCounter.GetDefaultAsync(); } );
            }
            else
            {
                await _stepCounter.ActivateAsync();
            }
            _sensorActive = true;
        }

        /// <summary>
        /// Initializes StepCounterSimulator.
        /// </summary>
        public async Task InitializeSimulatorAsync()
        {
/*            var obj = await SenseRecording.LoadFromFileAsync( "Simulations\\short recording.txt" );
            if( !await CallSensorCoreApiAsync( async () => { _stepCounter = await StepCounterSimulator.GetDefaultAsync( obj, DateTime.Now - TimeSpan.FromHours( 12 ) ); } ) )
            {
                Application.Current.Terminate();
            }
            _sensorActive = true;*/
        }

        /// <summary>
        /// Performs asynchronous Sensorcore SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">Action for which the SensorCore will be activated.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSensorCoreApiAsync( Func<Task> action )
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch( Exception e )
            {
                failure = e;
            }
            if( failure != null )
            {
                switch( SenseHelper.GetSenseError( failure.HResult ) )
                {
                    case SenseError.LocationDisabled:
                    {
                        //MessageBoxResult rc = MessageBox.Show( "Location has been disabled. Do you want to open Location settings now?", "Information", MessageBoxButton.OKCancel );
                        //if( rc == MessageBoxResult.OK )
                        //{
                        //    await SenseHelper.LaunchLocationSettingsAsync();
                        //}
                        return false;
                    }
                    case SenseError.SenseDisabled:
                    {
                        //MessageBoxResult rc = MessageBox.Show( "Motion data has been disabled. Do you want to open Motion data settings now?", "Information", MessageBoxButton.OKCancel );
                        //if( rc == MessageBoxResult.OK )
                        //{
                        //    await SenseHelper.LaunchSenseSettingsAsync();
                        //}
                        return false;
                    }
                    default:
                    {
                        //MessageBox.Show( "Error: " + SenseHelper.GetSenseError( failure.HResult ), "Information", MessageBoxButton.OK );
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
