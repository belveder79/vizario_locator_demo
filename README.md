# vizario_locator_demo

## Setup

Important:
- Check MqttHostIP (Runtime - Vizario GPS): 127.0.0.1 when App on device running broker (otherwise change IP)
- IMU Calib, will be loaded from Application.persistentDataPath; In Localization handler there is the possibilitie to enable copiing a calib file from Resources to persitsenDataPath
- ARFoundation V3.1.3
- make sure Settings-Graphics: Unlit/Texture in always include Shader


## Demo Scenes

### VizarioLocatorDemo

Take real world measurements (utm) in an AR environment combining ARFoundation Tracking and Vizario Location Service.

![Screenshot of VizarioLocatorDemo Scene](img/take_measurement.PNG?raw=true "VizarioLocatorDemo.scene")


## Demo Description

- VizarioGPS.cs : This script is added to Runtime GameObject and will take care of communication with the Chip. It will spawn an MqttClient instance, connecting to the specified IP Address. If more than one Instances are used (eg. 2 devices connecting to the same chip, make sure to use ClientIDs.) An Calibration File can be specified too, the File should be placed in the ApplicationPersistent Path. An Example to copy such file from Resources is given in the LocalizationHandler.
- LocalizationHandler.cs :   
	Here handling of Localization data is shown. Both possibilities to access GPS, IMU and Altimeter data, either via Callbacks, or via Getter methods.
	Furthermore there are two examples how to combine ARFoundations Local tracking with real world positioning:
		- SetWorldOrigin() is used to align the local coordinate system to current UTM Position
		- AddObject() is an example to combine RayCasting in local space to obtain a Global Position.

- MapCreator.cs : Spawning an OpenStreetMap to show current position. Here an comparison from device GPS vs our GPS is shown.
- GUI.cs : Configurations of the chip can also be done via Unity, here are some examples. However, using the VizarioCalibrationApp is recommended.
- NorthingHandler.cs : Functionallitie to calculate northing fix of ArFoundation Pose using GPS RTK fixed mesaurements in UTM space (due to UTM y = north orientated).

## VizarioGPS Library Functionallity

- GyroPower(bool power) : turn on/off IMU
- GyroUpdateRate(float rate) : set UpdateRate of IMU
- AltiPower(bool power) : turn on/off Altimeter
- AltiUpdateRate(float rate) : set UpdateRate of Altimeter
- GPSPower(bool power) : turn on/off GPS
- ResetConfig(int resetType) : 0 = hard reset, 1 = wifi reset, 2= factory reset
- SaveConfig() : saves current configuration on chip, otherwise it will not be the same after a restart
- SetMQTT(String server, int port) : Change MQTT Server (better do not use via code)
- SetNTRIP(bool enable, String server, int port, String stream, String username, String password) : set NTRIP Server for GPS corrrections
- bool GetAltimeterValues(out float alti, out float temp) : Getter for last Altimeter update ; return false if no valid data
- bool GetGyroQuaternionRaw(out Quaternion q) : Getter for raw IMU values without calibration (and not in Unity space) ; return false if no valid data
- bool GetGyroQuaternion(out Quaternion q) Getter for IMU values ; return false if no valid data
- bool GetUTMPosition(out double x, out double y, out string z, out int state) : Getter for UTM position ; return false if no valid data
- bool GetLatLonPoition(out double lat, out double lon, out int state) : Getter for Lat/Lon Position; return false if no valid data
- bool IsChipConnected() : returns connection state of Chip
- bool IsCalibrationSet() : returns calibration state
- bool IsMqttConnected() : returns Mqtt connection state
- SetGPSCallback(GPSCallBack cb) : set GPS callback
- SetGyroCallback(GyroCallback cb) : set IMU callback
- SetAltiCallback(AltiCallback cb) : set Altimeter callback
- setResponseCallback(JSCallback cb) : set Response callback. This will forward all responses from the chip. Those are json messages, indicating if any settings appled to the chip succeeded or failed
