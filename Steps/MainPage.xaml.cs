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
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.StartScreen;
using Windows.UI;

namespace Steps
{

    public partial class MainPage : PhoneApplicationPage
    {
        private const string TileID = "SecondaryTile.Steps";
        

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            LayoutRoot.DataContext = App.Engine.MainModel;
            StepGraph.Loaded += StepGraph_Loaded;
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await App.Engine.DeactivateAsync();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {

            if (e.NavigationMode != NavigationMode.Reset)
            {
                await App.Engine.ActivateAsync();
            }
            UpdateMenuAndAppBarIcons();
        }

        private void StepGraph_Loaded(object sender, RoutedEventArgs e)
        {
            App.Engine.MainModel.SetParameters(StepGraph.ActualWidth, StepGraph.ActualHeight);
        }

        /// <summary>
        /// User taps on step graph. This will change max value for the graph  
        /// </summary>
        private void StepGraph_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.Engine.MainModel.ChangeMax();
        }

        private void Click_NavigateToAbout(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private async static Task RemoveBackgroundTaskAsync(String taskName)
        {
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
        }

        /// <summary>
        /// Removes old background agent if exists and adds new background task  
        /// </summary>
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
        /// Creates or removes a secondary tile  
        /// </summary>
        private async Task CreateOrRemoveTileAsync(bool removeTile)
        {
            if (!removeTile)
            {
                uint stepCount = await App.Engine.GetStepCountAsync();
                uint meter = Helper.GetMeter(stepCount);
                uint meterSmall = Helper.GetSmallMeter(stepCount);

                try
                {

                    var secondaryTile = new SecondaryTile(
                            TileID,
                            "Steps",
                            "/MainPage.xaml",
                             new Uri("ms-appx:///Assets/Tiles/square" + meterSmall + ".png", UriKind.Absolute),
                            TileSize.Square150x150);

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

                SecondaryTile secondaryTile = new SecondaryTile(TileID);
                await secondaryTile.RequestDeleteAsync();
                UpdateMenuAndAppBarIcons();
            }
        }

        private void UpdateMenuAndAppBarIcons()
        {
            //Show unpin or pin button

            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            if (!SecondaryTile.Exists(TileID))
            {
                btn.IconUri = new Uri("Assets/Images/appbar.pin.png", UriKind.Relative);
                btn.Text = "Pin";
            }
            else
            {
                btn.IconUri = new Uri("Assets/Images/appbar.pin.remove.png", UriKind.Relative);
                btn.Text = "Unpin";
            }


            //Disable the back button if we show 7th day in the past
            ApplicationBarIconButton back = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            if (App.Engine.MainModel.day == 7)
                back.IsEnabled = false;
            else
                back.IsEnabled = true;

            //Disable next button if we show today's steps
            ApplicationBarIconButton next = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            if (App.Engine.MainModel.day == 0)
                next.IsEnabled = false;
            else
                next.IsEnabled = true;

        }

        private async void ApplicationBarRegisterUnRegisterTile(object sender, EventArgs e)
        {

            //Register background task that updates live tile
            bool removeTile = SecondaryTile.Exists(TileID);

            if (removeTile)
            {
                await RemoveBackgroundTaskAsync("StepTriggered");
            }
            else
            {

                if (Microsoft.Devices.Environment.DeviceType != Microsoft.Devices.DeviceType.Emulator)
                {

                    ApiSupportedCapabilities caps = await SenseHelper.GetSupportedCapabilitiesAsync();

                    //Use StepCounterUpdate to trigger live tile update if it is supported. Otherwise we use time trigger
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
                    //On emulator we use always TimeTrigger
                    BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

                    IBackgroundTrigger trigger = new TimeTrigger(15, false);
                    await RegisterBackgroundTaskAsync(trigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
                }

            }

            await CreateOrRemoveTileAsync(removeTile);

            return;
        }

        private async void ApplicationBarIconButton_Click_Next(object sender, EventArgs e)
        {
            await App.Engine.ChangeDayAsync(-1);
            UpdateMenuAndAppBarIcons();
        }

        private async void ApplicationBarIconButton_Click_Back(object sender, EventArgs e)
        {
            await App.Engine.ChangeDayAsync(1);
            UpdateMenuAndAppBarIcons();
        }

    }
}