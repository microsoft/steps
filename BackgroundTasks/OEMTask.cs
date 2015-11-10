/*
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
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Devices.Sensors;
using Windows.UI.Notifications;
using Lumia.Sense;

using BackgroundTasks.Converters;

namespace BackgroundTasks
{
    /// <summary>
    /// Background task class for step counter trigger
    /// </summary>
    public sealed class StepTriggerTask : IBackgroundTask
    {
        #region Constant definitions
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
        /// Number of steps
        /// </summary>
        private StepCountData _steps;

        /// <summary>
        /// Sens error code
        /// </summary>
        private SenseError _lastError;
        #endregion

        /// <summary>
        /// Performs the work of a background task. The system calls this method when
        /// the associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try
            {
                if (await GetStepsAsync())
                {
                    UpdateTile(_steps.RunningCount + _steps.WalkingCount);
                }
            }
            catch (Exception)
            {
            }
            deferral.Complete();
        }

        /// <summary>
        /// Returns small live tile image index
        /// </summary>
        /// <param name="stepCount">Current step count</param>
        /// <returns>Small live tile image index</returns>
        public static uint GetSmallLiveTileImageIndex(uint stepCount)
        {
            return (NUM_SMALL_METER_IMAGES - 1) * Math.Min(stepCount, TARGET_STEPS) / TARGET_STEPS;
        }

        /// <summary>
        /// Returns large live tile image index
        /// </summary>
        /// <param name="stepCount">Current step count</param>
        /// <returns>Large live tile image index</returns>
        public static uint GetLargeLiveTileImageIndex(uint stepCount)
        {
            return (NUM_LARGE_METER_IMAGES - 1) * Math.Min(stepCount, TARGET_STEPS) / TARGET_STEPS;
        }

        /// <summary>
        /// Gets number of steps for current day
        /// </summary>
        /// <returns><c>true</c> if steps were successfully fetched, <c>false</c> otherwise</returns>
        private async Task<bool> GetStepsAsync()
        {
            // First try the pedometer
            try
            {
                var readings = await Pedometer.GetSystemHistoryAsync(DateTime.Now.Date, DateTime.Now - DateTime.Now.Date);
                _steps = StepCountData.FromPedometerReadings(readings);
                return true;
            }
            catch (Exception)
            {
                // Continue to the fallback
            }

            // Fall back to using Lumia Sensor Core.
            StepCounter stepCounter = null;
            try
            {
                stepCounter = await StepCounter.GetDefaultAsync();
                StepCount count = await stepCounter.GetStepCountForRangeAsync(
                    DateTime.Now.Date,
                    DateTime.Now - DateTime.Now.Date);
                _steps = StepCountData.FromLumiaStepCount(count);
            }
            catch (Exception e)
            {
                _lastError = SenseHelper.GetSenseError(e.HResult);
                return false;
            }
            finally
            {
                if (stepCounter != null)
                {
                    stepCounter.Dispose();
                }
            }
            return true;
        }

        /// <summary>
        /// Update the live tile
        /// </summary>
        /// <param name="stepCount">Step count</param>
        private void UpdateTile(uint stepCount)
        {
            uint meterIndex = GetLargeLiveTileImageIndex(stepCount);
            uint smallMeterIndex = GetSmallLiveTileImageIndex(stepCount);
            var smallTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare71x71Image);
            XmlNodeList imageAttribute = smallTile.GetElementsByTagName("image");
            ((XmlElement)imageAttribute[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/small_square" + smallMeterIndex + ".png");
            var bindingSmall = (XmlElement)smallTile.GetElementsByTagName("binding").Item(0);

            // Square tile
            var SquareTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150PeekImageAndText01);
            var tileTextAttributes = SquareTile.GetElementsByTagName("text");
            tileTextAttributes[0].AppendChild(SquareTile.CreateTextNode((_steps.TotalCount).ToString() + " steps"));
            tileTextAttributes[1].AppendChild(SquareTile.CreateTextNode(_steps.RunningCount.ToString() + " running steps"));
            tileTextAttributes[2].AppendChild(SquareTile.CreateTextNode(_steps.WalkingCount.ToString() + " walking steps"));
            var bindingSquare = (XmlElement)SquareTile.GetElementsByTagName("binding").Item(0);
            bindingSquare.SetAttribute("branding", "none");
            XmlNodeList img = SquareTile.GetElementsByTagName("image");
            ((XmlElement)img[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/square" + smallMeterIndex + ".png");

            // Wide tile
            var wideTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150ImageAndText02);
            var squareTileTextAttributes = wideTileXml.GetElementsByTagName("text");
            squareTileTextAttributes[0].AppendChild(wideTileXml.CreateTextNode(stepCount.ToString() + " steps today"));
            imageAttribute = wideTileXml.GetElementsByTagName("image");
            ((XmlElement)imageAttribute[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/wide" + meterIndex + ".png");
            var bindingWide = (XmlElement)wideTileXml.GetElementsByTagName("binding").Item(0);
            bindingWide.SetAttribute("branding", "none");

            var nodeWide = smallTile.ImportNode(bindingWide, true);
            var nodeSquare = smallTile.ImportNode(bindingSquare, true);
            smallTile.GetElementsByTagName("visual").Item(0).AppendChild(nodeWide);
            smallTile.GetElementsByTagName("visual").Item(0).AppendChild(nodeSquare);
            var tileNotification = new TileNotification(smallTile);
            var tileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(TILE_ID);
            tileUpdater.Update(tileNotification);
        }
    }
}
