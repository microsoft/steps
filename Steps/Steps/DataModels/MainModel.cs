using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steps 
{


    public class MainModel : INotifyPropertyChanged
    {
        private double _width;
        private double _height;
        private List<uint> _steps = new List<uint>();
        private uint _max = 20000;
        int _resolutionInMinutes = 15;
        int _arrayMaxSize = 96;
        int _margin = 6;

        public void setParameters(double width, double height, List<uint> steps, int resolutionInMinutes)
        {
            _width = width;
            _height = height;
            _steps = steps;

            _resolutionInMinutes = resolutionInMinutes;  // Resolution of graph is 15 min. Can be 5,10,15,20...60
            _arrayMaxSize = 1440 / _resolutionInMinutes;

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
                 

            NotifyPropertyChanged(null); //We refresh all properties here
        }



        public string MAX { get { return _max.ToString(); } }
        public string HALF { get { return (_max/2).ToString(); } }
        public string MARGIN { get { return (_margin).ToString(); } }


        private uint _walkingSteps = 0;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
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

        private uint _runningSteps = 0;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
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
        private uint _stepsToday = 0;

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

        public string PathString { 
            get {
                
                String path = "M 6," + ((uint)_height).ToString(); 
                for(int i = 1 ; i < _steps.Count ; i++ ){
                    uint stepcount = _max > _steps[i] ? _steps[i] : _max;

                    path += " L " + ((uint)(((double)i / _arrayMaxSize) * (_width-6) )+6).ToString() + "," + (_height - (uint)(stepcount * (_height / _max))).ToString() + " ";
                }
                System.Diagnostics.Debug.WriteLine(path);
                return path; 
            } 
        }
        

        public event PropertyChangedEventHandler PropertyChanged;
        
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
