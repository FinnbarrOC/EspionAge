﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
    public CinemachineBrain brain;

    // Cinemachine does not have built-in events for OnBlendingStart / OnBlendingComplete so we have our own events for that
    public delegate void BlendingStartAction(CinemachineVirtualCamera fromCamera, CinemachineVirtualCamera toCamera);
    public delegate void BlendingCompleteAction(CinemachineVirtualCamera fromCamera, CinemachineVirtualCamera toCamera);
    public event BlendingStartAction OnBlendingStart;
    public event BlendingCompleteAction OnBlendingComplete;

    private void Start()
    {
        if (!brain && !(brain = FindObjectOfType<CinemachineBrain>()))
        {
            Utils.LogErrorAndStopPlayMode($"{name} needs a CinemachineBrain component! Could not fix automatically.");
        }
    }

    public ICinemachineCamera GetActiveCamera()
    {
        return brain.ActiveVirtualCamera;
    }

    public bool IsActiveCameraValid()
    {
        return GetActiveCamera() != null && GetActiveCamera().IsValid;
    }

    public Transform GetActiveCameraTransform()
    {
        return GetActiveCamera().VirtualCameraGameObject.transform;
    }

    public void BlendTo(CinemachineVirtualCamera blendToCamera)
    {
        // This can fail when we first start the game, so lets check for it
        if (!IsActiveCameraValid())
        {
            return;
        }

        CinemachineVirtualCamera fromCamera = GetActiveCamera().VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        if (!fromCamera || blendToCamera == fromCamera)
        {
            return;  // early exit, because we cannot blend between the same cameras??
        }

        OnBlendingStart?.Invoke(fromCamera, blendToCamera);  // alert all listeners that we started blending some camera

        // Set the camera for this zone to active
        blendToCamera.gameObject.SetActive(true);

        // Whatever the current active camera is, deactivate it
        fromCamera.gameObject.SetActive(false);

        // The CinemachineBrain in the scene should handle the blending between these two cameras if we set up a custom blend already

        StartCoroutine(WaitForBlendFinish(fromCamera, blendToCamera));
    }

    // This is just here so we can alert listeners that we have finished blending
    //  and it seems like the only way to do this is to keep checking if the last camera is still live or not
    private IEnumerator WaitForBlendFinish(CinemachineVirtualCamera waitForCamera, CinemachineVirtualCamera toCamera)
    {
        while(CinemachineCore.Instance.IsLive(waitForCamera))
        {
            yield return null;  // same as FixedUpdate
        }

        OnBlendingComplete?.Invoke(waitForCamera, toCamera);  // alert all listeners that we stopped blending some camera

        yield return null;
    }
}
