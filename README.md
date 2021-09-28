# vizario_locator_demo

## Setup

Important:
- import unitypackage [VizarioCapsLocDLL](https://www.dropbox.com/s/7psan5kdd3nmcfn/capsloc-CapsLocDLL-v1.0-21.09.28.11.unitypackage?dl=0)
- Check MqttHostIP (CapsLocRuntime - VizarioCapsLocManager): 127.0.0.1 when App on device running broker (otherwise change IP)
- IMU Calib, will be loaded from Application.persistentDataPath; In VizarioCapsLocManager there is the possibility to enable copiing a calib file from Resources to persitsenDataPath
- ARFoundation V3.1.3
- make sure Settings-Graphics: Unlit/Texture in always include Shader

## Demo Scenes

### VizarioLocatorDemo

Take real world measurements (utm) in an AR environment combining ARFoundation Tracking and Vizario Location Service.

![Screenshot of VizarioLocatorDemo Scene](img/take_measurement.PNG?raw=true "VizarioLocatorDemo.scene")

### SignPostExample
Place a AR SignPost into real world making use of ARFoundation and Vizario Location Service.

![Screenshot of SignPostExample Scene](img/place_sign.PNG?raw=true "SignPostExample.scene")

## Demo Description

- VizarioCapsLocManager.cs : This script is added to CapsLocRuntime GameObject and will take care of communication with the Chip. It will spawn an MqttClient instance, connecting to the specified IP Address. If more than one Instances are used (eg. 2 devices connecting to the same chip, make sure to use ClientIDs.) An Calibration File can be specified too, the File should be placed in the ApplicationPersistent Path (or use check box for StreamingAssets). 
- LocalizationHandler.cs :   
	Here handling of Localization data is shown. Both possibilities to access GPS, IMU and Altimeter data, either via Callbacks, or via Getter methods.
	Furthermore there are two examples how to combine ARFoundations Local tracking with real world positioning:
		- SetWorldOrigin() is used to align the local coordinate system to current UTM Position
		- AddObject() is an example to combine RayCasting in local space to obtain a Global Position.

- MapCreator.cs : Spawning an OpenStreetMap to show current position. Here an comparison from device GPS vs our GPS is shown.
- GUI.cs : Configurations of the chip can also be done via Unity, here are some examples. However, using the VizarioCalibrationApp is recommended.
- NorthingHandler.cs : Functionallitie to calculate northing fix of ArFoundation Pose using GPS RTK fixed mesaurements in UTM space (due to UTM y = north orientated).
- Locations.cs: Handling the json file with Locations for the SignPostExample

## VizarioCapsLoc Functionallity

### CapsLocManager
First the CapsLocRuntime Prefap (or an GameObject with this name containing the CapsLocManager Scripts) musst be in the Scene to start communication with the Sensor Cube.
Here you have following main configurations:
- MqttIP : The IP Adrress of the mqtt broker to connect to (normally the device running VizarioCapsLoc App)
- MqttPort : Set to -1 if you are using the default Port 1883, otherwise set Port
- MqttClientID : The Client ID to be used, if more than one Projects using VizarioCapsLoc Unity Lib, this should be set to an differing string (name)
- CalibrationFile : Name of the calibration file obtained from the CapsLoc Calibration App, should be in the AppPersistentPath
- LoadCalibrationFromStreamingAssets : If true, the calibration File is loaded from the StreamingAssets Folder and copied to PErsistantPAth

These are getter functions to access current sensor values:
- bool GetAltimeterValues(out float alti, out float temp) : Getter for last Altimeter update ; return false if no valid data
- bool GetGyroQuaternionRaw(out Quaternion q) : Getter for raw IMU values without calibration (and not in Unity space) ; return false if no valid data
- bool GetGyroQuaternion(out Quaternion q) Getter for IMU values ; return false if no valid data
- bool GetUTMPosition(out double x, out double y, out string z, out int state) : Getter for UTM position ; return false if no valid data
- bool GetLatLonPoition(out double lat, out double lon, out int state) : Getter for Lat/Lon Position; return false if no valid data
- bool IsChipConnected() : returns connection state of Chip
- bool IsCalibrationSet() : returns calibration state
- bool IsMqttConnected() : returns Mqtt connection state

The Following Commands can be used via CapsLocManager.Advanced : 
- GyroPower(bool power) : turn on/off IMU
- GyroUpdateRate(float rate) : set UpdateRate of IMU
- AltiPower(bool power) : turn on/off Altimeter
- AltiUpdateRate(float rate) : set UpdateRate of Altimeter
- GPSPower(bool power) : turn on/off GPS
- ResetConfig(int resetType) : 0 = hard reset, 1 = wifi reset, 2= factory reset
- SaveConfig() : saves current configuration on chip, otherwise it will not be the same after a restart
- SetMQTT(String server, int port) : Change MQTT Server (better do not use via code)
- SetNTRIP(bool enable, String server, int port, String stream, String username, String password) : set NTRIP Server for GPS corrrections

### CapsLocBehaviour
This script is for drag&drop use, the GameObjects transform will be updated according to the Sensor Chip.
Set update Position/Rotation to false if they should not be applied. Also use UTM_ref_point to achieve relative movements to this Point (in meter)

