using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steps
{


    public class MainModel : INotifyPropertyChanged
    {

        public int day = 0; //0 = today, 1 = yesterday, 2 = 2 days ago...
        public const int _resolutionInMinutes = 15;  // Resolution of graph is 15 min. Can be 5,10,15,20...60
        public const int _ArrayMaxSize = 1440 / _resolutionInMinutes + 1;
        public event PropertyChangedEventHandler PropertyChanged;

        private uint _walkingSteps = 0;
        private uint _runningSteps = 0;
        private uint _stepsToday = 0;

        private double _width;  //Graph canvas' width in pixels
        private double _height; //Graph canvas' height in pixels
        private const int _margin = 6;  //Graph canvas' margin in pixels
        private uint _max = 20000;  //Graph canvas max height in steps (can be 20000,10000,5000)
        private const int _arrayMaxSize = 96;
        private List<uint> _steps = new List<uint>();


        public void SetParameters(double width, double height)
        {
            _width = width;
            _height = height;
            NotifyPropertyChanged(null); //We refresh all properties here

        }
        
        public void UpdateGraphSteps(List<uint> steps)
        {
            _steps = steps;
            NotifyPropertyChanged(null); //We refresh all properties here
        }

        public void ChangeMax()
        {
            if (_max == 20000)
                _max = 10000;
            else if (_max == 10000)
                _max = 5000;
            else
                _max = 20000;
            NotifyPropertyChanged(null); //We refresh all properties
        }

        public string Max { get { return _max.ToString(); } }
        public string Half { get { return (_max / 2).ToString(); } }
        public string Margin { get { return (_margin).ToString(); } }
        public string Date
        {
            get
            {
                CultureInfo ci = new CultureInfo("en-GB");

                string format = "D";

                if (day == 0)
                    return "Today";
                else if (day == 1)
                    return "Yesterday";
                else if (day == 2)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(2);
                    return time.ToString(format, ci);
                }
                else if (day == 3)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(3);
                    return time.ToString(format, ci);
                }
                else if (day == 4)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(4);
                    return time.ToString(format, ci);
                }
                else if (day == 5)
                {
                    DateTime time = DateTime.Now - TimeSpan.FromDays(5);
                    return time.ToString(format, ci);
                }
                else if (day == 6)
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
