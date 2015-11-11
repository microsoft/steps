Steps
=====
Steps is a sample application demonstrating the usage of Step Counter API. In this 
sample application, history data is used to display a graph of user’s steps during 
current day, and up to 7 days in the past.

Starting with Windows 10, this sample supports the Windows.Devices.Sensors.Pedometer.
It checks if the Pedometer is present before falling back to using the SensorCore
Step Counter.


1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. The
example requires the Lumia SensorCore SDK's NuGet package but will retrieve it
automatically (if missing) on first build.

To build the application you need to have Windows 10 and Windows 10 SDK installed.

Using the Windows 10 SDK:

1. Open the SLN file: File > Open Project, select the file `Steps.sln`
2. Select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Alternatively you can also build the example for the emulator (x86) in which case
the Steps will use simulated data. In order to use the emulator, you need to uncomment
the code block in StepsEngine to use the emulator, and include Lumia.Sense.Testing.

Please refer to the official SDK sample documentation for Universal Windows Platform
development.
https://github.com/microsoft/windows-universal-samples/

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
 
3. Version history
--------------------------------------------------------------------------------
* Version 2.0.0.0: Updated to Universal Windows Platform
* Version 1.1.0.3: Updated to use latest Lumia SensorCore SDK 1.1 Preview
* Version 1.1.0.2:
 * Some bug fixes made in this release.  
* Version 1.1: 
 * Step counter on live tile gets updated using triggers for background tasks. 
 * Besides today up to 7 days of step history made available. 
 * Update to use Lumia SensorCore SDK 1.0
* Version 1.0: The first release.


4. Downloads
---------

| Project | Release | Download |
| ------- | --------| -------- |
| Steps | v2.0.0.0 | [steps-2.0.zip](https://github.com/Microsoft/steps/archive/v2.0.zip) |
| Steps | v1.1.0.3 | [steps-1.1.0.3.zip](https://github.com/Microsoft/steps/archive/v1.1.0.3.zip) |
| Steps | v1.1.0.2 | [steps-1.1.0.2.zip](https://github.com/Microsoft/steps/archive/v1.1.0.2.zip) |
| Steps | v1.1 | [steps-1.1.zip](https://github.com/Microsoft/steps/archive/v1.1.zip) |
| Steps | v1.0 | [steps-1.0.zip](https://github.com/Microsoft/steps/archive/v1.0.zip) |


5. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the SensorCore APIs

* Steps -  https://github.com/Microsoft/steps
* Places - https://github.com/Microsoft/places
* Tracks - https://github.com/Microsoft/tracks
* Activities - https://github.com/Microsoft/activities
* Recorder - https://github.com/Microsoft/recorder
