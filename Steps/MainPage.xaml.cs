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
using System.Windows.Threading;
using System.Threading;

namespace Steps
{
    /// <summary>
    /// Main page of the application
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
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
        private SemaphoreSlim _sync = new SemaphoreSlim( 1 );

        /// <summary>
        /// Timer to update step counts periodically
        /// </summary>
        private DispatcherTimer _pollTimer;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            _model = new MainModel();
            LayoutRoot.DataContext = _model;
            StepGraph.Loaded += StepGraph_Loaded;
        }

        #region NavigationHelper registration
        /// <summary>
        /// Called when a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected async override void OnNavigatedFrom( NavigationEventArgs e )
        {
            if( _pollTimer != null )
            {
                _pollTimer.Stop();
                _pollTimer = null;
            }
            await App.Engine.DeactivateAsync();
        }

        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">Provides data for non-cancelable navigation events</param>
        protected async override void OnNavigatedTo( NavigationEventArgs e )
        {
            await App.Engine.ValidateSettingsAsync();
            await App.Engine.ActivateAsync();
            UpdateMenuAndAppBarIcons();

            await _sync.WaitAsync();
            try
            {
                await _model.UpdateAsync();
            }
            finally
            {
                _sync.Release();
            }

            // Start poll timer to update steps counts periodically
            if( _pollTimer == null )
            {
                _pollTimer = new DispatcherTimer();
                _pollTimer.Interval = TimeSpan.FromSeconds( 5 );
                _pollTimer.Tick += PollTimerTick;
                _pollTimer.Start();
            }
        }
        #endregion

        /// <summary>
        /// Step counter poll timer callback
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void PollTimerTick( object sender, EventArgs e )
        {
            await _sync.WaitAsync();
            try
            {
                // No need to update if we are not looking at today
                if( _model.DayOffset == 0 )
                {
                    await _model.UpdateAsync();
                }
            }
            finally
            {
                _sync.Release();
            }
        }

        /// <summary>
        /// Executes when the Step graph finished loading.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void StepGraph_Loaded( object sender, RoutedEventArgs e )
        {
            _model.SetDimensions( StepGraph.ActualWidth, StepGraph.ActualHeight );
        }

        /// <summary>
        /// Step graph tap event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void StepGraph_Tap( object sender, System.Windows.Input.GestureEventArgs e )
        {
            _model.CycleZoomLevel();
        }

        /// <summary>
        /// About menu item click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void AboutMenuItem_Click( object sender, EventArgs e )
        {
            NavigationService.Navigate( new Uri( "/AboutPage.xaml", UriKind.Relative ) );
        }

        /// <summary>
        /// Removes background task
        /// </summary>
        /// <param name="taskName">Name of task to be removed</param>
        /// <returns>Asynchronous task</returns>
        private async static Task RemoveBackgroundTaskAsync( String taskName )
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            if( result != BackgroundAccessStatus.Denied )
            {
                // Remove previous registration
                foreach( var task in BackgroundTaskRegistration.AllTasks )
                {
                    if( task.Value.Name == taskName )
                    {
                        task.Value.Unregister( true );
                    }
                }
            }
        }

        /// <summary>
        /// Registers background task
        /// </summary>
        /// <param name="trigger">Task trigger</param>
        /// <param name="taskName">Task name</param>
        /// <param name="taskEntryPoint">Task entry point</param>
        /// <returns>Asynchronous task</returns>
        private async static Task RegisterBackgroundTaskAsync( IBackgroundTrigger trigger, String taskName, String taskEntryPoint )
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            if( result != BackgroundAccessStatus.Denied )
            {
                await RemoveBackgroundTaskAsync( taskName );

                // Register task
                BackgroundTaskBuilder myTaskBuilder = new BackgroundTaskBuilder();
                myTaskBuilder.SetTrigger( trigger );
                myTaskBuilder.TaskEntryPoint = taskEntryPoint;
                myTaskBuilder.Name = taskName;
                BackgroundTaskRegistration myTask = myTaskBuilder.Register();
            }
        }

        /// <summary>
        /// Creates or removes a secondary tile
        /// </summary>
        /// <param name="removeTile"><c>true</c> to remove tile, <c>false</c> to create tile</param>
        /// <returns>Asynchronous task</returns>
        private async Task CreateOrRemoveTileAsync( bool removeTile )
        {
            if( !removeTile )
            {
                var steps = await App.Engine.GetTotalStepCountAsync( DateTime.Now.Date );
                uint stepCount = steps.WalkingStepCount + steps.RunningStepCount;
                uint meter = ( NUM_SMALL_METER_IMAGES - 1 ) * Math.Min( stepCount, TARGET_STEPS ) / TARGET_STEPS;
                uint meterSmall = ( NUM_LARGE_METER_IMAGES - 1 ) * Math.Min( stepCount, TARGET_STEPS ) / TARGET_STEPS;
                try
                {
                    var secondaryTile = new SecondaryTile( TILE_ID, "Steps", "/MainPage.xaml", new Uri( "ms-appx:///Assets/Tiles/square" + meterSmall + ".png", UriKind.Absolute ), TileSize.Square150x150 );
                    secondaryTile.VisualElements.Square71x71Logo = new Uri( "ms-appx:///Assets/Tiles/small_square" + meterSmall + ".png", UriKind.Absolute );
                    secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = false;
                    secondaryTile.VisualElements.ShowNameOnWide310x150Logo = false;
                    secondaryTile.VisualElements.BackgroundColor = Color.FromArgb( 255, 0, 138, 0 );
                    secondaryTile.VisualElements.Wide310x150Logo = new Uri( "ms-appx:///Assets/Tiles/wide" + meter + ".png", UriKind.Absolute );
                    secondaryTile.RoamingEnabled = false;
                    await secondaryTile.RequestCreateAsync();
                }
                catch( Exception )
                {
                }
            }
            else
            {
                SecondaryTile secondaryTile = new SecondaryTile( TILE_ID );
                await secondaryTile.RequestDeleteAsync();
                UpdateMenuAndAppBarIcons();
            }
        }

        /// <summary>
        /// Updates menu and app bar icons
        /// </summary>
        private void UpdateMenuAndAppBarIcons()
        {
            // Show unpin or pin button
            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[ 2 ];
            if( !SecondaryTile.Exists( TILE_ID ) )
            {
                btn.IconUri = new Uri( "Assets/Images/pin-48px.png", UriKind.Relative );
                btn.Text = "Pin";
            }
            else
            {
                btn.IconUri = new Uri( "Assets/Images/unpin-48px.png", UriKind.Relative );
                btn.Text = "Unpin";
            }
            ApplicationBarIconButton back = (ApplicationBarIconButton)ApplicationBar.Buttons[ 0 ];
            back.IsEnabled = _model.DayOffset != 6;
            ApplicationBarIconButton next = (ApplicationBarIconButton)ApplicationBar.Buttons[ 1 ];
            next.IsEnabled = _model.DayOffset != 0;
        }

        /// <summary>
        /// Creates secondary tile if it is not yet created or removes the tile if it already exists.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void ApplicationBar_PinTile( object sender, EventArgs e )
        {
            bool removeTile = SecondaryTile.Exists( TILE_ID );
            if( removeTile )
            {
                await RemoveBackgroundTaskAsync( "StepTriggered" );
            }
            else
            {
                ApiSupportedCapabilities caps = await SenseHelper.GetSupportedCapabilitiesAsync();
                // Use StepCounterUpdate to trigger live tile update if it is supported. Otherwise we use time trigger
                if( caps.StepCounterTrigger )
                {
                    var myTrigger = new DeviceManufacturerNotificationTrigger( SenseTrigger.StepCounterUpdate, false );
                    await RegisterBackgroundTaskAsync( myTrigger, "StepTriggered", "BackgroundTasks.StepTriggerTask" );
                }
                else
                {
                    BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                    IBackgroundTrigger trigger = new TimeTrigger( 15, false );
                    await RegisterBackgroundTaskAsync( trigger, "StepTriggered", "BackgroundTasks.StepTriggerTask" );
                }
            }
            await CreateOrRemoveTileAsync( removeTile );
        }

        /// <summary>
        /// Next day button click event handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private async void ApplicationBarIconButtonNext_Click( object sender, EventArgs e )
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
        private async void ApplicationBarIconButtonBack_Click( object sender, EventArgs e )
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