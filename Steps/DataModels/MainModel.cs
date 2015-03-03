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
using System.ComponentModel;
using System.Globalization;

namespace Steps
{
    /// <summary>
    /// Main model used in the application
    /// </summary>
    public class MainModel : INotifyPropertyChanged
    {
        #region Variable declarations
        /// <summary>
        /// Index of day.
        /// 0 = today, 1 = yesterday, 2 = 2 days ago...
        /// </summary>
        public int _day = 0; 

        /// <summary>
        /// Resolution of graph is 15 min. Can be 5,10,15,20...60
        /// </summary>
        public const int _resolutionInMinutes = 15;

        /// <summary>
        /// Array size.
        /// </summary>
        public const int _ArrayMaxSize = 1440 / _resolutionInMinutes + 1;

        /// <summary>
        /// Property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Number of walking steps.
        /// </summary>
        private uint _walkingSteps = 0;

        /// <summary>
        /// Number of running steps.
        /// </summary>
        private uint _runningSteps = 0;

        /// <summary>
        /// Number of total steps for the current day.
        /// </summary>
        private uint _stepsToday = 0;

        /// <summary>
        /// Graph canvas' width in pixels.
        /// </summary>
        private double _width;

        /// <summary>
        /// Graph canvas' height in pixels.
        /// </summary>
        private double _height;

        /// <summary>
        /// Graph canvas margin in pixels.
        /// </summary>
        private const int _margin = 6;

        /// <summary>
        /// Graph canvas max height in steps (can be 20000, 10000, 5000)
        /// </summary>
        private uint _max = 20000;

        /// <summary>
        /// Maximum array size.
        /// </summary>
        private const int _arrayMaxSize = 96;

        /// <summary>
        /// Collection of steps.
        /// </summary>
        private List<uint> _steps = new List<uint>();
        #endregion

        /// <summary>
        /// Sets graph parameters.
        /// </summary>
        /// <param name="width">Graph canvas' width in pixels.</param>
        /// <param name="height">Graph canvas' height in pixels.</param>
        public void SetParameters(double width, double height)
        {
            _width = width;
            _height = height;
            //We refresh all properties here
            NotifyPropertyChanged(null);
        }
        
        /// <summary>
        /// Updates graph steps
        /// </summary>
        /// <param name="steps">Steps list</param>
        public void UpdateGraphSteps(List<uint> steps)
        {
            _steps = steps;
            //We refresh all properties here
            NotifyPropertyChanged(null);
        }

        /// <summary>
        /// Change steps range in graph
        /// </summary>
        public void ChangeMax()
        {
            if (_max == 20000)
                _max = 10000;
            else if (_max == 10000)
                _max = 5000;
            else
                _max = 20000;
            //We refresh all properties
            NotifyPropertyChanged(null);
        }

        /// <summary>
        /// Gets maximum steps range for graph
        /// </summary>
        public string Max { get { return _max.ToString(); } }
        
        /// <summary>
        /// Gets half steps range for graph
        /// </summary>
        public string Half { get { return (_max / 2).ToString(); } }

        /// <summary>
        /// Gets margin of the step graph
        /// </summary>
        public string Margin { get { return (_margin).ToString(); } }

        /// <summary>
        /// Get the datetime for the graph
        /// </summary>
        public string Date
        {
            get
            {
                CultureInfo ci = new CultureInfo("en-GB");
                string format = "D";
                if (_day == 0)
                    return "Today";
                else if (_day == 1)
                    return "Yesterday";
                else if (_day == 2)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(2);
                    return time.ToString(format, ci);
                }
                else if (_day == 3)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(3);
                    return time.ToString(format, ci);
                }
                else if (_day == 4)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(4);
                    return time.ToString(format, ci);
                }
                else if (_day == 5)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(5);
                    return time.ToString(format, ci);
                }
                else if (_day == 6)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(6);
                    return time.ToString(format, ci);
                }
                else
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(7);
                    return time.ToString(format, ci);
                }
            }
        }

        /// <summary>
        /// WalkingSteps property. This property is displayed on the app
        /// </summary>
        public uint WalkingSteps
        {
            get
            {
                return _walkingSteps;
            }
            set
            {
                if (value != _walkingSteps)
                {
                    _walkingSteps = value;
                    NotifyPropertyChanged("WalkingSteps");
                }
            }
        }

        /// <summary>
        /// RunningSteps property. This property is displayed on the app
        /// </summary>
        public uint RunningSteps
        {
            get
            {
                return _runningSteps;
            }
            set
            {
                if (value != _runningSteps)
                {
                    _runningSteps = value;
                    NotifyPropertyChanged("RunningSteps");
                }
            }
        }

        /// <summary>
        /// Total steps today property.
        /// </summary>
        public uint StepsToday
        {
            get
            {
                return _stepsToday;
            }
            set
            {
                if (value != _stepsToday)
                {
                    _stepsToday = value;
                    NotifyPropertyChanged("StepsToday");
                }
            }
        }

        /// <summary>
        /// This property makes canvas path that is displayed to the user. 
        /// For instance string could look like "M 6,255 L 10,255  L 14,255  L 18,255  L 23,255...
        /// where M 6, 255 is a start point for path ( 6 pixels from left, 255 pixels down)
        /// L 10, 255 line that goes 4 pixels to right from previous point 
        /// More details here: http://msdn.microsoft.com/en-us/library/ms752293(v=vs.110).aspx
        /// </summary>
        public string PathString
        {
            get
            {
                String path = "M 6," + ((uint)_height).ToString();
                for (int i = 1; i < _steps.Count; i++)
                {
                    uint stepcount = _max > _steps[i] ? _steps[i] : _max;
                    path += " L " + ((uint)(((double)i / _arrayMaxSize) * (_width - 12)) + 6).ToString() + "," + (_height - (uint)(stepcount * (_height / _max))).ToString() + " ";
                }
                return path;
            }
        }

        /// <summary>
        /// Execures when a property has changed
        /// </summary>
        /// <param name="propertyName">Property which will be changed</param>
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
