# XR Public Spectator Interactive

## Introduction/Demo

[![Demo](https://img.youtube.com/vi/TY0tixBOBzg/0.jpg)](https://www.youtube.com/watch?v=TY0tixBOBzg&list=PLG6UT0XWAmnTg6YH4z-yfRCFUisKAga5l&index=1)

## Architecture

### Components

The system requires:

- 2 laptops:
  - **ZED:** One of which interfaces with the ZED camera, thus requiring a strong enough graphic card (we use the lab's laptop). This laptop also interfaces with the projector to show the ZED's camera feed on the large display.
  - **Trackers:** The other interfaces with the Vive headset, its trackers, and its base stations. As we do not render anything on the Vive headset, a display port to HDMI adapter is good enough to connect the headset to the laptop.
  - _Note:_ We cannot have 1 laptop to do both as the Vive headset and the projector are both treated as external monitors thus confusing the headset's drivers.
- **Origin marker:** A 43.3cm x 43.3cm ARUCO marker placed on the floor used as the common origin between devices and for detecting the floor. Marker must be seen by ZED during calibration and can be scanned by headset users.
- **Large display:** A large display large enough to fit 3 33.6cm x 33.6cm ARUCO markers at any of its 3 corners (preferrably with buffer space between the markers). All 3 markers (thus corners) must be seen by ZED during calibration.
- **Headset:** Any headset (preferrably the Hololens or the Quest 3) with camera feed access to scan the origin marker.

### Communication Protocol

Devices are connected through a LAN network (preferrably with firewall disabled). **Any device can start hosting a server and the rest can join.** The order of connection of devices does not matter and devices send data to eachothers directly (not mediated by a server/host).

To do this, we use Unity's Netcode for Game Object thus all the code must be in the same Unity project (To execute the code for a specific device (Vive trackers, ZED, or headset), we pick the device manually before running).

## Calibration

As different devices start with different origins and different relative transforms (position and rotation) to their own origins in the game scene, devices do not know where eachothers are.

To fix this, each device must figure out the relative transform between it and the origin marker in the game scene, then move and rotate the virtual (virtual = in the game scene) origin marker to Unity's origin all the while preserving said relative transform.

### ZED Calibration

ZED detects the relative transform using OpenCV plugin for Unity. It (re)calibrates as soon as it has connected to a host (or started as a host) or when the operator presses the "E" key on the keyboard.

The calibration algorithm basically sample the origin marker's transforms a few times. If there are too many outlier samples, we restart the calibration. If not, we use the samples to calculate the marker's average transform for calibration.

After ZED is (re)calibrated, the large display is calibrated right after. First, 3 markers appears on the screen at 3 corners of the display as part of Unity's UI. Then, using a similar algorithm, we detect the virtual markers and construct a plane from these 3 points. This plane is used as a representation of the virtual large display.

### Vive Trackers Calibration

As virtual Vive trackers' transforms are relative to the virtual Vive headset's, to calibrate trackers means to calibrate the headset. We do this by placing one of the trackers X meters to the left and Y meters in front of the marker with X and Y predetermined such that the tracker is still within the detection zone of the base stations. The orientation of the tracker should match that of the marker as well. Then, the operator can click the "Calibrate" button to calibrate the trackers and headset positions.

Their virtual transforms are then synced with other devices.

_Note:_ As long as the base stations and the Vive headset do not move, the virtual Vive headset's transform remains the same. Thus, we can just save the calibration information from the previous session for use in the current session if the conditions are met.

### HMD(s) Calibration

Uses a similar algorithm to ZED's calibration. Can press the recalibration button to recalibrate if necessary.

## Base Interactions

As we have already mapped everything into the virtual world, we will now only refer to virtual objects unless stated otherwise.

### Direct Pointing

With the trackers strapped to the spectators' hand, we can do a simple raycast from the tracker forward to simulate pointing at something. A crosshair will appear at the spot the user is pointing at on the virtual object (or the virtual floor). As the user points **directly** at the virtual object's position in the real world, this is called **direct pointing**. The crosshair is synced across devices so the headset users can see it.

![Direct pointing](https://github.com/LeStolz/XR-Interactive/blob/main/Docs/direct.png)

### Indirect Pointing

Spectators can point at a virtual object shown on the large display and a crosshair will be shown at the spot on that virtual object itself (not on the virtual object shown on the display) that the spectator points at (you can imagine the large display as a portal connecting the real world and the virtual world allowing the spectator to point into the virtual world from the real world).

As what is shown on the large display is the ZED's camera feed, a screen point (think of it as a pixel) on the large display should corresponds to the screen point on the ZED's feed of the camera with the same coordinates (assuming the origin is at the bottom left of each screen). The same argument can be made for the virtual large display and the ZED virtual camera (provided by the Unity ZED SDK which has the same parameters as the real camera).

Thus, by calculating the 2D coordinates of the crosshair on the virtual display relative to the display's origin, we can deduce the coordinates of the screen point on the virtual camera feed. Then, we just need to call the virtual ZED camera's `ScreenPointToRay` function to cast a ray from the camera to the virtual point that corresponds to the screen point on the ZED camera. This point is the virtual point that the spectator is pointing at through the large display.

![Indirect pointing](https://github.com/LeStolz/XR-Interactive/blob/main/Docs/indirect.png)

## Measurements

- Center of marker to side: 41.5cm.
- Side length of tile: 45.5cm.
- Cubes: 6 / 2 \* 8 distinct cubes with distinct faces in each cube.

## Setup

- Download OpenCVForUnity.

## TODO

- Screen/Real/ObjScreen/ObjReal + Duration + Time to complete task for 3. file name
- Fix zed, lighting.
- Fishing rod pointing. 3x2

- VR, VR view for ZED.
- 2 ZED.
