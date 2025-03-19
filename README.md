# XR Public Spectator Interactive

## Todo

- Local + auto connect.
- Fix trackers (rewrite readme), Hololens, display and origin error in calibration.
- Less bandwidth 3.

## Setup

- Download OpenCVForUnity.

## Implementation

The following calibration steps do not need to be in order.

### Calibrate ZED

- Detect origin and floor using marker with ID 0 on the floor.
- _Then_ detect large display using 3 markers at 3 corners of the screen shown by the projector.

### Calibrate Vive trackers

- Headset is placed right under ZED.
- By subtracting the y-coordinate of ZED by some meters, we get the position of the headset.
- By setting the forward vector of the headset to be in the same plane as ZED's, we get its orientation.

### Calibrate HMD(s)

- Detect origin using marker with ID 0 on the floor.

### Spectators' interactions

- Implement a "portal" with 2 ends:
  - 1 end is the large display.
  - The other is the ZED camera.
- As the content on the large display is from the POV of the ZED, we can let the Spectators' "beams" pass through the "portal" into the mixed reality world seamlessly.
