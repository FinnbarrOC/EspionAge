﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveList : Singleton<ObjectiveList>
{
    private Image background;
    private TMP_Text objectiveText;
    private Animator root;

    // Start is called before the first frame update
    void Start()
    {
        background = GetComponentInChildren<Image>();
        objectiveText = GetComponentInChildren<TMP_Text>();
        root =  GetComponentInChildren<Animator>();
        
        HideObjectiveList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisplayObjectiveList()
    {
        root.gameObject.SetActive(true);
    }

    public void DisplayObjectiveText(string textToSet)
    {
        DisplayObjectiveList();
        
        if (objectiveText != null) 
        {   
            objectiveText.text = textToSet;
            
        }
    }

    public void CrossOutObjectiveText()
    {
        objectiveText.text = "<s>" + objectiveText.text + "</s>";
    }

    public void HideObjectiveList()
    {
        root.gameObject.SetActive(false);
    }
}