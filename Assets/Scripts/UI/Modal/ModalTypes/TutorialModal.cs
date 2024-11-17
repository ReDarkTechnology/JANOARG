using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TutorialModal : Modal
{
    public static TutorialModal main;

    public Tutorial CurrentTutorial { get; private set; }
    public int CurrentStep { get; private set; }

    public TMP_Text TitleLabel;

    [Space]
    public GameObject ListSectionHolder;
    public TutorialEntry EntrySample;
    public RectTransform EntryHolder;

    [Space] 
    public GameObject TutorialSectionHolder;
    public Slider TutorialProgress;
    public TMP_Text ContentLabel;
    public GameObject NextButton;
    public TMP_Text NextConditionLabel;
    public GameObject LastStepActionsHolder;
    public GameObject NextTutorialButton;
    public TMP_Text NextTutorialLabel;

    [Space] 
    public RectTransform CurrentFocusItem;
    public RectTransform FocusIndicator;
    public Graphic FocusIndicator2;

    public void Awake()
    {
        if (main) Close();
        else main = this;
    }
    
    public new void Start()
    {
        base.Start();
        transform.SetParent(ModalHolder.main.PriorityModalHolder);
        FocusIndicator.SetParent(transform.parent);
        foreach (Tutorial tut in Tutorials.tutorials) 
        {
            TutorialEntry entry = Instantiate(EntrySample, EntryHolder);
            entry.Label.text = tut.Name;
            entry.Button.onClick.AddListener(() => StartTutorial(tut));
        }
        UpdateUI();
    }

    public void OnDestroy() 
    {
        Destroy(FocusIndicator.gameObject);
    }

    public void Update() {
        var condition = CurrentTutorial?.Steps[CurrentStep].NextCondition;
        if (condition != null && condition()) NextStep();

        float lerp = 1 - Mathf.Pow(0.01f, Time.deltaTime);
        TutorialProgress.value = Mathf.Lerp(TutorialProgress.value, CurrentStep, lerp);

        FocusIndicator.gameObject.SetActive(CurrentFocusItem);
        if (CurrentFocusItem) 
        {
            Vector3[] corners = new Vector3[4];
            CurrentFocusItem.GetWorldCorners(corners);
            Vector2 center = (corners[0] + corners[2]) / 2;
            Vector2 size = corners[2] - corners[0];

            FocusIndicator.position = Vector2.Lerp(FocusIndicator.position, center, lerp);
            FocusIndicator.sizeDelta = Vector2.Lerp(FocusIndicator.sizeDelta, size + new Vector2(4, 4), lerp);
            float lerp2 = 1 - Mathf.Pow(0.02f, Time.deltaTime);
            FocusIndicator2.rectTransform.position = Vector2.Lerp(FocusIndicator2.rectTransform.position, center, lerp2);
            FocusIndicator2.rectTransform.sizeDelta = Vector2.Lerp(FocusIndicator2.rectTransform.sizeDelta, size + new Vector2(8, 8), lerp2);
            FocusIndicator2.color = Color.HSVToRGB(Time.time % 1, 1, 1);
        }
    }

    public void UpdateUI() 
    {
        bool isActive = CurrentTutorial != null;
        ListSectionHolder.SetActive(!isActive);
        TutorialSectionHolder.SetActive(isActive);
        TitleLabel.text = isActive 
            ? CurrentTutorial.Name 
            : "Tutorials";
        CurrentFocusItem = null;
        if (isActive)
        {
            TutorialStep step = CurrentTutorial.Steps[CurrentStep];
            bool isLastStep = CurrentStep >= CurrentTutorial.Steps.Length - 1;
            TutorialProgress.maxValue = CurrentTutorial.Steps.Length - 1;
            ContentLabel.text = step.Content;
            NextButton.gameObject.SetActive(step.NextCondition == null && !isLastStep);
            NextConditionLabel.gameObject.SetActive(step.NextCondition != null);
            NextConditionLabel.text = step.NextConditionLabel;
            LastStepActionsHolder.SetActive(isLastStep);

            if (!string.IsNullOrWhiteSpace(step.FocusItemPath)) 
            {
                string[] paths = step.FocusItemPath.Split("->");
                GameObject obj = GameObject.Find(paths[0]);
                CurrentFocusItem = obj ? (RectTransform)obj.transform : null;
                for (int i = 1; i < paths.Length; i++) 
                {
                    CurrentFocusItem = (RectTransform)FindRecursive(CurrentFocusItem, paths[i]);
                    if (!CurrentFocusItem) break;
                }
                FocusIndicator2.rectTransform.position = FocusIndicator.position = new Vector2(Screen.width, Screen.height) / 2;
                FocusIndicator2.rectTransform.sizeDelta = FocusIndicator.sizeDelta = new Vector2(Screen.width, Screen.height);
            }
            Debug.Log(CurrentFocusItem);

            if (isLastStep) 
            {
                int index = Array.IndexOf(Tutorials.tutorials, CurrentTutorial);
                bool isLastTutorial = index + 1 >= Tutorials.tutorials.Length;

                NextTutorialButton.SetActive(!isLastTutorial);
                if (!isLastTutorial) NextTutorialLabel.text = "Continue to " + Tutorials.tutorials[index + 1].Name;
            }
        }
    }

    public void StartTutorial(Tutorial tutorial)
    {
        if (tutorial.Checker(() => StartTutorial(tutorial)))
        {
            CurrentTutorial = tutorial;
            TutorialProgress.value = CurrentStep = 0;
            UpdateUI();
        }
    }

    public void StartNextTutorial()
    {
        int index = Array.IndexOf(Tutorials.tutorials, CurrentTutorial);
        if (index + 1 >= Tutorials.tutorials.Length) return;
        StartTutorial(Tutorials.tutorials[index + 1]);
    }

    public void NextStep() 
    {
        CurrentStep++;
        base.Start();
        UpdateUI();
    }

    Transform FindRecursive(Transform parent, string name)
    {
        if (parent == null) return null;

        Transform res = parent.Find(name);
        if (res) return res;

        foreach (Transform child in parent) 
        {
            res = FindRecursive(child, name);
            if (res) return res;
        }

        return null;
    }
}

public class Tutorial
{
    public string Name;
    public Func<Action, bool> Checker;
    public TutorialStep[] Steps;

    public Tutorial (string name, Func<Action, bool> checker, TutorialStep[] steps)
    {
        Name = name;
        Checker = checker;
        Steps = steps;
    }
}

public class TutorialStep
{
    public string Content;
    public string FocusItemPath;
    public Func<bool> NextCondition;
    public string NextConditionLabel;

    public TutorialStep (string content)
    {
        Content = content;
    }

    public TutorialStep (string content, Func<bool> nextCon, string nextLabel) 
    {
        Content = content;
        NextCondition = nextCon;
        NextConditionLabel = nextLabel;
    }

    public TutorialStep (string content, string focusPath)
    {
        Content = content;
        FocusItemPath = focusPath;
    }

    public TutorialStep (string content, string focusPath, Func<bool> nextCon, string nextLabel) 
    {
        Content = content;
        FocusItemPath = focusPath;
        NextCondition = nextCon;
        NextConditionLabel = nextLabel;
    }
}

public class Tutorials 
{
    public static Tutorial[] tutorials = new [] {
        new Tutorial (
            "Intro & Opening a Playable Song",
            onHomeCondition,
            new TutorialStep[] {
                new (
                    "<b>Welcome to the JANOARG Chartmaker!</b>"
                    + "\n\nThis interactive tutorial series will guide you on how to create and edit charts for the hit rhythm game JANOARG."
                    + "\n\nTo advance the tutorial, press the Continue buttons below the instruction text."
                ),
                new (
                    "Sometimes, items on the screen will be highlighted with an animated border.\n(for example, the Continue button below has been highlighted)"
                    + "\n\nSome other times, you'll be asked to do certain things in order for the tutorial to advance."
                    + "\n\nYou can drag the tutorial overlay by dragging the title bar by the way, you might want to do this if this modal is obscuring stuff on the screen."
                    + "\n\nFor the first tutorial, you'll learn how Playable Songs work and how to open them for charting.",
                    "Tutorial Modal(Clone)->Continue"
                ),
                new (
                    "So first, some basics on how the game's chart format works."
                    + "\n\nIf you've played rhythm games before, you'd see that they commonly offer multiple difficulties to choose for a song, and JANOARG is nothing different."
                    + "\n\nIn order to do this, we create <i>Playable Songs</i> to represent, well, playable songs that are in the game. Each one of them can contain multiple <i>Charts</i> that equate to multiple difficulties of that song."
                    + "\n\nTake note of the terminology, it is important!"
                ),
                new (
                    "When you start up the Chartmaker, the first thing you'll see is the Home modal, which lets you choose which song to open for charting."
                    + "\n\nHere, you'll find options to:\na) create a Playable Song,\nb) pick an existing Playable Song file from storage to open, and\nc) reopen a Playable Song that you've opened previously."
                    + "\n\n<i><size=8>(you also have the option to quit the Chartmaker here too but well you can always use the close window button)",
                    "Home Modal(Clone)"
                ),
                new (
                    "Let's take a deep dive into Playable Songs!"
                    + "\n\nFor this we'll need to open the \"New Playable Song\" modal by pressing this highlighted button.",
                    "Home Modal(Clone)->Options/New",
                    () => NewSongModal.main,
                    "(click on the button to continue)"
                ),
                new (
                    "This is the \"New Playable Song\" prompt modal, which you'll need to fill in order to create Playable Songs!"
                    + "\n\nIt'll ask you for some information about the song you're going to chart.",
                    "New Song Modal(Clone)"
                ),
                new (
                    "First is the song's \"codename\", which determines the name of the .japs file and the folder that contains it."
                    + "\n\nCodenames are locked to only be able to have alphanumeric characters because some special characters like \"\\\" would make some computers' file system managers not happy."
                    + "\n\nYou should think of an unique name for this, since if there's already a song in the Songs folder with the same codename, it will try to override the old song's data which is not a good thing actually.",
                    "New Song Modal(Clone)->Codename"
                ),
                new (
                    "Of course, to chart a song, you will need a copy of it."
                    + "\n\nIf your audio file ends with \".mp3\", \".wav\", or \".ogg\"* you should be good to go."
                    + "\n\n<i><size=8>(*there are actually multiple audio formats that use the same \".ogg\" extension so not all of them would work with the Chartmaker. The audio format that the Chartmaker knows is Vorbis--if you open the \".ogg\" file with a text editor one of the things you should see out of seemingly random characters is the word \"vorbis\")",
                    "New Song Modal(Clone)->Audio File"
                ),
                new (
                    "Here is the Metadata section, the place where you'd enter information like the name of the song and the name of the people who composed it."
                    + "\n\nYou might've noticed that there are two fields for the song name. That's because a song in JANOARG can have two names! For songs whose names aren't in Latin characters (like Japanese) those are the original name and the alternative Latinized name. JANOARG will display one of them depending on the player's language and name preferences."
                    + "\n\nIf the song actually doesn't have an alternative name you can leave the alt name field blank.",
                    "New Song Modal(Clone)->Metadata"
                ),
                new (
                    "If you want to skip these, you can press the conveniently placed \"Auto-fill\" button and JANOARG will try to fill some of the fields if there is metadata information in the audio file."
                    + "\n\nOnly \".mp3\" and \".ogg\" files are supported for now, hence the \"Experimental\" label.",
                    "New Song Modal(Clone)->Auto-fill (Experimental)"
                ),
                new (
                    "The Colors here is used for a number of things, for example the background color here will be used as the default background color of the song illustration."
                    + "\n\n<i><size=8>(you don't declare the song's illustration here, we'll get to that later)</size></i>"
                    + "\n\nIf your song has an album cover, you could pick the colors from there and use it here.",
                    "New Song Modal(Clone)->Colors"
                ),
                new (
                    "And of course, here you can define the song's timing information."
                    + "\n\nAs timing the notes to the song is crucial for any rhythm game, you should make sure you got this correctly before you do any of the charting."
                    + "\n\nFor the record, you don't have to touch this--there are more tools to modify this once we get to the Editor screen, but if you already know the BPM of the song, you can enter the starting BPM here for convenience."
                    + "\n\nIf your song has an album cover, you could pick the colors from there and use it here.",
                    "New Song Modal(Clone)->Timing"
                ),
                new (
                    "Alright, that's all you need to know for now."
                    + "\n\nTry to fill up the New Playable Song form and click on that \"Create Song\" button to create a song, or you can close the form and select an existing Playable Song to open on the Home modal. The choice is yours.",
                    () => Chartmaker.main.CurrentSong != null,
                    "(open a Playable Song to continue)"
                ),
                new (
                    "You did it! You've opened a Playable Song!"
                    + "\n\nFrom here on, you can click on the button below to switch to the next tutorial."
                ),
            }
        ),
        new Tutorial (
            "Getting Familliar with the Editor",
            onSongCondition,
            new TutorialStep[] {
                new (
                    "Welcome to the Editor! Isn't it cozy?"
                    + "\n\nFor this tutorial, let us have a quick tour of the Editor view, shall we?"
                ),
                new (
                    "This is the Information Bar. Here lies the most basic information about the song in play, such as the current time position of the head.",
                    "Info Bar"
                ),
                new (
                    "Here also lies the button to you to play/pause the current song. You can also do this by pressing Space on your keyboard."
                    + "\n\nTry to learn all the keybindings, they are really useful and can help you chart faster!",
                    "Info Bar->Play&Pause"
                ),
                new (
                    "This is the Visualizer. This defaults to being to a visual metronome but you can click on it to switch between different visualizations!"
                    + "\n\n<i><size=8>(i spent way too much time working on the visualization without thinking that not that many people will care enough to swtich them but well)",
                    "Info Bar->Visualizer"
                ),
                new (
                    "Here is the tab buttons that let you switch between Song view and Chart view."
                    + "\n\nIf there isn't a Chart opened yet, there will be a button here telling you to select a Chart to open."
                    + "\n\nWe'll get to the Chart thing later, but for now we still have a lot of the Editor to explain.",
                    "Info Bar->Song Details/Background"
                ),
                new (
                    "Ooh, what's this dropdown over here?",
                    "Info Bar->Play Options",
                    () => InformationBar.main.PlayOptions.gameObject.activeSelf,
                    "(press it to continue)"
                ),
                new (
                    "This is the Play Options panel, where you can change the playback volume of the song and some other things."
                    + "\n\nCan you guess which slider changes which? (hover over the icons for answer!)",
                    "Canvas/Play Options"
                ),
                new (
                    "This here is the Hierarchy, which lists relevant objects in a hierarical way."
                    + "\n\nItems here can be left-clicked on to be selected, or be right-clicked on to display more action regarding that item. You can also click on the triangle on the left of each object to expand/collapse them."
                    + "\n\nThere's also a search bar to find items by their names, and a filter option to make the Hierarchy view filters out specific types of items.",
                    "Content/View/Hierarchy"
                ),
                new (
                    "The view below here is the Timeline, where items are displayed in a chronological order."
                    + "\n\nYou can click on items here to interact with them similar to Hierarchy items, and you can also drag items to move them along the time axis.",
                    "Content/Timeline/Timeline"
                ),
                new (
                    "The bottom bar here lies some stuff that we should take a look into."
                    + "\n\nThe buttons on the left hand side here does editing actions like undo, cut, copy, and paste; though as you can guess these can also be done with the keybindings."
                    + "\n\nThose fields on the right hand side though are more interesting--they change some of the ways that the Timeline works. Since this is quite important, we should look more deeply into some of them.",
                    "Content/Timeline/Bottom Items"
                ),
                new (
                    "This field here change the beat division factor of the lines on the Timeline. The Timeline's beat division are applied recursively when you zoom in or out, so a beat division of 2 mean the Timeline will place beat lines every half note, fourth note, 8th note, 16th note, and so on.",
                    "Content/Timeline->Options/Separation"
                ),
                new (
                    "This field here change changes the playback speed of the song. One of the uses of this is when you might want to slow the song down so you can tell notes apart more easily.",
                    "Content/Timeline->Options/Speed"
                ),
                new (
                    "Ooh, another dropdown? What does this one do?",
                    "Content/Timeline->More Options",
                    () => TimelinePanel.main.Options.gameObject.activeSelf,
                    "(press it to continue)"
                ),
                new (
                    "This here is the Timeline Options panel, which has even more Timeline options."
                    + "\n\nThe notable option here is the one that let you add visual cues, such as having a waveform or a spectrogram of the song on the background of the Timeline. You might find this a lot helpful on the task of syncing things to the song.",
                    "Canvas/Timeline Options"
                ),
                new (
                    "This is the Picker. You can, well, pick items here."
                    + "\n\nThe Picker is separated into two parts: the top one whose items place objects into the Hierarchy, and the bottom one whose items change the behavior when you interact with the Timeline."
                    + "\n\n<i><size=8>(there's also something hidden inside the Picker...)",
                    "Content/Picker"
                ),
                new (
                    "The Hierarchy items works quite simply. You just click on one of them and the corresponding items will be put into the Hierarchy.",
                    "Content/Picker/Hierarchy Items"
                ),
                new (
                    "The Timeline items are sightly more complicated. They don't change the items on the Timeline directly, instead they change how interacting with the Timeline works."
                    + "\n\nItems here changes based on what Timeline tab is active, but there are 3 common items that are the same for all of the tabs:",
                    "Content/Picker/Timeline Items"
                ),
                new (
                    "... the Cursor item that lets you seek the song with the left mouse button, ...",
                    "Content/Picker/Timeline Items->Cursor"
                ),
                new (
                    "... the Select item which lets you select a range of items by dragging the background, ...",
                    "Content/Picker/Timeline Items->Select"
                ),
                new (
                    "... and the Delete item that let you remove items on the Timeline by clicking on them.",
                    "Content/Picker/Timeline Items->Delete"
                ),
                new (
                    "This is the Inspector view. When you select an item using the Timeline or the Hierarchy, its properties will be shown here.",
                    "Content/View/Inspector"
                ),
                new (
                    "And last and most importantly, this is the Player View. This is what the player would see when they play your chart!",
                    "Content/View/Player View"
                ),
                new (
                    "This pretty much covers the basic interface of the Editor, in the following tutorials we'll see what sorts of things you can do with it!"
                ),
            }
        ),
        new Tutorial (
            "Editing Playable Song Information",
            onSongCondition,
            new TutorialStep[] {
                new (
                    "Welcome back!"
                    + "\n\nIn the last tutorial, we've covered the basic interface of the Editor View. Now lets see what you can do with it!"
                    + "\n\nWe won't be doing any of the charting stuff yet, instead we'll learn editing the Playable Song data first."
                ),
                new (
                    "Since this is a tutorial about editing the Playable Song, you'll have to switch to the Song view in order to continue.",
                    "Info Bar->Song Details/Song",
                    () => HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong,
                    "(press this tab to continue)"
                ),
                new (
                    "When the Editor is in the Song view, the Hierarchy view will show the Playable Song object as the hierarchy root."
                    + "\n\nBy selecting it you can change the metadata of the Playable Song (in case you entered some of the fields incorrectly at the song creation step.)",
                    "Content/View/Hierarchy"
                ),
                new (
                    "It is also here that you can find the Cover object. It basically dictates how the game renders the song's illustrtion cover."
                    + "\n\nAnd for the reason why you don't declare the illustration during the song creation step ...",
                    "Content/View/Hierarchy->Content/Cover"
                ),
                new (
                    "... a Cover can have multiple layers! This will be used to create a parallax scrolling effect, which we'll demonstrate later on."
                    + "\n\nTo create a Cover Layer, you just need to click this button here.",
                    "Content/Picker->Cover Layer",
                    () => NewCoverLayerModal.main,
                    "(press this button to continue)"
                ),
                new (
                    "This is the new Cover Layer modal, which you'll need to go through in order to make a Cover Layer.",
                    "New Cover Layer Modal(Clone)"
                ),
                new (
                    "... Well, in order to do that, you'll need to have a image source, of course."
                    + "\n\nThese buttons here will allow you to load a cover image from two source: ...",
                    "New Cover Layer Modal(Clone)->Load"
                ),
                new (
                    "... you can choose to load the cover image from the song's metadata if there is one, or ...",
                    "New Cover Layer Modal(Clone)->Load/Metadata"
                ),
                new (
                    "... you can choose to do it the traditional way and supply an image file on your computer.",
                    "New Cover Layer Modal(Clone)->Load/Image File"
                ),
                new (
                    "Either way, once you done it, the modal will show the image on the screen so you can know if you choose the correct image. At that point you can just press the \"Create\" button and a Cover Layer using that image will be created!"
                ),
                new (
                    "... well, after you specify where the Chartmaker should save the resulting image into, of course.",
                    "New Cover Layer Modal(Clone)->Content/Top"
                ),
                new (
                    "Now's your turn! Try to create a Cover Layer yourself and select it on the Hierarchy to advance to the next step."
                    + "\n\n(or if your song already have a Cover Layer, you can just select it instead, we just need a Cover Layer selected to have something to demonstrate)",
                    () => InspectorPanel.main.CurrentObject is CoverLayer,
                    "(select a Cover Layer to continue)"
                ),
                new (
                    "This is the Inspector, which since you just selected a Cover Layer, is now displaying its properties."
                    + "\n\nA Cover Layer, besides from the image source, also keeps track of some things: its position, scale, parallax scrolling factor, and whether it's a tiling image.",
                    "Content/View/Inspector"
                ),
                new (
                    "The Position property moves the Cover Layer a specified amount of units in space."
                    + "\n\nThe precise unit depends on where the cover is displayed ...",
                    "Content/View/Inspector->Content/Position"
                ),
                new (
                    "... which you can change using the buttons here to test it.",
                    "Content/View/Player View->Cover Tools/View Mode"
                ),
                new (
                    "The Panorama view ...",
                    () => {
                        PlayerView.main.SetCoverViewMode(CoverViewMode.Panorama);
                        return true;
                    },
                    ""
                ),
                new (
                    "The Panorama view appears when the player selects the song and becomes the background of the screen. This view allows the player to see the most of your cover."
                    + "\n\nThe view during Panorama view will always be 880 units wide and 200 units tall.",
                    "Content/View/Player View->Bounding Box"
                ),
                new (
                    "The Icon view ...",
                    () => {
                         PlayerView.main.SetCoverViewMode(CoverViewMode.Icon);
                        return true;
                    },
                    ""
                ),
                new (
                    "The Icon view appears when your song appears in a list of songs. Your song's cover will be much smaller during this view so you should frame this into the most dormant object in your cover so the player can identify it quickly in a list of songs."
                    + "\n\nThe size of the Icon view depends on what is defined in the main Cover object.",
                    "Content/View/Player View->Bounding Box"
                ),
                new (
                    "The Scale property resizes the Cover Layer."
                    + "\n\nA scale of 1 means the Cover Layer will be as wide as the Panorama view.",
                    "Content/View/Inspector->Content/Scale"
                ),
                new (
                    "The Parallax Z property determines the parallax scrolling factor that will be applied to the Cover Layer when the cover moves around."
                    + "\n\nA value of 0 means the layer moves with the cover.\nA value of 1 means the layer stays stationary when the cover moves, kinda like a windowing effect."
                    + "\n\nYou can enter a number between 0 and 1 which makes the layer follow the cover frame but slower. You can also enter a number greater than 1 (which makes the layer move in the opposite direction) or a negative number (which makes the layer out-paces the cover frame when it moves.)",
                    "Content/View/Inspector->Content/Parallax Z"
                ),
                new (
                    "And last but not least, the Tiling property determines if the image should be repeated in both the X and Y axis.",
                    "Content/View/Inspector->Content/Tiling"
                ),
                new (
                    "Let's go to the Cover object for more...",
                    "Content/View/Hierarchy->Content/Cover",
                    () => InspectorPanel.main.CurrentObject is Cover,
                    "(select this Cover object to continue)"
                ),
                new (
                    "For the parent Cover object, you can change its background color, and the position and size of the Icon view frame.",
                    "Content/View/Inspector"
                ),
                new (
                    "Alright, that it's for now. I hoped that you now learned to edit Playable Songs' Covers and- wait..."
                ),
                new (
                    "I forgot one very important thing! Silly me."
                    + "\n\nWhen the editor is in Song view, you can use the Timeline to edit the song's timing information!"
                    + "\n\nYou should make sure the timing information here is correct before you even do any of the charting, since editing it later on will cause every object that uses beat position information to move accordingly!",
                    "Content/Timeline"
                ),
                new (
                    "See that one square thing in the Timeline? That's the representation of a BPM Stop!"
                    + "\n\nA BPM Stop basically tells the game which portion of the song will it have a specific BPM measurement."
                ),
                new (
                    "Now, let's select it...",
                    () => InspectorPanel.main.CurrentObject is BPMStop,
                    "(select a BPM Stop to continue)"
                ),
                new (
                    "Since you just select a BPM Stop, its properties are now displayed on the Inspector."
                    + "\n\nHere you can change its BPM data, starting position, the time signature, and whether it's \"significant\"",
                    "Content/View/Inspector"
                ),
                new (
                    "This field at the top here is the object's time position. It determines when the BPM Stop will basically \"take effect\"."
                    + "\n\nFor the first BPM Stop on the list chronologically (the earliest one) this is where the 0th beat position is. For the other BPM stops this is where the BPM changes from the previous BPM to this one."
                    + "\n\nNote that this is defined is seconds, not milliseconds!",
                    "Content/View/Inspector->Header/Offset"
                ),
                new (
                    "You might also have noticed that the Signature field only accepts one value instead of two that you'd expect from... well... an actual music time signature."
                    + "\n\nThis is because the second value (the beat length) is irrelevant in this situation. A beat's length in JANOARG is already defined in absolute units (the BPM) so we don't need to defined another one in relative units (a quarter note, e.t.c...)",
                    "Content/View/Inspector->Content->Signature"
                ),
                new (
                    "You might have also wondered what this \"Significant\" toggle does. It basically marks the BPM Stop that it is relevant enough to be considered into the song's BPM range calculation."
                    + "\n\nIf you found yourself, for example, using a BPM Stop to realign the beats of the song because a section of the song has irregular timing, you can unmark the \"Significant\" toggle off so it doesn't actually count as a BPM change noticable enough to be treated as such.",
                    "Content/View/Inspector->Content->Significant"
                ),
                new (
                    "Alright, that it's for now, for real. I hope that you've learned how to edit Playable Songs' Covers and perfect their Timing information."
                    + "\n\nWe are now finally be ready to create a Chart and actually start charting for once!"
                ),
            }
        ),
        new Tutorial (
            "Creating a New Chart File",
            onSongCondition,
            new TutorialStep[] {
                new (
                    "In this tutorial, we will learn how to create a Chart file that will contain a sequence of objects representing a difficulty of a song."
                ),
            }
        ),
    };

    static bool onHomeCondition(Action start) 
    {
        if (Chartmaker.main.CurrentSong != null) 
        {
            DialogModal dm = ModalHolder.main.Spawn<DialogModal>();
            dm.SetDialog(
                "Start this Tutorial?", 
                "This action will close the song.",
                new [] {"Cancel", "Start"},
                (x) => {
                    if (x == 0) return;
                    Chartmaker.main.DirtyModal(() => {
                        Chartmaker.main.CloseSong();
                        start();
                    });
                }
            );
            return false;
        }
        return true;
    }
    static bool onSongCondition(Action start) 
    {
        if (Chartmaker.main.CurrentSong == null) 
        {
            DialogModal dm = ModalHolder.main.Spawn<DialogModal>();
            dm.SetDialog(
                "Missing Requirement", 
                "This tutorial requires an active Playable Song.",
                new [] {"Ok"},
                _ => {}
            );
            return false;
        }
        return true;
    }
}