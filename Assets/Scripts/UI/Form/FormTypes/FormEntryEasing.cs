using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryEasing : FormEntry<IEaseDirective>
{
    public TMP_Text ValueLabel;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    public void Reset()
    {
        ValueLabel.text = CurrentValue.ToString();
    }

    public void OpenPicker()
    {
        EasingPicker.main.CurrentEasing = CurrentValue;
        EasingPicker.main.Open();
        EasingPicker.main.OnSet = () => {
            SetValue(EasingPicker.main.CurrentEasing);
            Reset();
        };
    }
}