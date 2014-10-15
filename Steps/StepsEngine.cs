using Lumia.Sense;
using Lumia.Sense.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Steps
{
    public class StepsEngine
    {

        /// <summary>
        /// Access to the main model of the app
        /// </summary>
        /// <returns>The MainModel of the app</returns>
        public MainModel MainModel { get; private set; }

        
        private IStepCounter _stepCounter;

        private DispatcherTimer _pollTimer;

        /// <summary>
        /// constructor  
        /// </summary>
        public StepsEngine()
        {
            MainModel = new MainModel();
        }

        /// <summary>
        /// SensorCore needs to be deactivated when app goes to background  
        /// </summary>
        public async Task DeactivateAsync()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer = null;
            }

            if (_stepCounter != null)
                await _stepCounter.DeactivateAsync();

        }

        /// <summary>
        /// SensorCore needs to be activated when app comes back to foreground  
        /// </summary>
        public async Task ActivateAsync()
        {
            if (_stepCounter != null)
            {
                await _stepCounter.ActivateAsync();
            }
            else
            {
                await InitializeAsync();
            }
            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(5);
            _pollTimer.Tick += PollTimerTick;
            _pollTimer.Start();
        }

        /// <summary>
        /// Updates both step counters and graph  
        /// </summary>
        public async Task UpdateModelAsync()
        {
            await UpdateGraphAsync();
            await UpdateStepCountersAsync();
        }

        /// <summary>
        /// +1 one day back, -1 one day forward.  
        /// </summary>
        public async Task ChangeDayAsync(int day)
        {
            if (MainModel.day + day > 7 || MainModel.day + day < 0)
                return;

            MainModel.day += day;
            await UpdateModelAsync();
        }

        /// <summary>
        /// Creates an array of user's steps for selected day and passes it to the model 
        /// </summary>
        public async Task<bool> UpdateGraphAsync()
        {
            uint firstWalkingSteps = 0;
            uint firstRunningSteps = 0;

            List<uint> steps = new List<uint>();

            for (int i = 0; i < MainModel._ArrayMaxSize; i++)
                steps.Add(0);

            IList<StepCounterReading> results = null;

            if (MainModel.day == 0)
                results = await _stepCounter.GetStepCountHistoryAsync(DateTime.Now.Date, DateTime.Now - DateTime.Now.Date);
            else
                results = await _stepCounter.GetStepCountHistoryAsync(DateTime.Now.Date - TimeSpan.FromDays(MainModel.day), TimeSpan.FromDays(1));


            //If there is no items available in the history, we pass array with full of 0 items.
            //This happens for example when Motion data has been disabled the whole day

            if (results == null || results.Count == 0)
            {
                MainModel.UpdateGraphSteps(steps);
                return true;
            }

            bool first = true;

            int currentStep = (results[0].Timestamp.Minute + results[0].Timestamp.Hour * 60) / MainModel._resolutionInMinutes;

            foreach (StepCounterReading reading in results)
            {
                if (first)
                {
                    first = false;
                    firstWalkingSteps = reading.WalkingStepCount;
                    firstRunningSteps = reading.RunningStepCount;
                }
                if (reading.Timestamp.DateTime.Minute % MainModel._resolutionInMinutes == 0)
                {
                    steps[currentStep] = reading.WalkingStepCount + reading.RunningStepCount - firstWalkingSteps - firstRunningSteps;
                    currentStep++;
                }
            }

            //If there are gaps in the array we fill them e.g. 13,15,0,20 will be 13,15,15,20
            for (int i = 0; i < currentStep - 1; i++)
            {
                if (steps[i] > steps[i + 1])
                    steps[i + 1] = steps[i];
            }

            //Removes empty items from end of the array
            if (currentStep < MainModel._ArrayMaxSize)
                steps.RemoveRange(currentStep, steps.Count - currentStep);

            MainModel.UpdateGraphSteps(steps);

            return true;
        }

        /// <summary>
        /// Updates step counters for selected day.  
        /// </summary>
        public async Task UpdateStepCountersAsync()
        {
            if (_stepCounter != null)
            {
                if (MainModel.day == 0) //today's step
                {
                    StepCounterReading current = null;
                    bool res = await CallSensorCoreApiAsync(async () => { current = await _stepCounter.GetCurrentReadingAsync(); });
                    StepCounterReading beginOfDay = await FirstReadingForTodayAsync();

                    if (current != null && beginOfDay != null && res)
                    {
                        MainModel.WalkingSteps = current.WalkingStepCount - beginOfDay.WalkingStepCount;
                        MainModel.RunningSteps = current.RunningStepCount - beginOfDay.RunningStepCount;
                        MainModel.StepsToday = MainModel.WalkingSteps + MainModel.RunningSteps;
                    }
                }
                else //previous days' steps
                {
                    StepCount count = await _stepCounter.GetStepCountForRangeAsync(DateTime.Now.Date - TimeSpan.FromDays(MainModel.day), TimeSpan.FromDays(1));
                    if (count != null)
                    {
                        MainModel.StepsToday = count.RunningStepCount + count.WalkingStepCount;
                        MainModel.WalkingSteps = count.WalkingStepCount;
                        MainModel.RunningSteps = count.RunningStepCount;
                    }
                    else
                    {
                        MainModel.WalkingSteps = MainModel.StepsToday = MainModel.RunningSteps = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a total number of steps for today   
        /// </summary>
        public async Task<uint> GetStepCountAsync()
        {
            try
            {
                var steps = await  _stepCounter.GetStepCountForRangeAsync(DateTime.Now.Date, DateTime.Now - DateTime.Now.Date);
                if (steps != null)
                    return steps.WalkingStepCount + steps.RunningStepCount;
            }
            catch (Exception e)
            {
  
            }

            return 0;            
        }

        /// <summary>
        /// Updates step counters every 5 seconds  
        /// </summary>
        private async void PollTimerTick(object sender, EventArgs e)
        {
            await UpdateStepCountersAsync();
        }

        /// <summary>
        /// Initializes simulator if example runs on emulator otherwise initializes StepCounter   
        /// </summary>
        public async Task InitializeAsync()
        {
            if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
            {
                await InitializeSimulatorAsync();
            }
            else
            {
                await InitializeSensorAsync();
            }

            await UpdateModelAsync();
        }

        /// <summary>
        /// Initializes StepCounter  
        /// </summary>
        private async Task InitializeSensorAsync()
        {
            if (!await StepCounter.IsSupportedAsync())
            {
                MessageBox.Show(
                    "Your device doesn't support Motion Data. Application will be closed",
                    "Information", MessageBoxButton.OK);
                Application.Current.Terminate();
            }

            if (_stepCounter == null)
            {
                await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounter.GetDefaultAsync(); });

            }
            else
            {
                await _stepCounter.ActivateAsync();
            }
        }

        /// <summary>
        /// Initializes StepCounterSimulator
        /// </summary>
        public async Task InitializeSimulatorAsync()
        {
            var obj = await SenseRecording.LoadFromFileAsync("Simulations\\short recording.txt");

            bool res = await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounterSimulator.GetDefaultAsync(obj, DateTime.Now - TimeSpan.FromHours(12)); });

            if (!res)
                Application.Current.Terminate();
        }

        /// <summary>
        /// Helper function that fetches the first existing item for today
        /// </summary>
        private async Task<StepCounterReading> FirstReadingForTodayAsync()
        {
            //We look at the first value for today.
            var results = await _stepCounter.GetStepCountHistoryAsync(DateTime.Now.Date - TimeSpan.FromDays(MainModel.day), TimeSpan.FromDays(1));

            if (results != null)
                return results[0];
            else
                return null;

        }

        /// <summary>
        /// Helper function that catches SensorCore exceptions
        /// </summary>
        private async Task<bool> CallSensorCoreApiAsync(Func<Task> action, bool secondTry = false)
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

                switch (SenseHelper.GetSenseError(failure.HResult))
                {
                    case SenseError.LocationDisabled:
                        MessageBoxResult res = MessageBox.Show(
                            "Location has been disabled. Do you want to open Location settings now?",
                            "Information",
                            MessageBoxButton.OKCancel
                            );
                        if (res == MessageBoxResult.OK)
                        {
                            await SenseHelper.LaunchLocationSettingsAsync();
                        }

                        return false;

                    case SenseError.SenseDisabled:

                        MessageBoxResult res2 = MessageBox.Show(
                            "Motion data has been disabled. Do you want to open Motion data settings now?",
                            "Information",
                            MessageBoxButton.OKCancel
                            );

                        if (res2 == MessageBoxResult.OK)
                        {
                            await SenseHelper.LaunchSenseSettingsAsync();
                        }

                        return false;

                    case SenseError.SensorDeactivated:
                        //If SensorCore is disabled, try to activate it and call again.
                        if (secondTry == false)
                        {
                            try
                            {
                                await _stepCounter.ActivateAsync();
                            }
                            catch (Exception e)
                            {
                                return false;
                            }
                            return await CallSensorCoreApiAsync(action,true);

                        }

                        return false;


                    default:
                        MessageBoxResult res3 = MessageBox.Show(
                              "Error:" + SenseHelper.GetSenseError(failure.HResult),
                              "Information",
                              MessageBoxButton.OK);

                        return false;
                }
            }
            else
            {
                return true;
            }
        }
    }

    public sealed class Helper
    {
        public static uint GetMeter(uint steps)
        {
            if (steps < 600) { return 0; }
            else if (steps < 1400) { return 1; }
            else if (steps < 2600) { return 2; }
            else if (steps < 3800) { return 3; }
            else if (steps < 5000) { return 4; }
            else if (steps < 6200) { return 5; }
            else if (steps < 7400) { return 6; }
            else if (steps < 8600) { return 7; }
            else { return 8; }
        }

        public static uint GetSmallMeter(uint steps)
        {
            if (steps < 2000) { return 0; }
            else if (steps < 4300) { return 1; }
            else if (steps < 7600) { return 2; }
            else if (steps < 9000) { return 3; }
            else { return 3; }
        }
    }

}
