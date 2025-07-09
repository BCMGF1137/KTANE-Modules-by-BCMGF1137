using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class SimonsSumsScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    [SerializeField]
    public KMSelectable[] Buttons;
    [SerializeField]
    private TextMesh newText;
    public Material[] materials;
    public Renderer[] ButtonMaterials;
    public KMColorblindMode CBM;

    private Color32 colorStored;

    private IEnumerator[] sequences = new IEnumerator[5];

    private KMAudio.KMAudioRef _mySound;

    private const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private string[] colorsA = { "R", "G", "B", "C", "M", "Y" };
    private string[] colorsB = { "-", "-", "-", "-", "-", "-" };
    private int pos = 0;
    private int stage = 1;

    private int tempPress;
    private int tempHL;

    private int x = 0;
    private int y = 0;
    private int[,] yValues = { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } };

    private int z1 = 0;

    private int[] initialValues = { 0, 0, 0, 0, 0, 0 }; // Initial color or button values
    private int[,] flashValues = { { 0, 0, 0, 0}, // C# won't let me use this length 2 so [2] and [3] will be zeros
                                   { 0, 0, 0, 0}, // Same case
                                   { 0, 0, 0, 0 } }; // Oh yeah, these are the flash values
    private int uselessInt = 0; // I count the amount of ticks it takes for the colors to be generated [hopefully it doesn't crash the module]

    private string[] presses = { "", "", "" }; // What is to be pressed in each stage

    private int[,] increment = { // Mandatory for initial button values
                                {00,06,20,24,32,31 },
                                {03,29,35,26,12,27 },
                                {18,17,23,09,25,19 },
                                {07,02,14,11,10,28 },
                                {01,05,21,16,04,13 },
                                {30,34,15,08,33,22 }
                                };
    private double[,] divisors = { // Mandatory for X-values
                                {1.396, 1.990, 1.900, 1.201, 1.039, 1.688},
                                {1.714, 1.101, 1.180, 1.854, 1.367, 1.675},
                                {1.530, 1.562, 1.969, 1.497, 1.159, 1.672},
                                {1.488, 1.048, 1.759, 1.794, 1.356, 1.555},
                                {1.883, 1.887, 1.961, 1.342, 1.409, 1.138},
                                {1.492, 1.099, 1.162, 1.698, 1.860, 1.482}
                                };

    private string[] stages = { "", "", "", "" }; // Colors that flash in each stage
    private string order = ""; // The button order, north clockwise

    private char initialFinal = ' '; // Required for fixing a bug where the button that needs to be pressed in the final stage changes upon a strike

    private int BRUG = 0; // Detects the GGGY case. It will be 4 if GGGY (or similar) happens

    // Placeholders

    private string temp = ""; 
    private int[] tempList = { 0, 0, 0 };
    private int[] tempList2 = { 0, 0, 0, 0 };
    private int[] flashValues2 = {0, 0, 0, 0 }; // C# won't let me use 2D arrays in list lookups
    private bool found = false;
    private int IDK = 0;

    // What *YOU PRESSED* in each stage

    private string submissionColors = "";
    private int[] submissionTimes = {0, 0, 0, 0};

    // Used for determining if your answer is correct

    private string colorResult = "";
    private int correctColors = 0;

    // Used for module functions

    private bool _moduleSolved = false; // If the module is solved.
    private bool _moduleActivated = false; // For when the module makes sounds.
    private bool _moduleDeactivated = false; // Used for activating boredom repellent
    private bool _boredomRepellent = false; // Used for toggling boredom repellent

    private string solveText = "";

    private string displayText = ""; // Colorblind mode usage
    private string colorText = ""; // Colorblind mode usage

    private bool colorblindMode; // Colorblind mode activated?
    private bool autosolving = false; // For TP autosolver

    private float elapsed = 0f; // For solve animation

    void Awake() {
    
        _moduleID = _moduleIdCounter++;

        foreach(KMSelectable Button in Buttons)
        {
            Button.OnInteract += delegate ()
            {
                ButtonPress(Button);
                return false;
            };
            Button.OnHighlight += delegate ()
            {
                HLButton(Button);
            };
            Button.OnHighlightEnded += delegate ()
            {
                HLEndButton(Button);
            };
        }
        
    }

    // Use this for initialization
    void Start()
    {

        Debug.LogFormat("[Simon's Sums #{0}] This module has been generated at Update 1.", _moduleID);

        sequences[0] = Sequence();
        sequences[1] = StrikeSequence();
        sequences[2] = SolveAnimation();
        sequences[3] = TextAnimation();
        sequences[4] = TimeFix();

        colorblindMode = CBM.ColorblindModeActive;

        // Generating button colors
        uselessInt = 0;

        for (int i = 0; i < 6; i++)
        {
            pos = UnityEngine.Random.Range(0, 6);
            while (colorsA[pos] == "-")
            {
                pos = UnityEngine.Random.Range(0, 6);

                uselessInt++;
            }
            colorsB[i] = colorsA[pos];
            colorsA[pos] = "-";
            uselessInt++;
        }

        Debug.LogFormat("[Simon's Sums #{0}] Initializing button positions took {1} ticks.", _moduleID, uselessInt);

        displayText = "1";

        order = "" + colorsB[0] + colorsB[1] + colorsB[2]
            + colorsB[3] + colorsB[4] + colorsB[5];
        Debug.LogFormat("[Simon's Sums #{0}] The color order (north clockwise) is {1}.", _moduleID, order);

        for (int i = 0; i < 6; i++)
        {
            switch (colorsB[i])
            {
                case "R":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(255, 000, 000, 100);
                    break;
                case "G":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(000, 255, 000, 100);
                    break;
                case "B":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(000, 000, 255, 100);
                    break;
                case "C":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(000, 255, 255, 100);
                    break;
                case "M":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(255, 000, 255, 100);
                    break;
                case "Y":
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color32(255, 255, 000, 100);
                    break;
            }
        }

        // Initial button values

        pos = base36.IndexOf(Bomb.GetSerialNumber()[0]);

        Debug.LogFormat("[Simon's Sums #{0}] The first character of the serial number is {1}. When we convert it to base-36, we get {2}.", _moduleID, Bomb.GetSerialNumber()[0], pos);

        for (int i = 0; i < 6; i++)
        {
            switch (colorsB[i])
            {
                case "R":
                    initialValues[i] = (pos + increment[0, i]) % 36;
                    break;
                case "Y":
                    initialValues[i] = (pos + increment[1, i]) % 36;
                    break;
                case "G":
                    initialValues[i] = (pos + increment[2, i]) % 36;
                    break;
                case "C":
                    initialValues[i] = (pos + increment[3, i]) % 36;
                    break;
                case "B":
                    initialValues[i] = (pos + increment[4, i]) % 36;
                    break;
                case "M":
                    initialValues[i] = (pos + increment[5, i]) % 36;
                    break;
            }
        }

        Debug.LogFormat("[Simon's Sums #{0}] The initial values of the buttons (north clockwise) are {1}, {2}, {3}, {4}, {5}, {6}.", _moduleID,
            initialValues[0], initialValues[1], initialValues[2],
            initialValues[3], initialValues[4], initialValues[5]);

        // All stages are predetermined.

        for (int i = 0; i < 2; i++)
        {
            stages[0] = stages[0] + "RGBCMY"[UnityEngine.Random.Range(0, 6)];
        }

        Debug.LogFormat("[Simon's Sums #{0}] The first stage's flashes will be {1}.", _moduleID, stages[0]);

        for (int i = 0; i < 3; i++)
        {
            stages[1] = stages[1] + "RGBCMY"[UnityEngine.Random.Range(0, 6)];
        }

        Debug.LogFormat("[Simon's Sums #{0}] The second stage's flashes will be {1}.", _moduleID, stages[1]);

        for (int i = 0; i < 4; i++)
        {
            stages[2] = stages[2] + "RGBCMY"[UnityEngine.Random.Range(0, 6)];
        }

        Debug.LogFormat("[Simon's Sums #{0}] The third stage's flashes will be {1}.", _moduleID, stages[2]);



        // Calculating flash values
        for (int ii = 0; ii < 3; ii++)
        {
            for (int i = 0; i < (ii + 2); i++)
            {
                // 1. Start with the initial value of the color that flashed.
                flashValues[ii, i] = initialValues[order.IndexOf(stages[ii][i])];

                Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has the initial value of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);

                // 2. Add the number of times this color flashed in the sequence, minus one.
                temp = Convert.ToString(stages[ii][i]);

                for (int j = 0; j < stages[ii].Length; j++)
                {
                    if (Convert.ToString(stages[ii][j]) == temp)
                    {
                        flashValues[ii, i]++;
                    }
                }

                flashValues[ii, i]--;

                Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has a flash modification of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);

                // 3. If an adjacent button flashed, add eight to F.

                pos = order.IndexOf(stages[ii][i]) + 6;

                if (stages[ii].Contains(order[((pos - 1) % 6)]) || stages[ii].Contains(order[((pos + 1) % 6)]))
                {

                    flashValues[ii, i] += 8;
                    Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has an adjacent flash modification of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);
                }

                // 4. If this the second or third stage, and this color didn’t flash in the previous stage, subtract fifteen.

                if ((ii > 0) && !(stages[ii - 1].Contains(stages[ii][i])))
                {
                    flashValues[ii, i] -= 15;
                    Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has an absence flash modification of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);
                }

                // 5. If this is NOT the first flash of the stage, subtract one from F for the number of buttons you need to go counterclockwise to get to the button that previously flashed. If the previous was the same as this one, add thirty instead.
                if (i != 0)
                {
                    pos = order.IndexOf(stages[ii][i - 1]) - order.IndexOf(stages[ii][i]);
                    
                    // There may have been a better way for me to do it

                    if (pos < 0)
                    {
                        pos *= -1;
                    } else if (pos > 0)
                    {
                        pos = 6 - pos;
                    }

                    if (pos == 0)
                    {
                        flashValues[ii, i] += 30;
                    }
                    else
                    {
                        flashValues[ii, i] -= pos;
                    }
                    Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has a counterclockwise flash modification of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);
                }

                // 6. If F is negative, take the absolute value (make it positive).
                if (flashValues[ii, i] < 0)
                {
                    flashValues[ii, i] = -1 * (flashValues[ii, i]);
                }
                Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has a final flash value calculation of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);

                // 7. If this F-value matches a previous F-value earlier in this stage, add one until this rule doesn't apply.

            for (int j = 0; j < i; j++)
                    {
                        if (flashValues[ii, j] == flashValues[ii, i])
                        {
                            Debug.LogFormat("[Simon's Sums #{0}] Flash {2} of stage {3} has a duplicate flash value, so adding 1 leads to a final flash value calculation of {1}.", _moduleID, flashValues[ii, i], i + 1, ii + 1);
                            j = 0;
                            flashValues[ii, i]++;
                        }
                    
                }

            }
        }

        Debug.LogFormat("[Simon's Sums #{0}] The final flash values for stage 1 are {1}, {2}.", _moduleID, flashValues[0, 0], flashValues[0, 1]);
        Debug.LogFormat("[Simon's Sums #{0}] The final flash values for stage 2 are {1}, {2}, {3}.", _moduleID, flashValues[1, 0], flashValues[1, 1], flashValues[1, 2]);
        Debug.LogFormat("[Simon's Sums #{0}] The final flash values for stage 3 are {1}, {2}, {3}, {4}.", _moduleID, flashValues[2, 0], flashValues[2, 1], flashValues[2, 2], flashValues[2, 3]);


        // First stage: Sorting the flash values
        
            if (flashValues[0, 0] > flashValues[0, 1])
            {
                presses[0] = ("" + stages[0][1] + stages[0][0]);
            }
            else
            {
                presses[0] = ("" + stages[0][0] + stages[0][1]);
            }
        

        Debug.LogFormat("[Simon's Sums #{0}] The first stage has presses of {1} and {2}.", _moduleID, presses[0][0], presses[0][1]);

        // Calculating first stage

        for (int j = 0; j < 2; j++) {

            x = 0;
            for (int i = 0; i < 2; i++)
            {
                if (stages[0][i] == presses[0][j])
                {
                    x += flashValues[0, i];
                }
            }
            Debug.LogFormat("[Simon's Sums #{0}] The sum of the flash values for {2} is {1}.", _moduleID, x, presses[0][j]);

            x = (int)((double)x / divisors["RYGCBM".IndexOf(presses[0][j]), order.IndexOf(presses[0][j])]);

            Debug.LogFormat("[Simon's Sums #{0}] Dividing the sum by {1} gives us ~{2}.", _moduleID, divisors["RYGCBM".IndexOf(presses[0][j]), order.IndexOf(presses[0][j])], x);

            y = x % 5;

            Debug.LogFormat("[Simon's Sums #{0}] The raw Y-value for {2} is {1}.", _moduleID, y, presses[0][j]);

            if (j > 0)
            {
                y = (y + yValues[0, j - 1])%5;
            }

            yValues[0, j] = y;

            Debug.LogFormat("[Simon's Sums #{0}] The final Y-value for {2} is {1}.", _moduleID, y, presses[0][j]);
        }
        Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[0][0], yValues[0,0], yValues[0,0] + 5, yValues[0,0] + 10);
        Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[0][1], yValues[0,1], yValues[0,1] + 5, yValues[0,1] + 10);
        StartCoroutine(sequences[0]);
        // StartCoroutine(sequences[2]);
    }

    // Pressing buttons!
    void ButtonPress(KMSelectable Button)
    {
        if (!_moduleSolved){
            for (int i = 0; i < 6; i++)
            {
                if (Button == Buttons[i])
                {
                    if (Buttons[i] == Button)
                    {
                        tempPress = i;
                        break;
                    }
                }
            }

            StopCoroutine(sequences[1]);
            StopCoroutine(sequences[0]);

            z1 = ((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6);

            submissionColors = submissionColors + order[tempPress];
            submissionTimes[submissionColors.Length - 1] = z1;

            Debug.LogFormat("[Simon's Sums #{0}] You pressed {1} at time XX:{3}{2}. The sum of the digits is {4}.", _moduleID, order[tempPress], ((int)Bomb.GetTime() % 10), (((int)Bomb.GetTime() / 10) % 6), z1);

            if (stage == 3 && submissionColors.Length == 3)
            {
                // The final button

                Debug.LogFormat("[Simon's Sums #{0}] Now that you have pressed three colors, it is time to calculate the final press.", _moduleID);

                // I forgot to change it back upon a strike, thanks Quinn Wuest

                presses[2] = "" + presses[2][0] + presses[2][1] + presses[2][2] + initialFinal;

                // 1. Take the sum of the F-values and the I-value of the button with the “target button”, which right now, is the button with the largest F-value, and make it your X-value.

                x = initialValues[order.IndexOf(presses[2][3])];
                for (int i = 0; i < 4; i++)
                {
                    if (stages[2][i] == presses[2][3])
                    {
                        x += flashValues[2, i];
                    }
                }

                Debug.LogFormat("[Simon's Sums #{0}] The initial value of {1} is {2}, and when we add the flash values, we get {3}.", _moduleID, presses[2][3], initialValues[order.IndexOf(presses[2][3])], x);

                // 2. Add two times the sum of the previous Y-values from all of the previous stages to X. 

                x += 2 * (yValues[0, 0] + yValues[0, 1]);
                x += 2 * (yValues[1, 0] + yValues[1, 1] + yValues[1, 2]);
                x += 2 * (yValues[2, 0] + yValues[2, 1] + yValues[2, 2]);

                Debug.LogFormat("[Simon's Sums #{0}] Adding the sum of the previous Y-values gives {1}.", _moduleID, x);

                // 3. Multiply your X-value by two plus a fifth of the number of strikes when you pressed the previous button, and round down. Then, add one.

                x = (int)((double)x * (2.0 + (Bomb.GetStrikes() / 5.0)));

                Debug.LogFormat("[Simon's Sums #{0}] Since we have {2} strikes, we multiply X by " + (2.0 + (Bomb.GetStrikes() / 5.0)) + ", giving {1}.", _moduleID, x, Bomb.GetStrikes());

                // 4. Use the table under the “Obtaining Press Values” section to exponentiate X, and round down. This is your final X-value.

                x = (int)Math.Pow(x, divisors["RYGCBM".IndexOf(presses[2][3]), order.IndexOf(presses[2][3])]);

                Debug.LogFormat("[Simon's Sums #{0}] Exponentiating X by {2} gives ~{1}. This is our final X value.", _moduleID, x, divisors["RYGCBM".IndexOf(presses[2][3]), order.IndexOf(presses[2][3])]);
                Debug.LogFormat("[Simon's Sums #{0}] We are now going to calculate what to press.", _moduleID);

                // 5. Follow the table below to modify the target button:
                // The buttons (north clockwise) are Red, Green, Blue, Cyan, Magenta, Yellow - Skip this table and use the button that flashed for your target button.

                if (order == "RGBCMY")
                {
                    Debug.LogFormat("[Simon's Sums #{0}] Interestingly enough, our button order (north clockwise) is RGBCMY, so we are going to press {1} as our final button.", _moduleID, presses[2][3]);
                }
                else
                {
                    temp = "" + presses[2][3];

                    // The northwest button is Yellow - Go to the button diametrically opposite your target button.
                    if (order[5] == 'Y')
                    {
                        temp = "" + order[(order.IndexOf(temp) + 3) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] The northwest button is Yellow, so our new button is {1}.", _moduleID, temp);
                    }

                    // Red is diametrically opposite Cyan - Go to the button with the complementary color of your target button.
                    if ((order.IndexOf("R") - order.IndexOf("C") + 60) % 6 == 3)
                    {
                        temp = "" + "RYGCBM"[("RYGCBM".IndexOf(temp) + 3) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] Red is diametrically opposite Cyan, so our new button is {1}.", _moduleID, temp);
                    }

                    // Yellow and Green are separated by one button - Go counterclockwise until you reach a Red, Green, or Blue button.
                    if (Math.Abs(order.IndexOf("Y") - order.IndexOf("G")) == 2 || Math.Abs(order.IndexOf("Y") - order.IndexOf("G")) == 4)
                    {
                        temp = "" + order[(order.IndexOf(temp) + 5) % 6];
                        while (!"RGB".Contains(temp))
                        {
                            temp = "" + order[(order.IndexOf(temp) + 5) % 6];
                        }
                        Debug.LogFormat("[Simon's Sums #{0}] Yellow and Green are separated by one button, so the previous primary is {1}.", _moduleID, temp);
                    }

                    // Blue and Magenta are separated by one button - Go clockwise until you reach a Cyan, Green, or Blue button.
                    if (Math.Abs(order.IndexOf("B") - order.IndexOf("M")) == 2 || Math.Abs(order.IndexOf("B") - order.IndexOf("M")) == 4)
                    {
                        temp = "" + order[(order.IndexOf(temp) + 1) % 6];
                        while (!"CMY".Contains(temp))
                        {
                            temp = "" + order[(order.IndexOf(temp) + 1) % 6];
                        }
                        Debug.LogFormat("[Simon's Sums #{0}] Blue and Magenta  are separated by one button, so the next secondary is {1}.", _moduleID, temp);
                    }

                    // Red is in the top half of the module - Go to the next color in the sequence RYGCBM.
                    if (order[5] == 'R' || order[0] == 'R' || order[1] == 'R')
                    {
                        temp = "" + "RYGCBM"[("RYGCBM".IndexOf(temp) + 1) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] Red is in the top half of the module, so the next color in the sequence RYGCBM is {1}.", _moduleID, temp);
                    }

                    // Blue is in the bottom half of the module - Go to the next color in the sequence RGBCMY.
                    if (order[2] == 'B' || order[3] == 'B' || order[4] == 'B')
                    {
                        temp = "" + "RGBCMY"[("RGBCMY".IndexOf(temp) + 1) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] Blue is in the bottom half of the module, so the next color in the sequence RGBCMY is {1}.", _moduleID, temp);
                    }

                    // Starting from Red, the Blue button is further clockwise than the Green button - Go to the button two clockwise from your target button.
                    IDK = order.IndexOf("R");

                    while ((order[IDK] != 'G') && (order[IDK] != 'B'))
                    {
                        IDK++;
                        IDK %= 6;
                    }
                    if (order[IDK] == 'G')
                    {
                        temp = "" + order[(order.IndexOf(temp) + 2) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] Blue is further than Green, so the color two clockwise from the previous button is {1}.", _moduleID, temp);
                    }

                    // Starting from Cyan, the Yellow button is further clockwise than the Magenta button - Go to the button one counterclockwise from your target button.
                    IDK = order.IndexOf("C");

                    while ((order[IDK] != 'M') && (order[IDK] != 'Y'))
                    {
                        IDK++;
                        IDK %= 6;
                    }
                    if (order[IDK] == 'M')
                    {
                        temp = "" + order[(order.IndexOf(temp) + 5) % 6];
                        Debug.LogFormat("[Simon's Sums #{0}] Yellow is further than Magenta, so the color one counterclockwise from the previous button is {1}.", _moduleID, temp);
                    }

                    temp = "" + order[(order.IndexOf(temp) + x) % 6];

                    // 6. Take the X-value. Go clockwise that many buttons to get your final button.
                    Debug.LogFormat("[Simon's Sums #{0}] X is equal to {1}, and X modulo 6 equals {2}. When we go clockwise {2} buttons, we land on {3}, which is going to be our final button.", _moduleID, x, x % 6, temp);
                }
                presses[2] = "" + presses[2][0] + presses[2][1] + presses[2][2] + temp;

                // 7. Take the digital root of X and make it your Y-value.
                y = (x - 1) % 9 + 1;
                Debug.LogFormat("[Simon's Sums #{0}] X is currently {1}. The digital root of {1} is {2}.", _moduleID, x, y);

                // 8. Add the Y-value obtained in the previous press.
                y += yValues[2, 2];
                Debug.LogFormat("[Simon's Sums #{0}] Adding the previous Y-value gives us {1}.", _moduleID, y);

                // 9. If the target button is north, southeast, or southwest, add one to Y. If it’s northeast, south, or northwest, subtract one.
                if (order.IndexOf(temp) % 2 == 0)
                {
                    y++;
                    Debug.LogFormat("[Simon's Sums #{0}] The target button is north, southeast, or southwest. We add one to Y to get {1}.", _moduleID, y);
                }
                else
                {
                    y--;
                    Debug.LogFormat("[Simon's Sums #{0}] The target button is northeast, south, or northwest. We subtract one from Y to get {1}.", _moduleID, y);
                }

                Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is exactly {2}.", _moduleID, presses[2][3], y);
                yValues[2, 3] = y;
            }

            StartCoroutine(PressedButton());
        }
        if (_moduleDeactivated) // Boredom repellent
        {
            if (_boredomRepellent)
            {
                _mySound.StopSound();
            } else
            {
                _mySound = Audio.PlaySoundAtTransformWithRef("Ryzmik - Minuit", transform);
            }
            _boredomRepellent = !_boredomRepellent;
        }
    }

    // Flashing colors!
    private IEnumerator Sequence()
    {
            stages[3] = stages[stage - 1];

        for (int i = 0; i <= (6 + 2 * stage); i++)
        {
            if (i >= 3 && i%2==1)
            {
                colorStored = Buttons[order.IndexOf(stages[stage-1][(i-3)/2])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf(stages[stage-1][(i - 3) / 2])].material = materials[1];
                Buttons[order.IndexOf(stages[stage-1][(i - 3) / 2])].GetComponent<MeshRenderer>().material.color = colorStored;

               if (_moduleActivated) {
                    Audio.PlaySoundAtTransform(stages[stage - 1][(i - 3) / 2].ToString(), transform);
                }

                yield return new WaitForSeconds(0.2f);
            } else
            {
                for (int j = 0; j < 6; j++)
                {
                    colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[j].material = materials[0];
                    Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
                }

                yield return new WaitForSeconds(0.2f);
            }

            i = i % (4 + 2 * stage);
            
        }
    }

    private IEnumerator StrikeSequence()
    {
        for (int i=0; i<500; i++)
        {

            i = i % (4 + 2 * stage);
            if (i == 0)
            {
                displayText = stage.ToString();
            }
            else if (i % 2 == 0)
            {
                displayText = colorResult[(i-2)/2].ToString();
            }
            else
            {
                displayText = "";
            }

            yield return new WaitForSeconds(0.1414f);
        }
    }

    private IEnumerator PressedButton()
    {
        _moduleActivated = true;
        colorResult = "";

        for (int j = 0; j < 6; j++)
        {
            colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
            ButtonMaterials[j].material = materials[0];
            Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
        }

        colorStored = Buttons[order.IndexOf(submissionColors[submissionColors.Length - 1])].GetComponent<MeshRenderer>().material.color;
        ButtonMaterials[order.IndexOf(submissionColors[submissionColors.Length - 1])].material = materials[1];
        Buttons[order.IndexOf(submissionColors[submissionColors.Length - 1])].GetComponent<MeshRenderer>().material.color = colorStored;

        Audio.PlaySoundAtTransform(submissionColors[submissionColors.Length - 1].ToString(), transform);

        yield return new WaitForSeconds(0.2f);

        for (int j = 0; j < 6; j++)
        {
            colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
            ButtonMaterials[j].material = materials[0];
            Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
        }

        if (submissionColors.Length >= stage + 1)
        {
            colorResult = "";
            correctColors = 0;

            for (int i = 0; i < stage + 1; i++)
            {
                if (i < 3)
                {
                    if (submissionColors[i] == presses[stage - 1][i] && (submissionTimes[i] % 5) == yValues[stage - 1, i]) // Correct color, correct time
                    {

                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the correct button at the correct time.", _moduleID, submissionColors[i], submissionTimes[i], i + 1);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "G";
                                break;
                            case 1:
                                colorResult = colorResult + "M";
                                break;
                        }
                        correctColors++;

                        BRUG++;

                    }
                    else if (submissionColors[i] == presses[stage - 1][i] && ((submissionTimes[i] % 5) != yValues[stage - 1, i])) // Correct color, wrong time
                    {

                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the correct button at the wrong time.", _moduleID, submissionColors[i], submissionTimes[i], i + 1);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "Y";
                                break;
                            case 1:
                                colorResult = colorResult + "B";
                                break;
                        }

                    }
                    else // Wrong color
                    {

                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the wrong button.", _moduleID, submissionColors[i], submissionTimes[i], i + 1, presses[stage - 1][i]);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "R";
                                break;
                            case 1:
                                colorResult = colorResult + "C";
                                break;
                        }

                    }
                } else
                { // The final press
                    if (submissionColors[3] == presses[2][3] && (submissionTimes[i]) == yValues[2, 3])
                    {
                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the correct button at the correct time.", _moduleID, submissionColors[i], submissionTimes[i], i + 1);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "G";
                                break;
                            case 1:
                                colorResult = colorResult + "M";
                                break;
                        }
                        correctColors++;
                    }
                    else if (submissionColors[3] == presses[2][3] && (submissionTimes[i]) != yValues[2, 3])
                    {
                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the correct button at the wrong time.", _moduleID, submissionColors[i], submissionTimes[i], i + 1);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "Y";
                                break;
                            case 1:
                                colorResult = colorResult + "B";
                                break;
                        }

                        BRUG++;
                    }
                    else
                    {
                        Debug.LogFormat("[Simon's Sums #{0}] Press #{3} ({1} at sum {2}) was the wrong button.", _moduleID, submissionColors[i], submissionTimes[i], i + 1);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                colorResult = colorResult + "R";
                                break;
                            case 1:
                                colorResult = colorResult + "C";
                                break;
                        }
                        BRUG++;
                    }
                }
            }

            submissionColors = "";
            submissionTimes[0] = 0;
            submissionTimes[1] = 0;
            submissionTimes[2] = 0;
            submissionTimes[3] = 0;

            if (BRUG == 4) // Sound of anguish plays when you get GGGY/GGGR in this module (or something equivalent)
            {
                Buttons[0].AddInteractionPunch(50f);
                Audio.PlaySoundAtTransform("GGGY", transform);
            }

            BRUG = 0;

            if (correctColors != stage + 1)
            {
                Debug.LogFormat("[Simon's Sums #{0}] One or more inputs were incorrect. The display colors are {1}.", _moduleID, colorResult);

                if (!autosolving) Module.HandleStrike();
                
                StartCoroutine(sequences[0]);
                StartCoroutine(sequences[1]);
            } else
            {
                if (stage == 3) {
                    Debug.LogFormat("[Simon's Sums #{0}] Module solved!", _moduleID);
                    _moduleSolved = true;
                    // Module solved.
                    StopCoroutine(sequences[0]);
                    StopCoroutine(sequences[1]);
                    displayText = "";
                    StartCoroutine(sequences[2]);
                }
                else
                {
                    StopCoroutine(sequences[1]);
                    StartCoroutine(sequences[0]);

                    Debug.LogFormat("[Simon's Sums #{0}] That is correct. You are now advancing to stage {1}.", _moduleID, stage +1);

                    switch (stage + 1)
                    {
                        case 2:

                            // Second stage
                            // Sorting flash values

                            tempList[0] = flashValues[1, 0];
                            tempList[1] = flashValues[1, 1];
                            tempList[2] = flashValues[1, 2];

                            tempList = tempList.OrderBy(x => x).ToArray();

                            flashValues2[0] = flashValues[1, 0];
                            flashValues2[1] = flashValues[1, 1];
                            flashValues2[2] = flashValues[1, 2];
                            
                            tempList[0] = flashValues[1, 0];
                                tempList[1] = flashValues[1, 1];
                                tempList[2] = flashValues[1, 2];

                                tempList = tempList.OrderBy(x => x).ToArray();

                                presses[1] = stages[1][Array.IndexOf(flashValues2, tempList[0])].ToString();
                                presses[1] += stages[1][Array.IndexOf(flashValues2, tempList[1])].ToString();
                                presses[1] += stages[1][Array.IndexOf(flashValues2, tempList[2])].ToString();

                            Debug.LogFormat("[Simon's Sums #{0}] The second stage's initial presses (before modification) are {1}, {2}, and {3}.", _moduleID, presses[1][0], presses[1][1], presses[1][2]);

                            // Two clockwise from lowest

                            temp = "" + presses[1][0];
                            temp = "" + order[(2 + order.IndexOf(temp))%6];
                            presses[1] = temp + presses[1][1] + presses[1][2];

                            // Diametrically opposite second-highest

                            temp = "" + presses[1][1];
                            temp = "" + order[(3 + order.IndexOf(temp)) % 6];
                            presses[1] = "" + presses[1][0] + temp + presses[1][2];

                            // Complementary of highest

                            temp = "" + "RYGCBM"[(3 + "RYGCBM".IndexOf(presses[1][2])) % 6];
                            presses[1] = "" + presses[1][0] + presses[1][1] + temp;

                            Debug.LogFormat("[Simon's Sums #{0}] The second stage has presses of {1}, {2}, and {3}.", _moduleID, presses[1][0], presses[1][1], presses[1][2]);

                            // When to press
                            
                            for (int j = 0; j < 3; j++)
                            {
                                x = 0;
                                found = false;
                                for (int i = 0; i < 3; i++)
                                {
                                    if (stages[1][i] == presses[1][j])
                                    {
                                        x += flashValues[1, i];
                                        found = true;
                                    }
                                }
                                if (found)
                                {
                                    Debug.LogFormat("[Simon's Sums #{0}] The sum of the flash values for {2} is {1}.", _moduleID, x, presses[1][j]);
                                }
                                else
                                {
                                    x = initialValues[order.IndexOf(presses[1][j])];
                                    Debug.LogFormat("[Simon's Sums #{0}] Since {2} never flashed, we are using the initial value of {2}, which is {1}.", _moduleID, x, presses[1][j]);
                                }

                                x = (int)((double)x / divisors["RYGCBM".IndexOf(presses[1][j]), order.IndexOf(presses[1][j])]);

                                Debug.LogFormat("[Simon's Sums #{0}] Dividing the sum by {1} gives us ~{2}.", _moduleID, divisors["RYGCBM".IndexOf(presses[1][j]), order.IndexOf(presses[1][j])], x);

                                y = x % 5;

                                Debug.LogFormat("[Simon's Sums #{0}] The raw Y-value for {2} is {1}.", _moduleID, y, presses[1][j]);

                                if (j > 0)
                                {
                                    y = (y + yValues[1, j - 1]) % 5;
                                } else
                                {
                                    y = (y + yValues[0, 1]) % 5;
                                }

                                yValues[1, j] = y;

                                Debug.LogFormat("[Simon's Sums #{0}] The final Y-value for {2} is {1}.", _moduleID, y, presses[1][j]);
                            }
                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[1][0], yValues[1, 0], yValues[1, 0] + 5, yValues[1, 0] + 10);
                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[1][1], yValues[1, 1], yValues[1, 1] + 5, yValues[1, 1] + 10);
                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[1][2], yValues[1, 2], yValues[1, 2] + 5, yValues[1, 2] + 10);

                            break;

                        case 3:

                            // Third and final stage

                            tempList2[0] = flashValues[2, 0];
                            tempList2[1] = flashValues[2, 1];
                            tempList2[2] = flashValues[2, 2];
                            tempList2[3] = flashValues[2, 3];

                            tempList2 = tempList2.OrderBy(x => x).ToArray();

                            flashValues2[0] = flashValues[2, 0];
                            flashValues2[1] = flashValues[2, 1];
                            flashValues2[2] = flashValues[2, 2];
                            flashValues2[3] = flashValues[2, 3];

                                presses[2] = stages[2][Array.IndexOf(flashValues2, tempList2[0])].ToString();
                                presses[2] = presses[2] + stages[2][Array.IndexOf(flashValues2, tempList2[1])].ToString();
                                presses[2] = presses[2] + stages[2][Array.IndexOf(flashValues2, tempList2[2])].ToString();
                                presses[2] = presses[2] + stages[2][Array.IndexOf(flashValues2, tempList2[3])].ToString();

                            Debug.LogFormat("[Simon's Sums #{0}] The third stage's initial presses (before modification) are {1}, {2}, {3}, and {4}.", _moduleID, presses[2][0], presses[2][1], presses[2][2], presses[2][3]);

                            // First press: Next primary

                            temp = "" + presses[2][0];

                            temp = "" + order[(order.IndexOf(temp) + 1) % 6];

                            while (! "RGB".Contains(temp))
                            {
                                temp = "" + order[(order.IndexOf(temp) + 1) % 6];
                            }

                            presses[2] = temp + presses[2][1]+ presses[2][2]+ presses[2][3];

                            // Second press: Previous secondary

                            temp = "" + presses[2][1];

                            temp = "" + order[(order.IndexOf(temp) + 5) % 6];

                            while (!"CMY".Contains(temp))
                            {
                                temp = "" + order[(order.IndexOf(temp) + 5) % 6];
                            }

                            presses[2] = presses[2][0] + temp + presses[2][2] + presses[2][3];

                            // Third press: Complementary of diametrically opposite button

                            temp = "" + presses[2][2];

                            temp = "" + order[(order.IndexOf(temp) + 3) % 6];

                            temp = "" + "RYGCBM"[("RYGCBM".IndexOf(temp) + 3) % 6];

                            presses[2] = "" + presses[2][0] + presses[2][1] + temp + presses[2][3];

                            Debug.LogFormat("[Simon's Sums #{0}] The third stage's first 3 presses are {1}, {2}, and {3}. The final press will be determined once you press your third button.", _moduleID, presses[2][0], presses[2][1], presses[2][2]);

                            initialFinal = presses[2][3];

                            for (int j = 0; j < 3; j++)
                            {
                                x = 0;
                                found = false;
                                for (int i = 0; i < 4; i++)
                                {
                                    if (stages[2][i] == presses[2][j])
                                    {
                                        x += flashValues[2, i];
                                        found = true;
                                    }
                                }
                                if (found)
                                {
                                    Debug.LogFormat("[Simon's Sums #{0}] The sum of the flash values for {2} is {1}.", _moduleID, x, presses[2][j]);
                                }
                                else
                                {
                                    x = initialValues[order.IndexOf(presses[2][j])];
                                    Debug.LogFormat("[Simon's Sums #{0}] Since {2} never flashed, we are using the initial value of {2}, which is {1}.", _moduleID, x, presses[2][j]);
                                }

                                x = (int)((double)x / divisors["RYGCBM".IndexOf(presses[2][j]), order.IndexOf(presses[2][j])]);

                                Debug.LogFormat("[Simon's Sums #{0}] Dividing the sum by {1} gives us ~{2}.", _moduleID, divisors["RYGCBM".IndexOf(presses[2][j]), order.IndexOf(presses[2][j])], x);

                                y = x % 5;

                                Debug.LogFormat("[Simon's Sums #{0}] The raw Y-value for {2} is {1}.", _moduleID, y, presses[2][j]);

                                if (j > 0)
                                {
                                    y = (y + yValues[2, j - 1]) % 5;
                                }
                                else
                                {
                                    y = (y + yValues[1, 2]) % 5;
                                }

                                yValues[2, j] = y;

                                Debug.LogFormat("[Simon's Sums #{0}] The final Y-value for {2} is {1}.", _moduleID, y, presses[2][j]);
                            }

                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[2][0], yValues[2, 0], yValues[2, 0] + 5, yValues[2, 0] + 10);
                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[2][1], yValues[2, 1], yValues[2, 1] + 5, yValues[2, 1] + 10);
                            Debug.LogFormat("[Simon's Sums #{0}] {1} needs to be pressed when the sum of the timer digits is {2}, {3}, or {4}.", _moduleID, presses[2][2], yValues[2, 2], yValues[2, 2] + 5, yValues[2, 2] + 10);

                            break;
                    }

                    stage++;
            displayText = stage.ToString();
                }
            }

        }
    }

    void HLButton(KMSelectable Button)
    {
        if (!_moduleSolved)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Buttons[i] == Button)
                {
                    tempHL = i;
                    break;
                }
            }
            colorText = "" + order[tempHL];
        }
        else
        {
            colorText = "";
        }
    }
    void HLEndButton(KMSelectable Button)
    {
        colorText = "";
    }

    private IEnumerator SolveAnimation()
    {
        yield return new WaitForSeconds(0.5f);

        StartCoroutine(sequences[4]);

        for (int j = 0; j < 6; j++)
        {
            colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
            ButtonMaterials[j].material = materials[0];
            Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
        }

        Audio.PlaySoundAtTransform("Solve Sound", transform);

        for (int j = 0; j < 6; j++)
        {
            colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
            ButtonMaterials[j].material = materials[1];
            Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
            yield return new WaitUntil(() => elapsed > (float) ((j + 2) * 0.12));
        }

        if (Bomb.GetTime() < 60)
        {
            // Skips the solve animation if there is less than one minute remaining on the bomb timer
            Module.HandlePass();
            StartCoroutine(sequences[3]);
            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[j].material = materials[0];
                Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)((j + 9.6) * 0.12));
            }
        }
        else
        {
            // Solve animation xD

            // North clockwise
            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[j].material = materials[0];
                Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)((j + 8) * 0.12));
            }

            yield return new WaitUntil(() => elapsed > 2.52f);

            // RYGCBM order 
            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[1];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)((j + 22) * 0.12));
            }

            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[0];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)((j + 28) * 0.12));
            }

            yield return new WaitUntil(() => elapsed > 4.7f);

            // Primaries

            for (int j = 0; j < 6; j += 2)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[1];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            yield return new WaitUntil(() => elapsed > 5.4f);

            for (int j = 0; j < 6; j += 2)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[0];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            // Secondaries

            for (int j = 1; j < 6; j += 2)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[1];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            yield return new WaitUntil(() => elapsed > 6.5f);

            for (int j = 1; j < 6; j += 2)
            {
                colorStored = Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RYGCBM"[j])].material = materials[0];
                Buttons[order.IndexOf("RYGCBM"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            // Opposite counterclockwise

            for (int j = 19; j > 0; j--)
            {
                colorStored = Buttons[j % 6].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[j % 6].material = materials[1];
                Buttons[j % 6].GetComponent<MeshRenderer>().material.color = colorStored;
                colorStored = Buttons[(j + 3) % 6].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[(j + 3) % 6].material = materials[1];
                Buttons[(j + 3) % 6].GetComponent<MeshRenderer>().material.color = colorStored;

                yield return new WaitUntil(() => elapsed > (float)((19 - j + 65) * 0.1));

                colorStored = Buttons[j % 6].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[j % 6].material = materials[0];
                Buttons[j % 6].GetComponent<MeshRenderer>().material.color = colorStored;
                colorStored = Buttons[(j + 3) % 6].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[(j + 3) % 6].material = materials[0];
                Buttons[(j + 3) % 6].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            // Down

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    colorStored = Buttons[0].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[0].material = materials[1 - j];
                    Buttons[0].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(8.4 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[1].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[1].material = materials[1 - j];
                    Buttons[1].GetComponent<MeshRenderer>().material.color = colorStored;
                    colorStored = Buttons[5].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[5].material = materials[1 - j];
                    Buttons[5].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(8.52 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[2].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[2].material = materials[1 - j];
                    Buttons[2].GetComponent<MeshRenderer>().material.color = colorStored;
                    colorStored = Buttons[4].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[4].material = materials[1 - j];
                    Buttons[4].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(8.64 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[3].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[3].material = materials[1 - j];
                    Buttons[3].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(8.76 + (4 * j + 8 * i) * 0.12));
                }
            }

            // Up

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    colorStored = Buttons[3].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[3].material = materials[1 - j];
                    Buttons[3].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(10.32 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[2].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[2].material = materials[1 - j];
                    Buttons[2].GetComponent<MeshRenderer>().material.color = colorStored;
                    colorStored = Buttons[4].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[4].material = materials[1 - j];
                    Buttons[4].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(10.44 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[1].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[1].material = materials[1 - j];
                    Buttons[1].GetComponent<MeshRenderer>().material.color = colorStored;
                    colorStored = Buttons[5].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[5].material = materials[1 - j];
                    Buttons[5].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(10.56 + (4 * j + 8 * i) * 0.12));
                    colorStored = Buttons[0].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[0].material = materials[1 - j];
                    Buttons[0].GetComponent<MeshRenderer>().material.color = colorStored;
                    yield return new WaitUntil(() => elapsed > (float)(10.68 + (4 * j + 8 * i) * 0.12));
                }
            }

            // RGBCMY Order

            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[order.IndexOf("RGBCMY"[j])].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[order.IndexOf("RGBCMY"[j])].material = materials[1];
                Buttons[order.IndexOf("RGBCMY"[j])].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)(12.24 + j * 0.12));
            }

            yield return new WaitUntil(() => elapsed > 14f);

            // Triangular/Y Shape (Alternating)

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[j].material = materials[(j + i) % 2];
                    Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
                }
                yield return new WaitUntil(() => elapsed > (float)(14.1 + i * 0.11));
            }
            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[j].material = materials[0];
                Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            // Northwest Counterclockwise

            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[5-j].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[5 - j].material = materials[1];
                Buttons[5 - j].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)(15.3 + j * 0.1));
            }

            Module.HandlePass(); // Actually solves
            StartCoroutine(sequences[3]);

            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    colorStored = Buttons[j].GetComponent<MeshRenderer>().material.color;
                    ButtonMaterials[j].material = materials[i % 2];
                    Buttons[j].GetComponent<MeshRenderer>().material.color = colorStored;
                }
                yield return new WaitUntil(() => elapsed > (float)(15.9 + i * 0.1));
            }

            for (int j = 0; j < 6; j++)
            {
                colorStored = Buttons[5 - j].GetComponent<MeshRenderer>().material.color;
                ButtonMaterials[5 - j].material = materials[0];
                Buttons[5 - j].GetComponent<MeshRenderer>().material.color = colorStored;
                yield return new WaitUntil(() => elapsed > (float)(18.3 + j * 0.12));
            }

        }
            _moduleDeactivated = true;
    }

    private IEnumerator TextAnimation()
    {
        switch (UnityEngine.Random.Range(0, 12))
        {
            case 0:
            solveText = "YOU JUST SOLVED SIMON'S SUMS!";
            break;
            case 1:
            solveText = "TOOK YOU LONG ENOUGH... GG";
            break;
            case 2:
            solveText = "MODULE SOLVED! NEVER AGAIN.";
            break;
            case 3:
            solveText = "YOU DID THIS MOD FOR THE FUNNIES, RIGHT?";
            break;
            case 4:
            solveText = "THANKS FOR PLAYING MY MOD <3";
            break;
            case 5:
            solveText = "YOU ARE A LEGS. GOOD WORK.";
            break;
            case 6:
            solveText = "YOU DID IT. NOW GO OUTSIDE OR SMTH :)";
            break;
            case 7:
            solveText = "SIMON MADNESS PT. II COMING SOON?! :(";
            break;
            case 8:
            solveText = "*INSERT GROOVY SOLVE MESSAGE HERE*";
            break;
            case 9:
            solveText = "MODULE MADE BY BCMGF1137/19#5398";
            break;
            case 10:
            solveText = "SOLVE SOUND: \"MINUIT\" BY RYZMIK";
            break;
            case 11:
            solveText = "YOU NOTICED MY SOLVE TEXT... I'M IMPRESSED";
            break;
        }

        for (int i = 0; i < solveText.Length; i++)
        {
            displayText = "" + solveText[i];
            yield return new WaitForSeconds(0.07f);
            displayText = "";
            yield return new WaitForSeconds(0.07f);
        }
    }

    IEnumerator TimeFix()
    {
        while (elapsed < 25f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // Update is called once per frame
    // Colorblind mode
    void Update () {
		if (!_moduleSolved && colorblindMode)
        {
            if (colorText == "")
            {
                newText.text = displayText;
            }
            else
            {
                newText.text = colorText;
            }
        }
        else newText.text = displayText;
	}

    // Pre-TP
    private bool isInputValid(string a)
    {
        int temp1 = 0;
        bool preformed = int.TryParse(a, out temp1);
        return (preformed == true && (temp1 >= 0 && temp1 <= 14));
    }
    private bool isInputValid2(string a)
    {
        return a.EqualsIgnoreCase("r") || a.EqualsIgnoreCase("g") || a.EqualsIgnoreCase("b") || a.EqualsIgnoreCase("c") || a.EqualsIgnoreCase("m") || a.EqualsIgnoreCase("y") || a.EqualsIgnoreCase("red") || a.EqualsIgnoreCase("green") || a.EqualsIgnoreCase("blue") || a.EqualsIgnoreCase("cyan") || a.EqualsIgnoreCase("magenta") || a.EqualsIgnoreCase("yellow");
    }
    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <X> <Y> [Presses the color X (red/green/blue/cyan/magenta/yellow, or r/g/b/c/m/y) when the sum of the timer digits is Y (needs to be an integer in the range 0-14 inclusive)]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');

        if (parameters.Length == 2)
        {
            if (isInputValid2(parameters[0]))
            {
                if (isInputValid(parameters[1]))
                {
                    yield return null;
                    int timepress = 0;
                    int.TryParse(parameters[1], out timepress);
                    if (parameters[0].EqualsIgnoreCase("r") || parameters[0].EqualsIgnoreCase("red"))
                    {
                        yield return "sendtochat We are pressing red when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("R")].OnInteract();
                    }
                    else if (parameters[0].EqualsIgnoreCase("g") || parameters[0].EqualsIgnoreCase("green"))
                    {
                        yield return "sendtochat We are pressing green when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("G")].OnInteract();
                    }
                    else if (parameters[0].EqualsIgnoreCase("b") || parameters[0].EqualsIgnoreCase("blue"))
                    {
                        yield return "sendtochat We are pressing blue when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("B")].OnInteract();
                    }
                    else if (parameters[0].EqualsIgnoreCase("c") || parameters[0].EqualsIgnoreCase("cyan"))
                    {
                        yield return "sendtochat We are pressing cyan when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("C")].OnInteract();
                    }
                    else if (parameters[0].EqualsIgnoreCase("m") || parameters[0].EqualsIgnoreCase("magenta"))
                    {
                        yield return "sendtochat We are pressing magenta when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("M")].OnInteract();
                    }
                    else if (parameters[0].EqualsIgnoreCase("y") || parameters[0].EqualsIgnoreCase("yellow"))
                    {
                        yield return "sendtochat We are pressing yellow when the sum of the last two digits of the timer is " + timepress + "!";
                        while (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6) != timepress) yield return "trycancel Press cancelled.";
                        Buttons[order.IndexOf("Y")].OnInteract();
                    }
                }
                else yield return "sendtochaterror The specified sum to press is invalid!";
            }
            else yield return "sendtochaterror That color doesn't exist in the module!";
        }
        else yield return "sendtochaterror That command doesn't exist!";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Simon's Sums #{0}] Autosolving (TP)", _moduleID);
        autosolving = true;
        while (stage == 1)
        {
            yield return new WaitUntil(() => ((stage != 1) || (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6)) % 5 == yValues[0, submissionColors.Length]));
            if (stage != 1) break;
            try
            {
                Buttons[order.IndexOf(presses[0][submissionColors.Length].ToString())].OnInteract();
            }
            catch
            {
                // Sometimes an IndexOutOfRangeException happens for some reason :/
            }
        }
        while (stage == 2)
        {
            yield return new WaitUntil(() => ((stage != 2) || (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6)) % 5 == yValues[1, submissionColors.Length]));
            if (stage != 2) break;
            try
            {
                Buttons[order.IndexOf(presses[1][submissionColors.Length].ToString())].OnInteract();
            }
            catch
            {
                // Sometimes an IndexOutOfRangeException happens for some reason :/
            }
        }
        while (stage == 3)
        {
            while (submissionColors.Length < 3)
            {
                yield return new WaitUntil(() => ((submissionColors.Length == 3) || (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6)) % 5 == yValues[2, submissionColors.Length]));
                if (submissionColors.Length == 3) break;
                try
                {
                    Buttons[order.IndexOf(presses[2][submissionColors.Length].ToString())].OnInteract();
                }
                catch
                {
                    // Sometimes an IndexOutOfRangeException happens for some reason :/
                }
            }
            while (submissionColors.Length == 3)
            {
                yield return new WaitUntil(() => ((submissionColors.Length != 3) || (((int)Bomb.GetTime() % 10) + (((int)Bomb.GetTime() / 10) % 6)) == yValues[2, 3]));
                if (submissionColors.Length != 3) break;
                try
                {
                    Buttons[order.IndexOf(presses[2][3])].OnInteract();
                }
                catch
                {
                    // Sometimes an IndexOutOfRangeException happens for some reason :/
                }
            }
            yield return new WaitForSeconds(1);
            if (_moduleSolved) break;
        }
    }
}
