﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.XR;
#endif



/// <summary>
/// Extended version of ZEDControllerTracker that also checks for several inputs in a generic way.
/// You can check a state with
/// Used because input methods vary a lot between controllers and between SteamVR (new and old) and Oculus.
/// See base class ZEDControllerTracker for any code that don't directly relate to inputs.
/// </summary>
public class ZEDControllerTracker_DemoInputs : ZEDControllerTracker
{
    //#if ZED_STEAM_VR
#if ZED_SVR_2_0_INPUT



#elif ZED_STEAM_VR
    /// <summary>
    /// Legacy SteamVR button to cause a Fire event when checked or subscribed to.
    /// </summary>
    [Header("SteamVR Legacy Input Bindings")]
    [Tooltip("Legacy SteamVR button to cause a Fire event when checked or subscribed to.")]
    public EVRButtonId fireBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR button to cause a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Click event when checked or subscribed to.")]
    public EVRButtonId clickBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR button to cause a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Back event when checked or subscribed to.")]
    public EVRButtonId backBinding_Legacy = EVRButtonId.k_EButton_Grip;
    /// <summary>
    /// Legacy SteamVR button to cause a Grip event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Grip event when checked or subscribed to.")]
    public EVRButtonId grabBinding_Legacy = EVRButtonId.k_EButton_SteamVR_Trigger;
    /// <summary>
    /// Legacy SteamVR axis to cause a Vector2 Navigate UI event when checked or subscribed to.
    /// </summary>
    [Tooltip("Legacy SteamVR button to cause a Vector2 Navigate UI event when checked or subscribed to.")]
    public EVRButtonId navigateUIBinding_Legacy = EVRButtonId.k_EButton_Axis0;
#endif

#if ZED_OCULUS

    public static bool ovrUpdateCalledThisFrame = false;
#if UNITY_2019_3_OR_NEWER
    /// <summary>
    /// Input Button checked to signal a Fire event when checked or subscribed to.
    /// </summary>
    [Header("Input Bindings")]
    [Tooltip("Input Button checked to signal a Fire event when checked or subscribed to")]
    public InputFeatureUsage<bool> fireButton = CommonUsages.triggerButton;
    /// <summary>
    /// Input Button checked to signal a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Click event when checked or subscribed to")]
    public InputFeatureUsage<bool> clickButton = CommonUsages.triggerButton;
    /// <summary>
    /// Input Button checked to signal a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Back event when checked or subscribed to")]
    public InputFeatureUsage<bool> backButton = CommonUsages.secondaryButton; //Y, or B if just right controller is connected.
    /// <summary>
    /// Input Button checked to signal a Grab event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Grab event when checked or subscribed to")]
    public InputFeatureUsage<bool> grabButton = CommonUsages.gripButton;
    /// <summary>
    /// Input Button checked to signal a Vector2 UI navigation event when checked or subscribed to.
    /// </summary>
    [Tooltip("Input Button checked to signal a Vector2 UI navigation event when checked or subscribed to")]
    public InputFeatureUsage<Vector2> navigateUIAxis = CommonUsages.primary2DAxis;

    private bool fireActive = false;
    private bool clickActive = false;
    private bool backActive = false;
    private bool grabActive = false;

#else
    /// <summary>
    /// Oculus Button checked to signal a Fire event when checked or subscribed to.
    /// </summary>
    [Header("Oculus Input Bindings")]
    [Tooltip("Oculus Button checked to signal a Fire event when checked or subscribed to")]
    public OVRInput.Button fireButton = OVRInput.Button.PrimaryIndexTrigger;
    /// <summary>
    /// Oculus Button checked to signal a Click event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Click event when checked or subscribed to")]
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;
    /// <summary>
    /// Oculus Button checked to signal a Back event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Back event when checked or subscribed to")]
    public OVRInput.Button backButton = OVRInput.Button.Two; //Y, or B if just right controller is connected.
    /// <summary>
    /// Oculus Button checked to signal a Grab event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Grab event when checked or subscribed to")]
    public OVRInput.Button grabButton = OVRInput.Button.PrimaryHandTrigger;
    /// <summary>
    /// Oculus Button checked to signal a Vector2 UI navigation event when checked or subscribed to.
    /// </summary>
    [Tooltip("Oculus Button checked to signal a Vector2 UI navigation event when checked or subscribed to")]
    public OVRInput.Axis2D navigateUIAxis = OVRInput.Axis2D.PrimaryThumbstick;
#endif
#endif
    /// <summary>
    /// Events called when the Fire button/action was just pressed.
    /// </summary>
    [Header("Events")]
    [Space(5)]
    [Tooltip("Events called when the Fire button/action was just pressed.")]
    public UnityEvent onFireDown;
    /// <summary>
    /// Events called when the Fire button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Fire button/action was just released.")]
    public UnityEvent onFireUp;
    /// <summary>
    /// Events called when the Click button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Click button/action was just pressed.")]
    public UnityEvent onClickDown;
    /// <summary>
    /// Events called when the Click button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Click button/action was just released.")]
    public UnityEvent onClickUp;
    /// <summary>
    /// Events called when the Back button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Back button/action was just pressed.")]
    public UnityEvent onBackDown;
    /// <summary>
    /// Events called when the Back button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Back button/action was just released.")]
    public UnityEvent onBackUp;
    /// <summary>
    /// Events called when the Grab button/action was just pressed.
    /// </summary>
    [Tooltip("Events called when the Grab button/action was just pressed.")]
    public UnityEvent onGrabDown;
    /// <summary>
    /// Events called when the Grab button/action was just released.
    /// </summary>
    [Tooltip("Events called when the Grab button/action was just released.")]
    public UnityEvent onGrabUp;

    /// <summary>
    /// Returns if the Fire button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckFireButton(ControllerButtonState state)
    {


#if ZED_OCULUS
#if UNITY_2019_3_OR_NEWER
        return CheckButtonState(fireButton, state, fireActive);
#else
        return CheckOculusButtonState(fireButton, state);
#endif
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Click button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckClickButton(ControllerButtonState state)
    {

#if ZED_OCULUS
#if UNITY_2019_3_OR_NEWER
        return CheckButtonState(clickButton, state, clickActive);
#else
        return CheckOculusButtonState(clickButton, state);
#endif
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Back button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckBackButton(ControllerButtonState state)
    {

#if ZED_OCULUS
#if UNITY_2019_3_OR_NEWER
        return CheckButtonState(backButton, state, backActive);
#else
        return CheckOculusButtonState(backButton, state);
#endif
#endif
        return false;
    }

    /// <summary>
    /// Returns if the Grab button/action matched the provided state.
    /// </summary>
    /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    public bool CheckGrabButton(ControllerButtonState state)
    {

#if ZED_OCULUS
#if UNITY_2019_3_OR_NEWER
        return CheckButtonState(grabButton, state, grabActive);
#else
        return CheckOculusButtonState(grabButton, state);
#endif
#endif
        return false;
    }

    /// <summary>
    /// Returns the current 2D axis value of the NavigateUIAxis button/action.
    /// </summary>
    public Vector2 CheckNavigateUIAxis()
    {

#if ZED_OCULUS
#if UNITY_2019_3_OR_NEWER
        return Check2DAxisState(navigateUIAxis);
#else
        return CheckOculus2DAxisState(navigateUIAxis);
#endif
#endif
        return Vector3.zero;
    }

    protected override void Awake()
    {
        base.Awake();

        // #if ZED_SVR_2_0_INPUT
        //         if (!useLegacySteamVRInput)
        //         {
        //             if (!SteamVR.active) SteamVR.Initialize(true); //Force SteamVR to activate, so we can use the input system.

        //             //script binding example
        //             //fireBinding = SteamVR_Input._default.inActions.GrabGrip; //...
        //         }
        // #endif
    }

    protected override void Update()
    {
        base.Update();

        if (CheckClickButton(ControllerButtonState.Down)) onClickDown.Invoke();
        if (CheckClickButton(ControllerButtonState.Up)) onClickUp.Invoke();
        if (CheckFireButton(ControllerButtonState.Down)) onFireDown.Invoke();
        if (CheckFireButton(ControllerButtonState.Up)) onFireUp.Invoke();
        if (CheckBackButton(ControllerButtonState.Down)) onBackDown.Invoke();
        if (CheckBackButton(ControllerButtonState.Up)) onBackUp.Invoke();
        if (CheckGrabButton(ControllerButtonState.Down)) onGrabDown.Invoke();
        if (CheckGrabButton(ControllerButtonState.Up)) onGrabUp.Invoke();

    }

    protected void LateUpdate()
    {
#if ZED_OCULUS
        ovrUpdateCalledThisFrame = false;
#endif
    }

    // #if ZED_STEAM_VR
    //     protected override void UpdateControllerState()
    //     {
    //         base.UpdateControllerState();

    //         //If using legacy SteamVR input, we check buttons directly from the OpenVR API.
    // #if ZED_SVR_2_0_INPUT //If using SteamVR plugin 2.0 or higher, give the option to use legacy input.
    //         if (useLegacySteamVRInput)
    //         {
    //             openvrsystem.GetControllerState((uint)index, ref controllerstate, controllerstatesize);
    //         }
    // #else //We're using an older SteamVR plugin, so we need to use the legacy input.
    //         openvrsystem.GetControllerState((uint)index, ref controllerstate, controllerstatesize);
    // #endif
    //     }
    // #endif

    // #if ZED_OCULUS
    //     /// <summary>
    //     /// Checks the button state of a given Oculus button.
    //     /// </summary>
    //     /// <param name="state">Whether to check if the button/action is just pressed, just released, or is being held down.</param>
    //     public bool CheckOculusButtonState(OVRInput.Button button, ControllerButtonState state)
    //     {
    //         if (!ovrUpdateCalledThisFrame)
    //         {
    //             OVRInput.Update();
    //             ovrUpdateCalledThisFrame = true;
    //         }
    //         bool result = false;
    //         switch (state)
    //         {
    //             case ControllerButtonState.Down:
    //                 result = OVRInput.GetDown(button, GetOculusController());
    //                 break;
    //             case ControllerButtonState.Held:
    //                 result = OVRInput.Get(button, GetOculusController());
    //                 break;
    //             case ControllerButtonState.Up:
    //                 result = OVRInput.GetUp(button, GetOculusController());
    //                 break;
    //         }
    //         return result;
    //     }

    // #if UNITY_2019_3_OR_NEWER
    //     public bool CheckButtonState(InputFeatureUsage<bool> button, ControllerButtonState state, bool isActive){

    //         bool down = false;
    //         bool up = false;
    //         InputDevice device = new InputDevice();

    //         if (deviceToTrack == Devices.LeftController)
    //             device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    //         else device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

    //         ProcessInputDeviceButton(device, button, ref isActive,
    //             () => // On Button Down
    //             {
    //                 down = true;
    //             },
    //             () => // On Button Up
    //             {
    //                 up =  true;
    //         });

    //         if (state == ControllerButtonState.Down) return down;
    //         if (state == ControllerButtonState.Up) return up;
    //         else return false;
    //     }

    //     public Vector2 Check2DAxisState(InputFeatureUsage<Vector2> navigateUIAxis){

    //         InputDevice device = new InputDevice();

    //         if (deviceToTrack == Devices.LeftController)
    //             device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    //         else device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

    //         Vector2 result = Vector2.zero;
    //         if (device.TryGetFeatureValue(navigateUIAxis, out Vector2 value))
    //             result = value;

    //         return result;
    //     }

    //     private void ProcessInputDeviceButton(InputDevice inputDevice, InputFeatureUsage<bool> button, ref bool _wasPressedDownPreviousFrame, Action onButtonDown = null, Action onButtonUp = null, Action onButtonHeld = null)
    //     {
    //         if (inputDevice.TryGetFeatureValue(button, out bool isPressed) && isPressed)
    //         {
    //             if (!_wasPressedDownPreviousFrame) // // this is button down
    //             {
    //                 onButtonDown?.Invoke();
    //             }

    //             _wasPressedDownPreviousFrame = true;
    //             onButtonHeld?.Invoke();
    //         }
    //         else
    //         {
    //             if (_wasPressedDownPreviousFrame) // this is button up
    //             {
    //                 onButtonUp?.Invoke();
    //             }

    //             _wasPressedDownPreviousFrame = false;
    //         }
    //     }
    // #endif

    //     /// <summary>
    //     /// Returns the axis of a given Oculus axis button/joystick.
    //     /// </summary>
    //     public Vector3 CheckOculus2DAxisState(OVRInput.Axis2D axis)
    //     {
    //         if (!ovrUpdateCalledThisFrame)
    //         {
    //             OVRInput.Update();
    //             ovrUpdateCalledThisFrame = true;
    //         }

    //         return OVRInput.Get(axis, GetOculusController());
    //     }

    //     /// <summary>
    //     /// Returns the Oculus controller script of the controller currently attached to this object.
    //     /// </summary>
    //     public OVRInput.Controller GetOculusController()
    //     {
    //         if (deviceToTrack == Devices.LeftController) return OVRInput.Controller.LTouch;
    //         else if (deviceToTrack == Devices.RightController) return OVRInput.Controller.RTouch;
    //         else return OVRInput.Controller.None;
    //     }

    // #endif


    //#if ZED_STEAM_VR

}

/// <summary>
/// List of possible button states, used to check inputs.
/// </summary>
public enum ControllerButtonState
{
    /// <summary>
    /// The button was pressed this frame.
    /// </summary>
    Down,
    /// <summary>
    /// The button is being held down - it doesn't matter which frame it started being held.
    /// </summary>
    Held,
    /// <summary>
    /// The button was released this frame.
    /// </summary>
    Up
}
