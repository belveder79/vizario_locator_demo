#﻿ Vizario CapsLoc Application Examples for Unity3D

 This repository contains a collection of application examples for [Unity3D](https://unity.com) to get started with development using the Vizario.CapsLoc sensor cube.

![Vizario CapsLoc Sensor Cube](img/capsloc_sensor.jpg?raw=true "Vizario.CapsLoc Sensor Cube")

To set up Vizario.CapsLoc with your mobile device, please follow the instructions in the [Tutorial Video](https://youtu.be/or8ghl2m5fM).

- Companion App for iOS: https://apps.apple.com/pw/app/vizario-capsloc/id1562990158
- Companion App for Android: https://play.google.com/store/apps/details?id=io.ar4.calibrationapp

Note that both the tutorial video and this sample repository will be improved over time.

## Setup

Important:
- import unitypackage [VizarioCapsLocDLL](https://www.dropbox.com/s/ntc4hdrnkdl6cep/capsloc-master-21.11.04.16.unitypackage?dl=0)
- check if latest Firmware on CapsLoc Cube, if not upload new [Firmware](https://www.dropbox.com/sh/hpzjsknbeh27m02/AABas8TiwU8v1n-aVvwHaHc6a?dl=0)
- Check MqttHostIP (CapsLocRuntime - VizarioCapsLocManager): 127.0.0.1 when App on device running broker (otherwise change IP)
- IMU Calib, will be loaded from Application.persistentDataPath; In VizarioCapsLocManager there is the possibility to enable copiing a calib file from Resources to persitsenDataPath
- Tested with Unity2019.4.3(LTS), ARFoundation V3.1.3, XR Plugin Manager 3.2.16, TextMesh Pro 2.01
- make sure Settings-Graphics: Unlit/Texture in always include Shader

## Demo Scenes

### ARMeasuring (01_Measuring)

Take real world measurements (utm) in an AR environment combining ARFoundation Tracking and Vizario Location Service.

![Screenshot of VizarioLocatorDemo Scene](img/take_measurement.PNG?raw=true "VizarioLocatorDemo.scene")

### SignPostExample (02_Signpost)
Place a AR SignPost into real world making use of ARFoundation and Vizario Location Service.

![Screenshot of SignPostExample Scene](img/place_sign.PNG?raw=true "SignPostExample.scene")

### AvatarDemo (03_Avatar)
Visualize Vizario.CapsLoc Devices in your local AR environment.

![Screenshot of SignPostExample Scene](img/avatar_demo.PNG?raw=true "SignPostExample.scene")

setup:
- To run the Avatar Demo you need two CapsLoc Devices connected with two Mobile Devices running the app
- provide Mqtt Server in AvatarSession GameObject, if no certificate is needed, set variable to null, otherwise provide certificate as shown in the Avatar
- Since it is hard to provide a robust and accurate absolute height value there are several options in this Example. If the second CapsLoc Device is placed on the ground fixHeightToGroundPlane can be set to place the visualization of the second device at ground plane height. If not set, there is a toggle to switch between using Altimeter or the relative height to the detected groundplane, in the second case both users should be on the same ground plane.


## Demo Description

- VizarioCapsLocManager.cs : This script is added to CapsLocRuntime GameObject and will take care of communication with the Chip. It will spawn an MqttClient instance, connecting to the specified IP Address. If more than one Instances are used (eg. 2 devices connecting to the same chip, make sure to use ClientIDs.) An Calibration File can be specified too, the File should be placed in the ApplicationPersistent Path (or use check box for StreamingAssets).
- LocalizationHandler.cs :   
	Here handling of Localization data is shown. Both possibilities to access GPS, IMU and Altimeter data, either via Callbacks, or via Getter methods.
	Furthermore there are two examples how to combine ARFoundations Local tracking with real world positioning:
		- SetWorldOrigin() is used to align the local coordinate system to current UTM Position (in 03_Avatar there is a continous update of WorldOrigin Object)
		- AddObject() is an example to combine RayCasting in local space to obtain a Global Position.

- MapCreator.cs : Spawning an OpenStreetMap to show current position. Here an comparison from device GPS vs our GPS is shown.
- GUI.cs : Configurations of the chip can also be done via Unity, here are some examples. However, using the VizarioCalibrationApp is recommended.
- NorthingHandler.cs : Functionallitie to calculate northing fix of ArFoundation Pose using GPS RTK fixed mesaurements in UTM space (due to UTM y = north orientated).
- Locations.cs: Handling the json file with Locations for the SignPostExample
- Avatar.cs : Handling other CapsLoc positions in the AvatarDemo Project.
- MqttClient.cs : Handling Mqtt communication vith other AvatarDemo Apps.
- Antenna : Child GameObject of ARCamera, which is working as Antenna to Camera Calibration. (if Maxtenna is used => use imu_to_camera calibration values + (-0.02; 0.07; 0), where z & y are switched since we are in Unity space. )

## VizarioCapsLoc Functionallity

### How to combine CapsLoc in Unity

Algorithms and experiments for fusion tracking in Unity can be seen in the [Documentation](https://www.overleaf.com/read/dzgwnxjttshf). which will be updated continuously.

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

# Version Log
 - v1.0 2021.09.28 - Initial public exposure
