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

namespace Steps
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();

            this.Loaded += (sender, args) =>
          {               var ver = Windows.ApplicationModel.Package.Current.Id.Version;
              VersionNumber.Text = string.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Revision);
          };
        }
    }
}