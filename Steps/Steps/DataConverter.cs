/*
 * Copyright (c) 2014 Microsoft Mobile. All rights reserved.
 * See the license text file provided with this project for more information.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Steps
{

    public class Half : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return (double)value/2;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return "";
        }

    }

    public class Margin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return (double)value -6;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return "";
        }

    }
    

}
