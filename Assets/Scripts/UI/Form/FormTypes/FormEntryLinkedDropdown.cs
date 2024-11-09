using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryLinkedDropdown<T> : FormEntryDropdown<T>
{
    public Button LinkButton;
}

public class FormEntryLinkedDropdown : FormEntryDropdown
{
    public Button LinkButton;
}