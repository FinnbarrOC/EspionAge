﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoisePinger : MonoBehaviour
{
    [Header("General")]
    public GameObject pingPrefab;
    public float pingRadius = 10.0f;
    
    [Header("Tweaks")]
    public float pingGrowthScale = 20.0f;
    public float pingFloorOffset = 0.5f;

    [Header("FMOD Audio")]
    [FMODUnity.EventRef]
    public string breakable;
    
    public void SpawnNoisePing(Collision other)
    {
        FMODUnity.RuntimeManager.PlayOneShot(breakable, transform.position);
        Vector3 hitPoint = other.GetContact(0).point;
        hitPoint.y = pingFloorOffset;
        
        GameObject pingInstance = Instantiate(pingPrefab, hitPoint, Quaternion.identity);
        Utils.GetRequiredComponent<NoisePing>(pingInstance).Initialize(pingRadius, pingGrowthScale);
    }
    
    void OnDrawGizmos()
    {
        // pingRadius visualization
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pingRadius);
    }
}
