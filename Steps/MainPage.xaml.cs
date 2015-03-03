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
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Lumia.Sense;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.StartScreen;
using Windows.UI;

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace Steps
{
    /// <summary>
    /// Main page of the application
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// Tile ID
        /// </summary>
        private const string _TileID = "SecondaryTile.Steps";

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            LayoutRoot.DataContext = App.Engine.MainModel;
            StepGraph.Loaded += StepGraph_Loaded;
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await App.Engine.DeactivateAsync();
        }

        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitCore();
            await App.Engine.ActivateAsync();
            UpdateMenuAndAppBarIcons();
        }
        #endregion

        private async Task InitCore()
        {
            if (!await StepCounter.IsSupportedAsync())
            {
                MessageBoxResult dlg = MessageBox.Show("Unfortunately this device does not support step counting");
            }
            else
            {
                // MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                // Starting from version 2 of Motion data settings Step counter and Acitivity monitor are always available. In earlier versions system
                // location setting and Motion data had to be enabled.
                uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                if (apiSet > 2)
                {
                    if (!settings.LocationEnabled)
                    {
                        MessageBoxResult dlg = MessageBox.Show("In order to count steps you need to enable location in system settings. Do you want to open settings now?", "Information", MessageBoxButton.OKCancel);
                        if (dlg == MessageBoxResult.OK)
                            await SenseHelper.LaunchLocationSettingsAsync();
                    }
                    if (!settings.PlacesVisited)
                    {
                        MessageBoxResult dlg = new MessageBoxResult();
                        if (settings.Version < 2)
                        {
                            dlg = MessageBox.Show("In order to count steps you need to enable Motion data collection in Motion data settings. Do you want to open settings now?", "Information", MessageBoxButton.OKCancel);
                        }
                        else
                        {
                            dlg = MessageBox.Show("In order to collect and view visited places you need to enable Places visited in Motion data settings. Do you want to open settings now? if no, application will exit", "Information", MessageBoxButton.OKCancel);
                        }
                        if (dlg == MessageBoxResult.OK)
                            await SenseHelper.LaunchSenseSettingsAsync();
                        else
                            Application.Current.Terminate();
                    }
                }
            }
        }

        /// <summary>
        /// Executes when the Step graph finished loading.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void StepGraph_Loaded(object sender, RoutedEventArgs e)
        {
            App.Engine.MainModel.SetParameters(StepGraph.ActualWidth, StepGraph.ActualHeight);
        }

        /// <summary>
        /// User taps on step graph. This will change max value for the graph.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void StepGraph_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.Engine.MainModel.ChangeMax();
        }

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private void Click_NavigateToAbout(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Removes background agent.
        /// </summary>
        /// <param name="taskName">Name of task to be removed.</param>
        /// <returns>A task.</returns>
        private async static Task RemoveBackgroundTaskAsync(String taskName)
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result != BackgroundAccessStatus.Denied)
            {
                // Remove previous registration
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }
            }
        }

        /// <summary>
        /// Removes old background agent if exists and adds new background task.
        /// </summary>
        /// <param name="trigger">Parameter for triggered events.</param>
        /// <param name="taskName">Name of task to be removed.</param>
        /// <param name="taskEntryPoint">Entry point of the task to be removed.</param>
        /// <returns>A task.</returns>
        private async static Task RegisterBackgroundTaskAsync(IBackgroundTrigger trigger, String taskName, String taskEntryPoint)
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result != BackgroundAccessStatus.Denied)
            {
                // Remove previous registration
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }
                // Register new trigger
                BackgroundTaskBuilder myTaskBuilder = new BackgroundTaskBuilder();
                myTaskBuilder.SetTrigger(trigger);
                myTaskBuilder.TaskEntryPoint = taskEntryPoint;
                myTaskBuilder.Name = taskName;
                BackgroundTaskRegistration myTask = myTaskBuilder.Register();
            }
        }

        /// <summary>
        /// Creates or removes a secondary tile.
        /// </summary>
        /// <param name="removeTile">Variable to determine if a tile exists and needs to be removed.</param>
        /// <returns>A task.</returns>
        private async Task CreateOrRemoveTileAsync(bool removeTile)
        {
            if (!removeTile)
            {
                uint stepCount = await App.Engine.GetStepCountAsync();
                uint meter = Helper.GetMeter(stepCount);
                uint meterSmall = Helper.GetSmallMeter(stepCount);
                try
                {
                    var secondaryTile = new SecondaryTile(_TileID, "Steps", "/MainPage.xaml", new Uri("ms-appx:///Assets/Tiles/square" + meterSmall + ".png", UriKind.Absolute), TileSize.Square150x150);
                    secondaryTile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Tiles/small_square" + meterSmall + ".png", UriKind.Absolute);
                    secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = false;
                    secondaryTile.VisualElements.ShowNameOnWide310x150Logo = false;
                    secondaryTile.VisualElements.BackgroundColor = Color.FromArgb(255, 0, 138, 0);
                    secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Tiles/wide" + meter + ".png", UriKind.Absolute);
                    secondaryTile.RoamingEnabled = false;
                    await secondaryTile.RequestCreateAsync();
                }
                catch (Exception exp)
                {
                }
                return;
            }
            else
            {
                SecondaryTile secondaryTile = new SecondaryTile(_TileID);
                await secondaryTile.RequestDeleteAsync();
                UpdateMenuAndAppBarIcons();
            }
        }

        /// <summary>
        /// Updates menu and app bar icons.
        /// </summary>
        private void UpdateMenuAndAppBarIcons()
        {
            // Show unpin or pin button
            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            if (!SecondaryTile.Exists(_TileID))
            {
                btn.IconUri = new Uri("Assets/Images/pin-48px.png", UriKind.Relative);
                btn.Text = "Pin";
            }
            else
            {
                btn.IconUri = new Uri("Assets/Images/unpin-48px.png", UriKind.Relative);
                btn.Text = "Unpin";
            }
            // Disable the back button if we show 7th day in the past
            ApplicationBarIconButton back = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            if (App.Engine.MainModel._day == 7)
                back.IsEnabled = false;
            else
                back.IsEnabled = true;
            // Disable next button if we show today's steps
            ApplicationBarIconButton next = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            if (App.Engine.MainModel._day == 0)
                next.IsEnabled = false;
            else
                next.IsEnabled = true;
        }

        /// <summary>
        /// Registers/Unregisters tile.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private async void ApplicationBarRegisterUnRegisterTile(object sender, EventArgs e)
        {
            // Register background task that updates live tile
            bool removeTile = SecondaryTile.Exists(_TileID);
            if (removeTile)
            {
                await RemoveBackgroundTaskAsync("StepTriggered");
            }
            else
            {
                if (Microsoft.Devices.Environment.DeviceType != Microsoft.Devices.DeviceType.Emulator)
                {
                    ApiSupportedCapabilities caps = await SenseHelper.GetSupportedCapabilitiesAsync();
                    // Use StepCounterUpdate to trigger live tile update if it is supported. Otherwise we use time trigger
                    if (caps.StepCounterTrigger)
                    {
                        var myTrigger = new DeviceManufacturerNotificationTrigger(SenseTrigger.StepCounterUpdate, false);
                        await RegisterBackgroundTaskAsync(myTrigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
                    }
                    else
                    {
                        BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                        IBackgroundTrigger trigger = new TimeTrigger(15, false);
                        await RegisterBackgroundTaskAsync(trigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
                    }
                }
                else
                {
                    // On emulator we use always TimeTrigger
                    BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                    IBackgroundTrigger trigger = new TimeTrigger(15, false);
                    await RegisterBackgroundTaskAsync(trigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
                }
            }
            await CreateOrRemoveTileAsync(removeTile);
            return;
        }

        /// <summary>
        /// Select next day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private async void ApplicationBarIconButton_Click_Next(object sender, EventArgs e)
        {
            await App.Engine.ChangeDayAsync(-1);
            UpdateMenuAndAppBarIcons();
        }

        /// <summary>
        /// Select previous day.
        /// </summary>
        /// <param name="sender">The control that the action is for.</param>
        /// <param name="e">Parameter that contains the event data.</param>
        private async void ApplicationBarIconButton_Click_Back(object sender, EventArgs e)
        {
            await App.Engine.ChangeDayAsync(1);
            UpdateMenuAndAppBarIcons();
        }
    }
}