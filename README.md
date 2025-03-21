# XR Public Spectator Interactive

## Todo

- connect, reconnect. LobbyManager, ExampleNetworkDiscovery, LobbyUI, NetworkGameManager, LobbyListSlotUI, Utils, ZEDModelManager, Portal
- Fix trackers, Hololens, display and origin error in calibration.

## Setup

- Download OpenCVForUnity.

## Measurements

- Center of marker to side: 41.5cm.
- Side length of tile: 45.5cm.
- Side length of floor marker: 43.3cm.
- Side length of portal marker: 34cm.

## Implementation

The following calibration steps do not need to be in order.

### Calibrate ZED

- Detect origin and floor using marker with ID 0 on the floor.
- _Then_ detect large display using 3 markers at 3 corners of the screen shown by the projector.

### Calibrate Vive trackers

- Detect origin by placing one of the trackers at an offset of some meters from the ID 0 marker.
- Position and rotation is saved for subsequent uses.

### Calibrate HMD(s)

- Detect origin using marker with ID 0 on the floor.

### Spectators' interactions

- Implement a "portal" with 2 ends:
  - 1 end is the large display.
  - The other is the ZED camera.
- As the content on the large display is from the POV of the ZED, we can let the Spectators' "beams" pass through the "portal" into the mixed reality world seamlessly.
