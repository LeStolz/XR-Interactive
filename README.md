# XR Public Spectator Interactive

## Todo

- Local + auto connect.
- Fix Hololens, display and origin error in calibration.
- Less bandwidth.

## Setup

- Download OpenCVForUnity.

## Terminologies

- Player: The HMD user. There is only 1 Player.
- Spectator: The non-HMD user. There can be many Spectators.
- LSD: Large Screen Display.

## Current objective

- The POV of what is shown on the LSD is the POV of the Spectators for now.

- Spectators interactions:
  - When the spectators point their Vive handlers (or tracker, can use ImprovedEuroFilter) at the LSD, a preview trajectory and a preview "hit point" (both of these are in the mixed reality world) should be shown on the LSD. As these are previews, the Player won't see these.
  - They can then hold the trigger to make the preview visible to the Player.
  - Or they can click the trigger to place a temporary marker at the "hit point".

## Implementation direction draft

### Detecting the large display

We must detect the LSD in order to... know where it is and to map it in the mixed reality world.

- 2 markers at the 2 opposite corners of the screen.

### Displaying the mixed reality view on the LSD

- This is achieved using Zed.
- The Vive headset is placed right under the Zed to approximate the position of the Zed in the mixed reality world.

### Relative positions between the Player and the Spectators

- The Vive Pro headset will be placed right behind the Spectators. Their position can then be approximated by the headset's position.
- Thus, the 2 headsets (Player's and Spectators') can send eachothers positions via socket.
- We can then calculate their POV's.

### Spectators' interactions

- Implement a "portal" with 2 ends:
  - 1 end is the LSD in the real world (detected using markers or trackers).
  - The other is the LSD in the mixed reality world (which is the Vive headset's position in mixed reality space).
- With this, we can let the Spectators' handlers' "beams" pass through the "portal" into the mixed reality world.
