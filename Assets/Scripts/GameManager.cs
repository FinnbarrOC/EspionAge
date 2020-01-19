﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : Singleton<GameManager>
{
    public GameObject canRestUI;
    public TextMeshProUGUI restText;

    private string defaultRestText;
    private void Start()
    {
        defaultRestText = restText.text;
    }

    //// TEMP FUNCTION - Move to UIManager later
    public void EnableCanRestUI(bool toEnable)
    {
        canRestUI.SetActive(toEnable);
    }

    // TEMP FUNCTION - Move to UIManager later
    public void UpdateRestingText(bool isResting)
    {
        restText.text = (isResting ? "Resting..." : defaultRestText);
    }
}