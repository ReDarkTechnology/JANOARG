using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineOptionsPanel : MonoBehaviour
{
    [Header("Fields")]
    public float Speed = 1;
    public int SeparationFactor = 2;

    public LaneFilterMode LaneFilterMode = LaneFilterMode.All;
    public float VerticalScale = 1;
    public float VerticalOffset = 0;
    public float NewHitObjectLength = 0.25f;

    public bool FollowSeekLine = true;
    public int WaveformIdle = 1;
    public int WaveformMode = 1;

    [Header("Objects")]
    public TMP_InputField NewHitObjectLengthField;
    public TMP_InputField VerticalScaleField;
    public TMP_InputField VerticalOffsetField;
    public RectTransform LaneFilterModeButton;
    public GameObject LaneFilterModeInactiveIcon;
    public GameObject LaneFilterModeActiveIcon;
    public TMP_InputField SpeedField;
    public TMP_InputField SeparatorField;
    public Toggle FollowSeekLineToggle;
    public ToggleGroup WaveformIdleToggleGroup;
    public List<Toggle> WaveformIdleToggles;
    public ToggleGroup WaveformModeToggleGroup;
    public List<Toggle> WaveformModeToggles;

    bool recursionBuster;
    bool isDirty;

    public void Init()
    {
        GetValues();
        SetValues();
        UpdateUI();
    }

    public void OnEnable()
    {
        recursionBuster = true;
        UpdateFields();
        UpdateUI();
        recursionBuster = false;
    }

    public void OnDisable()
    {
        if (isDirty)
        {
            isDirty = false;
            Storage str = Chartmaker.PreferencesStorage;

            str.Set("PB:Speed", Speed);
            str.Set("TL:SeparationFactor", SeparationFactor);

            str.Set("TL:LaneFilterMode", LaneFilterMode);
            str.Set("TL:HOVerticalScale", VerticalScale);
            str.Set("TL:HOVerticalOffset", VerticalOffset);
            str.Set("TL:NewHitObjectLength", NewHitObjectLength);

            str.Set("TL:FollowSeekLine", FollowSeekLine);
            str.Set("TL:WaveformIdle", WaveformIdle);
            str.Set("TL:WaveformMode", WaveformMode);
            Chartmaker.main.StartSavePrefsRoutine();
        }
    }

    public void GetValues()
    {
        Storage str = Chartmaker.PreferencesStorage;

        Speed = str.Get("PB:Speed", 1f);
        SeparationFactor = str.Get("TL:SeparationFactor", 2);

        LaneFilterMode = str.Get("TL:LaneFilterMode", LaneFilterMode.All);
        VerticalScale = str.Get("TL:HOVerticalScale", 1f);
        VerticalOffset = str.Get("TL:HOVerticalOffset", 0f);
        NewHitObjectLength = str.Get("TL:NewHitObjectLength", 0.25f);

        FollowSeekLine = str.Get("TL:FollowSeekLine", true);
        WaveformIdle = str.Get("TL:WaveformIdle", 1);
        WaveformMode = str.Get("TL:WaveformMode", 1);
    }

    public void SetValues()
    {
        Chartmaker.main.SongSource.pitch = Speed;
        bool timelineDirty = false;
        if (TimelinePanel.main.SeparationFactor != SeparationFactor)
        {
            TimelinePanel.main.SeparationFactor = SeparationFactor;
            timelineDirty = true;
        }
        if (TimelinePanel.main.LaneFilterMode != LaneFilterMode)
        {
            TimelinePanel.main.LaneFilterMode = LaneFilterMode;
            timelineDirty = true;
        }
        if (TimelinePanel.main.VerticalScale != VerticalScale)
        {
            TimelinePanel.main.VerticalScale = VerticalScale;
            timelineDirty = true;
        }
        if (TimelinePanel.main.VerticalOffset != VerticalOffset)
        {
            TimelinePanel.main.VerticalOffset = VerticalOffset;
            timelineDirty = true;
        }
        if (timelineDirty && (Chartmaker.main.CurrentSong != null)) TimelinePanel.main.UpdateTimeline(true);
    }

    public void UpdateFields()
    {
        SpeedField.text = Speed.ToString();
        SeparatorField.text = SeparationFactor.ToString();
        VerticalScaleField.text = VerticalScale.ToString();
        VerticalOffsetField.text = VerticalOffset.ToString();
        NewHitObjectLengthField.text = NewHitObjectLength.ToString();
        FollowSeekLineToggle.isOn = FollowSeekLine;
        for (int a = 0; a < WaveformIdleToggles.Count; a++) WaveformIdleToggles[a].isOn = a == WaveformIdle;
        for (int a = 0; a < WaveformModeToggles.Count; a++) WaveformModeToggles[a].isOn = a == WaveformMode;
    }

    public void UpdateUI() 
    {
        LaneFilterModeActiveIcon.SetActive(LaneFilterMode != LaneFilterMode.All);
        LaneFilterModeInactiveIcon.SetActive(LaneFilterMode == LaneFilterMode.All);
    }

    public void OnFieldSet()
    {
        if (recursionBuster) return;
        recursionBuster = true;

        float.TryParse(SpeedField.text, out Speed);
        int.TryParse(SeparatorField.text, out SeparationFactor);
        float.TryParse(VerticalScaleField.text, out VerticalScale);
        float.TryParse(VerticalOffsetField.text, out VerticalOffset);
        float.TryParse(NewHitObjectLengthField.text, out NewHitObjectLength);

        SeparationFactor = Mathf.Max(SeparationFactor, 2);
        VerticalScale = VerticalScale <= 0 ? 1 : VerticalScale;
        FollowSeekLine = FollowSeekLineToggle.isOn;

        bool waveformDirty = false;
        if (!WaveformIdleToggles[WaveformIdle].isOn) waveformDirty = true;
        for (int a = 0; a < WaveformIdleToggles.Count; a++) if (WaveformIdleToggles[a].isOn) WaveformIdle = a;
        if (!WaveformModeToggles[WaveformMode].isOn) waveformDirty = true;
        for (int a = 0; a < WaveformModeToggles.Count; a++) if (WaveformModeToggles[a].isOn) WaveformMode = a;

        if (waveformDirty)
        {
            TimelinePanel.main.DiscardWaveform();
        } 

        SetValues();
        recursionBuster = false;
        isDirty = true;
    }

    public void OpenAnalysisPrefs() 
    {
        ModalHolder.main.Spawn<PreferencesModal>().SetTab(5);
        GetComponent<PopupPanel>().Close();
    }

    public void OpenLaneFilterModePopup() 
    {
        ContextMenuListAction option(String label, LaneFilterMode value) {
            return new ContextMenuListAction(label, () => {
                LaneFilterMode = value;
                SetValues();
                UpdateUI();
                TimelinePanel.main.UpdateItems();
            }, _checked: LaneFilterMode == value);
        }
        ContextMenuHolder.main.OpenRoot(new ContextMenuList(
            option("Show all Lanes", LaneFilterMode.All),
            option("Show Lanes visible in the Hierarchy", LaneFilterMode.HierarchyVisible)
        ), LaneFilterModeButton, ContextMenuDirection.Up);
    }
}