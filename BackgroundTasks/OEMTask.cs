
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Lumia.Sense;
using Windows.UI.StartScreen;


namespace BackgroundTasks
{
    /// <summary>
    /// Toast helper
    /// </summary>
    public sealed class Helper
    {
        public static uint GetMeter(uint steps)
        {
            if (steps < 1400) { return 1; }
            else if (steps < 2600) { return  2; }
            else if (steps < 3800) { return  3; }
            else if (steps < 5000) { return  4; }
            else if (steps < 6200) { return  5; }
            else if (steps < 7400) { return  6; }
            else if (steps < 8600) { return  7; }
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

    /// <summary>
    /// Background task class for step counter trigger
    /// </summary>
    public sealed class StepTriggerTask : IBackgroundTask
    {
        private const string TileID = "SecondaryTile.Steps";

        StepCount _steps;
        SenseError _lastError;

        /// <summary>
        /// Performs the work of a background task. The system calls this method when
        /// the associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            if (await GetStepsAsync())
            {
                UpdateTile(_steps.RunningStepCount + _steps.WalkingStepCount);
            }

            deferral.Complete();
        }

        private async Task<bool> GetStepsAsync()
        {
            StepCounter stepCounter = null;

            try
            {
                stepCounter = await StepCounter.GetDefaultAsync();
                _steps = await stepCounter.GetStepCountForRangeAsync(DateTime.Now.Date, DateTime.Now - DateTime.Now.Date);
            }
            catch (Exception e)
            {
                _lastError = SenseHelper.GetSenseError(e.HResult);
                return false;
            }
            finally
            {
                if (stepCounter != null) 
                    stepCounter.Dispose();
            }
            return true;
        }

        private bool UpdateTile(uint stepCount)
        {
            uint meter = Helper.GetMeter(stepCount);
            uint meter_small = Helper.GetSmallMeter(stepCount);

            var smallTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare71x71Image);

            XmlNodeList imageAttribute = smallTile.GetElementsByTagName("image");
            ((XmlElement)imageAttribute[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/small_square" + meter_small + ".png");

            var bindingSmall = (XmlElement)smallTile.GetElementsByTagName("binding").Item(0);


            // Square tile
            var SquareTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150PeekImageAndText01);
            var tileTextAttributes = SquareTile.GetElementsByTagName("text");
            tileTextAttributes[0].AppendChild(SquareTile.CreateTextNode((_steps.WalkingStepCount + _steps.RunningStepCount).ToString() + " steps"));
            tileTextAttributes[1].AppendChild(SquareTile.CreateTextNode(_steps.RunningStepCount.ToString() + " running steps"));
            tileTextAttributes[2].AppendChild(SquareTile.CreateTextNode(_steps.WalkingStepCount.ToString() + " walking steps"));
            
            var bindingSquare = (XmlElement)SquareTile.GetElementsByTagName("binding").Item(0);
            bindingSquare.SetAttribute("branding", "none");

            XmlNodeList img = SquareTile.GetElementsByTagName("image");
            ((XmlElement)img[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/square" + meter_small + ".png");

            // Provide a wide tile

            var wideTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150ImageAndText02);
            var squareTileTextAttributes = wideTileXml.GetElementsByTagName("text");
            squareTileTextAttributes[0].AppendChild(wideTileXml.CreateTextNode(stepCount.ToString() + " steps today"));

            imageAttribute = wideTileXml.GetElementsByTagName("image");
            ((XmlElement)imageAttribute[0]).SetAttribute("src", "ms-appx:///Assets/Tiles/wide"+meter+".png");



            var bindingWide = (XmlElement)wideTileXml.GetElementsByTagName("binding").Item(0);
            bindingWide.SetAttribute("branding", "none");


            // Add the wide tile to the notification.
            var nodeWide = smallTile.ImportNode(bindingWide, true);
            var nodeSquare = smallTile.ImportNode(bindingSquare, true);
            smallTile.GetElementsByTagName("visual").Item(0).AppendChild(nodeWide);
            smallTile.GetElementsByTagName("visual").Item(0).AppendChild(nodeSquare);

            // Create the notification based on the XML content.
            var tileNotification = new TileNotification(smallTile);

            // Create a secondary tile updater and pass it the secondary tileId
            var tileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(TileID);

            // Send the notification to the secondary tile.
            tileUpdater.Update(tileNotification);

            return true;
        }
    }
}

// end of file
