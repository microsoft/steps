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
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Steps
{
    public sealed partial class MainPage : Page
    {
        #region Private constants
        /// <summary>
        /// Tile ID
        /// </summary>
        private const string TILE_ID = "SecondaryTile.Steps";

        /// <summary>
        /// Target daily step count
        /// </summary>
        private const uint TARGET_STEPS = 10000;

        /// <summary>
        /// Number of large meter images
        /// </summary>
        private const uint NUM_LARGE_METER_IMAGES = 9;

        /// <summary>
        /// Number of small meter images
        /// </summary>
        private const uint NUM_SMALL_METER_IMAGES = 4;
        #endregion

        #region Private members
        /// <summary>
        /// Model for the app
        /// </summary>
        private MainModel _model = null;

        /// <summary>
        /// Synchronization object
        /// </summary>
        private SemaphoreSlim _sync = new SemaphoreSlim(1);

        /// <summary>
        /// Timer to update step counts periodically
        /// </summary>
        //private DispatcherTimer _pollTimer;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            _model = new MainModel();
            LayoutRoot.DataContext = _model;
            StepGraph.Loaded += StepGraph_Loaded;
        }

        /// <summary>
        /// Executes when the Step graph finished loading.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void StepGraph_Loaded(object sender, RoutedEventArgs e)
        {
            _model.SetDimensions(StepGraph.ActualWidth, StepGraph.ActualHeight);
        }

        /// <summary>
        /// Step graph tap event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void StepGraph_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _model.CycleZoomLevel();
        }

        /// <summary>
        /// Decrease opacity of the command bar when closed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void CommandBar_Closed(object sender, object e)
        {
            cmdBar.Opacity = 0.5;
        }

        /// <summary>
        /// Increase opacity of command bar when opened
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void CommandBar_Opened(object sender, object e)
        {
            cmdBar.Opacity = 1;
        }

        /// <summary>
        /// About menu item click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        /// <summary>
        /// Updates menu and app bar icons
        /// </summary>
        private void UpdateMenuAndAppBarIcons()
        {
            // Show unpin or pin button
            //ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
            //if (!SecondaryTile.Exists(TILE_ID))
            //{
            //    btn.IconUri = new Uri("Assets/Images/pin-48px.png", UriKind.Relative);
            //    btn.Text = "Pin";
            //}
            //else
            //{
            //    btn.IconUri = new Uri("Assets/Images/unpin-48px.png", UriKind.Relative);
            //    btn.Text = "Unpin";
            //}
            //ApplicationBarIconButton back = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            //back.IsEnabled = _model.DayOffset != 6;
            //ApplicationBarIconButton next = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            //next.IsEnabled = _model.DayOffset != 0;
        }

        /// <summary>
        /// Creates secondary tile if it is not yet created or removes the tile if it already exists.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void ApplicationBar_PinTile(object sender, RoutedEventArgs e)
        {
            //bool removeTile = SecondaryTile.Exists(TILE_ID);
            //if (removeTile)
            //{
            //    await RemoveBackgroundTaskAsync("StepTriggered");
            //}
            //else
            //{
            //    ApiSupportedCapabilities caps = await SenseHelper.GetSupportedCapabilitiesAsync();
            //    // Use StepCounterUpdate to trigger live tile update if it is supported. Otherwise we use time trigger
            //    if (caps.StepCounterTrigger)
            //    {
            //        var myTrigger = new DeviceManufacturerNotificationTrigger(SenseTrigger.StepCounterUpdate, false);
            //        await RegisterBackgroundTaskAsync(myTrigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
            //    }
            //    else
            //    {
            //        BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            //        IBackgroundTrigger trigger = new TimeTrigger(15, false);
            //        await RegisterBackgroundTaskAsync(trigger, "StepTriggered", "BackgroundTasks.StepTriggerTask");
            //    }
            //}
            //await CreateOrRemoveTileAsync(removeTile);
        }

        /// <summary>
        /// Next day button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void ApplicationBarIconButtonNext_Click(object sender, RoutedEventArgs e)
        {
            await _sync.WaitAsync();
            try
            {
                await _model.DecreaseDayOffsetAsync();
                UpdateMenuAndAppBarIcons();
            }
            finally
            {
                _sync.Release();
            }
        }

        /// <summary>
        /// Previous day button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void ApplicationBarIconButtonBack_Click(object sender, RoutedEventArgs e)
        {
            await _sync.WaitAsync();
            try
            {
                await _model.IncreaseDayOffsetAsync();
                UpdateMenuAndAppBarIcons();
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
