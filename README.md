Steps
=====
Steps is a sample application demonstrating the usage of Step Counter API. In this 
sample application, history data is used to display a graph of user’s steps during 
current day. The combination of history data and real-time step count is used to 
calculate the number of total steps, and the number of walking and running steps.

For more information on implementation, visit Nokia Lumia Developer's Library: 
http://developer.nokia.com/resources/library/Lumia/sensorcore-sdk/samples.html
http://developer.nokia.com/resources/library/Lumia/sensorcore-sdk/step-counter-api.html

1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. The
example requires the Lumia SensorCore SDK's NuGet package but will retrieve it
automatically (if missing) on first build.

To build the application you need to have Windows 8.1 and Windows Phone SDK 8.1
installed.

Using the Windows Phone 8.1 SDK:

1. Open the SLN file: File > Open Project, select the file `Steps.sln`
2. Remove the "AnyCPU" configuration (not supported by the Lumia SensorCore SDK)
or simply select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Alternatively you can also build the example for the emulator (x86) in which case
the Steps will use simulated data.

Please see the official documentation for
deploying and testing applications on Windows Phone devices:
http://msdn.microsoft.com/en-us/library/gg588378%28v=vs.92%29.aspx

2. Implementation
--------------------------------------------------------------------------------

Two important functions for Steps sample in the SensorCore SDK's StepCounter class 
are GetCurrentReadingAsync and GetStepCountHistoryAsync. These functions are used in
the sample to show graph of today's walking and running steps and current amount
of the steps. 

GetCurrentReadingAsync returns cumulative number of steps from the last motion 
data reset. The sample shows the total number of steps for today so we need
to get the cumulative step count reading at the beginning of the day and then 
subtract it from the current cumulative step count reading.  

The number of steps at the beginning of the day is got by calling:

GetStepCountHistoryAsync(DateTime.Now.Date, DateTime.Now-DateTime.Now.Date)

The function call returns step count history array for today at five minute 
intervals. The array has cumulative step count readings at 00:00, 00:05, 00:10... 
To calculate step count reading for today we subtract the step count at 00:00 
from the value of GetCurrentReadingAsync.

To draw the graph we use the same array that the above call to GetStepCountHistoryAsync
returns.
 
3. Licence
--------------------------------------------------------------------------------

See the licence text file delivered with this project. The licence file is also
available online at https://github.com/nokia-developer/steps/blob/master/Licence.txt


4. Version history
--------------------------------------------------------------------------------

* Version 1.0: The first release.
* Version 1.1: Secondary live tile and history support added

5. Downloads
---------

| Project | Release | Download |
| ------- | --------| -------- |
| Steps | v1.0 | [steps-1.0.zip](https://github.com/nokia-developer/steps/archive/v1.0.zip) |
| Steps | v1.1 | [steps-1.1.zip](https://github.com/nokia-developer/steps/archive/v1.1.zip) |

6. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the SensorCore APIs

* Steps -  https://github.com/nokia-developer/steps
* Places - https://github.com/nokia-developer/places
* Tracks - https://github.com/nokia-developer/tracks
* Activities - https://github.com/nokia-developer/activities
* Recorder - https://github.com/nokia-developer/recorder
