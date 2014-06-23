/*
 * Copyright (c) 2014 Microsoft Mobile. All rights reserved.
 * See the license text file provided with this project for more information.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Steps.Resources;
using Lumia.Sense;
using Lumia.Sense.Testing;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Data;
using System.Globalization;

namespace Steps
{
    
    public partial class MainPage : PhoneApplicationPage
    {
        private MainModel _mainModel;
        private IStepCounter _stepCounter;
        
        private uint _firstWalkingSteps = 0;
        private uint _firstRunningSteps = 0;

        private DispatcherTimer _pollTimer;

        const int _resolutionInMinutes = 15;  // Resolution of graph is 15 min. Can be 5,10,15,20...60
        const int _ArrayMaxSize = 1440 / _resolutionInMinutes;

        private bool _usingSimulator = false;

        // Constructor
        public MainPage()
        {
               
            InitializeComponent();

            _mainModel = new MainModel();
            LayoutRoot.DataContext = _mainModel;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_stepCounter == null)
            {
                await InitializeAsync();
            }
            await _stepCounter.ActivateAsync();
            await UpdateModelAsync(); 
        }
        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
            }
            await _stepCounter.DeactivateAsync();
        }

        private async Task UpdateModelAsync()
        {
            List<uint> steps = new List<uint>();

            await GetListAsync(steps);

            _mainModel.setParameters(StepGraph.ActualWidth, StepGraph.ActualHeight, steps, _resolutionInMinutes);

            await UpdateStepsAsync();

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(5);
            _pollTimer.Tick += _pollTimer_Tick;

            _pollTimer.Start();

        }

        async void _pollTimer_Tick(object sender, EventArgs e)
        {
            await UpdateStepsAsync();
        }

        private async Task UpdateStepsAsync()
        {
            if (_stepCounter != null)
            {
                StepCounterReading reading = null;

                bool res = await CallSensorCoreApiAsync(async () => { reading = await _stepCounter.GetCurrentReadingAsync(); });

                if (reading != null && res)
                {
                    _mainModel.StepsToday = reading.WalkingStepCount + reading.RunningStepCount - _firstRunningSteps - _firstWalkingSteps;
                    _mainModel.WalkingSteps = reading.WalkingStepCount - _firstWalkingSteps;
                    _mainModel.RunningSteps = reading.RunningStepCount - _firstRunningSteps;
                }
            }

        }


        private async Task InitializeAsync(bool useSimulator = false)
        {
            if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator || useSimulator)
            {
                await InitializeSimulatorAsync();
                _usingSimulator = true;
            }
            else
            {
                await InitializeSensorAsync();
                _usingSimulator = false;
            }
        }

        private async Task InitializeSimulatorAsync()
        {
            var obj = await SenseRecording.LoadFromFileAsync("Simulations\\short recording.txt");

            bool res = await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounterSimulator.GetDefaultAsync(obj, DateTime.Now - TimeSpan.FromHours(12)); });

            if (!res)
                Application.Current.Terminate();
        }

        private async Task InitializeSensorAsync()
        {

            if (!await StepCounter.IsSupportedAsync())
            {
                MessageBox.Show(
                    "Your device doesn't support Motion Data. Application will be closed",
                    "Information", MessageBoxButton.OK);
                Application.Current.Terminate();
            }

            bool res = await CallSensorCoreApiAsync(async () => { _stepCounter = await StepCounter.GetDefaultAsync(); });

            if (!res)
                Application.Current.Terminate();

        }

        /// <summary>
        /// Creates an array of today's step count.  
        /// </summary>
        private async Task<bool> GetListAsync( List<uint> steps)
        {

            for (int i = 0; i < _ArrayMaxSize; i++)
                steps.Add(0);

            var results = await _stepCounter.GetStepCountHistoryAsync(DateTime.Now.Date, DateTime.Now-DateTime.Now.Date);

            bool first = true;
            
            int currentStep = 0;

            foreach (StepCounterReading reading in results)
            {
                if (first)
                {
                    first = false;
                    _firstWalkingSteps = reading.WalkingStepCount;
                    _firstRunningSteps = reading.RunningStepCount;

                }
                else if (reading.Timestamp.DateTime.Minute % _resolutionInMinutes == 0)
                {
                    currentStep = (reading.Timestamp.DateTime.Hour * 60 + reading.Timestamp.DateTime.Minute) / _resolutionInMinutes;
                    steps[currentStep] = reading.WalkingStepCount + reading.RunningStepCount - _firstWalkingSteps - _firstRunningSteps;
                }

            }

            //If there are gaps in the array we fill them e.g. 13,15,0,20 will be 13,15,15,20
            for (int i = 0; i < currentStep - 1; i++)
            {
                if (steps[i] > steps[i + 1])
                    steps[i+1] = steps[i];
            }

            //Removes empty items from the end of array
            if (currentStep < _ArrayMaxSize)
                steps.RemoveRange(currentStep + 1, _ArrayMaxSize - 1 - currentStep);

            return true;
            
            
        }
        
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

        private void StepGraph_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _mainModel.ChangeMax();
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private async void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {

            (sender as ApplicationBarMenuItem).Text = _usingSimulator ? "Use Simulation" : "Use Sensor";

            await InitializeAsync(!_usingSimulator);

            await UpdateModelAsync();
        }



        
        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}