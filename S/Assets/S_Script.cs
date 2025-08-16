using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;
using UnityEngine;
using KModkit;

public class S_Script : MonoBehaviour
{

    static int _moduleIDCounter = 1;
    int _moduleID;

    // Public variables

    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public MeshRenderer[] buttonMaterials;
    public MeshRenderer[] stageMaterials;
    public Light[] lights;
    public KMSelectable[] buttons;

    public KMColorblindMode CBM;

    public Material stageOn;
    public Material stageOff;

    public TextMesh colorblindText;

    // Private variables

    private bool moduleActivated = false; // When the lights turn on, the mod will generate colors.
    private bool inputsReceived = false; // The module does not play sounds until inputs are received.
    private bool isInteractable = false; // Nothing happens if you attempt to interact with the mod while animating.
    private bool inSequence = false; // Is the module currently running the sequence?
    private bool colorblindMode = false; // Is colorblind mode enabled?

    private static string[] names = { "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose" };
    private Dictionary<string, Color> colors = names.Select((n, i) => new
    {
        Name = n,
        Color = Color.HSVToRGB(i / 12f, 1f, 1f)
    }).ToDictionary(x => x.Name, x => x.Color);

    private List<Dictionary<string, Color>> stageColors = new List<Dictionary<string, Color>>();
    private string missingColor = "";
    private string[] missingColorList = { "", "", "" };
    private List<string>[] flashes = { new List<string>(), new List<string>(), new List<string>() };
    private List<int>[] flashPositions = { new List<int>(), new List<int>(), new List<int>() };
    private List<string>[] answers = { new List<string>(), new List<string>(), new List<string>() };
    private List<int>[] answerPositions = { new List<int>(), new List<int>(), new List<int>() };
    private string binaryString = ""; // Determines the sounds played.
    private string modText = ""; // For colorblind mode.
    private List<string> submission = new List<string>(); // Stage 1 and Stage 2 submissions
    private string submission3 = ""; // Stage 3 submission (user input)

    private int stage = 1; // Current stage number
    private int maxStage = 1; // If you strike, stages will not be regenerated.

    private bool moduleSolved = false;

    private string[,] stage1Strings = new string[,]
    {
        {
            "Cyan","Azure","Blue","Violet","Magenta","Rose","Red","Orange","Yellow","Lime","Green","Jade"
        },
        {
            "Azure","Blue","Cyan","Green","Jade","Lime","Magenta","Orange","Red","Rose","Violet","Yellow"
        },
        {
            "Blue","Cyan","Magenta","Green","Yellow","Red","Orange","Violet","Jade","Rose","Azure","Lime"
        },
        {
            "Jade","Magenta","Lime","Blue","Orange","Cyan","Rose","Green","Violet","Yellow","Azure","Red"
        },
        {
            "Red","Blue","Cyan","Jade","Lime","Rose","Azure","Green","Orange","Violet","Yellow","Magenta"
        }
    };

    private List<IEnumerator> enumerators = new List<IEnumerator>();

    private Coroutine blinker;
    private Coroutine preparer;

    private int R, S;

    private string S3;

    void Awake()
    {
        _moduleID = _moduleIDCounter++;

        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate ()
            {
                StartCoroutine(PressButton(button));
                return false;
            };
            button.OnHighlight += delegate ()
            {
                HLButton(button);
            };
            button.OnHighlightEnded += delegate ()
            {
                HLEndButton(button);
            };
        }

        Module.OnActivate += delegate
        {
            moduleActivated = true;
        };
    }

    // Use this for initialization
    void Start()
    {
        foreach (MeshRenderer m in buttonMaterials)
        {
            m.material.color = new Color(0.5f, 0.5f, 0.5f);
        }

        foreach (Light l in lights)
        {
            l.enabled = false;
        }

        colorblindMode = CBM.ColorblindModeActive;

        enumerators.Add(Sequence());
        //enumerators.Add(PrepareColors());

        StartCoroutine(WaitToGenerate());
    }

    IEnumerator WaitToGenerate()
    {
        yield return new WaitUntil(() => moduleActivated);
        GenerateStage1();
    }

    void GenerateStage1()
    {
        // Initialize colors

        Debug.LogFormat("[S #{0}] =STAGE 1 GENERATION=", _moduleID);

        stageColors.Add(newStageColors(colors));
        string str = "The colors (positions 0-10) are: ";
        for (int i = 0; i < stageColors.ElementAt(0).Count; i++)
        {
            str += stageColors.ElementAt(0).ElementAt(i).Key + ", ";
        }
        Debug.LogFormat("[S #{0}] " + str.Substring(0, str.Length - 2) + ".", _moduleID);
        Debug.LogFormat("[S #{0}] The missing color is " + missingColor + ".", _moduleID);

        missingColorList[0] = missingColor;

        // Flashes

        for (int i = 0; i < 5; i++)
        {
            flashPositions[0].Add(Rnd.Range(0, 11));
            flashes[0].Add(stageColors.ElementAt(0).ElementAt(flashPositions[0][i]).Key);
        }

        Debug.LogFormat("[S #{0}] The flashes are: {1}.", _moduleID, flashes[0].Join(", "));
        Debug.LogFormat("[S #{0}] The positions of the flashes are: {1}.", _moduleID, flashPositions[0].Join(", "));

        // Simon Says Substitutions

        for (int i = 0; i < 5; i++)
        {
            str = stage1Strings[i, Array.IndexOf(names, flashes[0][i])];
            if (str == missingColor) str = flashes[0][i];
            answers[0].Add(str);
        }

        // Answer generated

        Debug.LogFormat("[S #{0}] The answer is: {1}.", _moduleID, answers[0].Join(", "));

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 12; j++)
            {
                if (answers[0][i] == stageColors[0].ElementAt(j).Key)
                {
                    answerPositions[0].Add(j);
                    break;
                }
            }
        }

        Debug.LogFormat("[S #{0}] The positions of the answer are: {1}.", _moduleID, answerPositions[0].Join(", "));

        preparer = StartCoroutine(PrepareColors());
    }

    void GenerateStage2()
    {
        // Initialize colors

        Debug.LogFormat("[S #{0}] =STAGE 2 GENERATION=", _moduleID);

        stageColors.Add(newStageColors(colors));
        string str = "The colors (positions 0-10) are: ";
        for (int i = 0; i < stageColors.ElementAt(1).Count; i++)
        {
            str += stageColors.ElementAt(1).ElementAt(i).Key + ", ";
        }
        Debug.LogFormat("[S #{0}] " + str.Substring(0, str.Length - 2) + ".", _moduleID);
        Debug.LogFormat("[S #{0}] The missing color is " + missingColor + ".", _moduleID);

        missingColorList[1] = missingColor;

        // Flashes

        for (int i = 0; i < 6; i++)
        {
            flashPositions[1].Add(Rnd.Range(0, 11));
            flashes[1].Add(stageColors.ElementAt(1).ElementAt(flashPositions[1][i]).Key);
        }

        Debug.LogFormat("[S #{0}] The flashes are: {1}.", _moduleID, flashes[1].Join(", "));
        Debug.LogFormat("[S #{0}] The positions of the flashes are: {1}.", _moduleID, flashPositions[1].Join(", "));

        // Determining R

        Debug.LogFormat("[S #{0}] The list of positions is: {1}, {2}.", _moduleID, flashPositions[1].Join(", "), answerPositions[0].Join(", "));
        {
            long x = 0;

            for (int i = 0; i < 6; i++)
            {
                x = x * 11 + flashPositions[1][i];
            }
            for (int i = 0; i < 5; i++)
            {
                x = x * 11 + answerPositions[0][i];
            }

            R = (int)(x % 100);
            Debug.LogFormat("[S #{0}] The obtained value is {1}. R is equal to {2}.", _moduleID, x, ("" + R).PadLeft(2, '0'));
        }

        if (Bomb.GetSolvableModuleNames().Contains("Bamboozled Again"))
        {
            R = 10 * (R % 10) + R / 10;
            Debug.LogFormat("[S #{0}] However, Bamboozled Again is present, so R is actually equal to {1}.", _moduleID, ("" + R).PadLeft(2, '0'));
        }

        // Statuses of colors

        string colorStats = "";

        for (int i = 0; i < 12; i++)
        {
            int y = 0;
            if (flashes[0].Contains(names[i]))
            {
                y++;
            }
            if (answers[0].Contains(names[i]))
            {
                y += 2;
            }
            colorStats += "" + y;
            switch (y)
            {
                case 0:
                    Debug.LogFormat("[S #{0}] {1} had no involvement in Stage 1.", _moduleID, names[i]);
                    break;
                case 1:
                    Debug.LogFormat("[S #{0}] {1} flashed in Stage 1.", _moduleID, names[i]);
                    break;
                case 2:
                    Debug.LogFormat("[S #{0}] {1} was pressed in Stage 1.", _moduleID, names[i]);
                    break;
                case 3:
                    Debug.LogFormat("[S #{0}] {1} flashed and was pressed in Stage 1.", _moduleID, names[i]);
                    break;
            }
        }

        // S From Bamboozled Again

        // Part 1: Color modifications

        S = R;

        for (int i = 0; i < 6; i++)
        {
            modifyS(flashes[1][i] + " " + colorStats[Array.IndexOf(names, flashes[1][i])]);
        }

        // Part 2: Edgework modifications

        str = Bomb.GetIndicators().Join();
        Debug.LogFormat("[S #{0}] The indicators are {1}.", _moduleID, str);

        for (int aghhhhhhhhh = 0; aghhhhhhhhh < 1; aghhhhhhhhh++) // Really obscure variable so I can easily break out in unicorn case
        {
            for (int i = 0; i < 5; i++)
            {
                if (str.Contains("SIMON"[i] + "")) aghhhhhhhhh++;
            }

            if (aghhhhhhhhh == 5)
            {
                Debug.LogFormat("[S #{0}] You can spell \"SIMON\" using your indicators. Skipping edgework step...", _moduleID);
                break;
            }

            if (str.Contains("SND") || str.Contains("SIG"))
            {
                Debug.LogFormat("[S #{0}] An SND or SIG indicator is present. Applying: Red.", _moduleID);
                modifyS("Red", colorStats);
            }

            if (str.Contains("MSA") || str.Contains("NSA"))
            {
                Debug.LogFormat("[S #{0}] An MSA or NSA indicator is present. Applying: Cyan.", _moduleID);
                modifyS("Cyan", colorStats);
            }

            if (Bomb.GetSolvableModuleNames().Contains("Simon's Sums") || Bomb.GetSolvableModuleNames().Contains("OmegaForget"))
            {
                Debug.LogFormat("[S #{0}] \"Simon's Sums\" or \"OmegaForget\" is present. Applying: {1}.", _moduleID, missingColorList[0]);
                modifyS(missingColorList[0], colorStats);
            }

            if (Bomb.IsPortPresent(Port.Serial) || Bomb.IsPortPresent(Port.PS2))
            {
                Debug.LogFormat("[S #{0}] A Serial or PS/2 port is present. Applying: Orange.", _moduleID);
                modifyS("Orange", colorStats);
                if (Bomb.IsPortPresent(Port.StereoRCA))
                {
                    Debug.LogFormat("[S #{0}] Additionally, a Stereo RCA port is present. Applying: Violet.", _moduleID);
                    modifyS("Violet", colorStats);
                }
            }

            if (Bomb.GetModuleNames().Count(x => x.Length == 1 && x != "S") > 0)
            {
                Debug.LogFormat("[S #{0}] A one-character module other than S exists. Applying: Jade.", _moduleID);
                modifyS("Jade", colorStats);

                if (Bomb.GetSolvableModuleNames().Count(x => x == "7" || x == "h") > 0)
                {
                    Debug.LogFormat("[S #{0}] \"7\" or \"h\" is present. Applying: Rose.", _moduleID);
                    modifyS("Rose", colorStats);
                }
            }

            if (Bomb.GetModuleNames().Count(x => x.Contains("Simon")) > 0)
            {
                Debug.LogFormat("[S #{0}] Another module with the word \"Simon\" is present. Applying: Azure.", _moduleID);
                modifyS("Azure", colorStats);
            }

            {
                int temp = Bomb.GetSerialNumber().Count(x => x == 'S');

                if (temp > 0)
                {
                    Debug.LogFormat("[S #{0}] The serial number contains an S. Applying: Rose.", _moduleID);
                    modifyS("Rose", colorStats);

                    if (temp > 1)
                    {
                        Debug.LogFormat("[S #{0}] The serial number contains more than one S. Applying: Jade.", _moduleID);
                        modifyS("Jade", colorStats);
                    }
                }
            }

            if (Bomb.GetSolvableModuleNames().Distinct().Count(x => x == "Cruel Boolean Wires" || x == "Modulo Maze") <= 1)
            {
                Debug.LogFormat("[S #{0}] At most one distinct module is present out of \"Cruel Boolean Wires\" and \"Modulo Maze\". Applying: {1}. ", _moduleID, missingColorList[1]);
                modifyS(missingColorList[1], colorStats);
            }

            {
                int temp = Bomb.GetSerialNumber().Distinct().Count(x => x == '6' || x == '7');

                if (temp > 0)
                {
                    Debug.LogFormat("[S #{0}] The serial number contains a 6 or 7. Applying: Blue, Cyan.", _moduleID);
                    modifyS("Blue", colorStats);
                    modifyS("Cyan", colorStats);

                    if (temp > 1)
                    {
                        Debug.LogFormat("[S #{0}] The serial number contains both a 6 and a 7. Applying: Lime.", _moduleID);
                        modifyS("Lime", colorStats);
                    }
                }
            }

            {
                int temp = 0;
                temp += Bomb.GetPortPlates().Where(plate => plate.Contains("DVI") && !plate.Contains("PS2") && !plate.Contains("RJ45") && !plate.Contains("StereoRCA")).Any() ? 1 : 0;
                temp += Bomb.GetBatteryCount() == Bomb.GetBatteryHolderCount() && Bomb.GetBatteryCount() >= 1 ? 1 : 0;
                temp += Bomb.IsIndicatorPresent("SND") || Bomb.IsIndicatorPresent("IND") ? 1 : 0;

                if (temp <= 1)
                {
                    Debug.LogFormat("[S #{0}] D's anti-unicorn rule does not apply. Applying: Magenta, Green.", _moduleID);
                    modifyS("Magenta", colorStats);
                    modifyS("Green", colorStats);

                    if (Bomb.GetSolvableModuleNames().Contains("D"))
                    {
                        Debug.LogFormat("[S #{0}] \"D\" is present. Applying: Yellow.", _moduleID);
                        modifyS("Yellow", colorStats);
                    }
                }
            }

            if (Bomb.GetModuleNames().Count >= 47)
            {
                Debug.LogFormat("[S #{0}] There are 47 or more modules. Applying: {1}.", _moduleID, stageColors[1].ElementAt(5).Key);
                modifyS(stageColors[1].ElementAt(5).Key, colorStats);

                if (Bomb.GetModuleNames().Count >= 71)
                {
                    Debug.LogFormat("[S #{0}] There are 71 or more modules. Applying: Lime.", _moduleID);
                    modifyS("Lime", colorStats);
                }
            }

            if (Bomb.GetStrikes() == 0)
            {
                Debug.LogFormat("[S #{0}] There are no strikes. Applying: Yellow.", _moduleID);
                modifyS("Yellow", colorStats);
            }
        }

        str = ("" + S).PadLeft(2, '0');

        Debug.LogFormat("[S #{0}] Your final S-value is {1}.", _moduleID, str);

        // Answer retrieval
        for (int i = 0; i < 2; i++)
        {
            switch (int.Parse(str[i] + ""))
            {
                case 0:
                    answers[1].Add("Red");
                    answers[1].Add(stageColors[1].ElementAt(10).Key);
                    answers[1].Add("Jade");
                    answers[1].Add(stageColors[1].ElementAt(0).Key);
                    break;
                case 1:
                    answers[1].Add("Cyan");
                    answers[1].Add("Blue");
                    answers[1].Add(stageColors[1].ElementAt(4).Key);
                    answers[1].Add(stageColors[1].ElementAt(2).Key);
                    break;
                case 2:
                    answers[1].Add(stageColors[1].ElementAt(8).Key);
                    answers[1].Add("Magenta");
                    answers[1].Add(stageColors[1].ElementAt(3).Key);
                    answers[1].Add("Green");
                    break;
                case 3:
                    answers[1].Add("Violet");
                    answers[1].Add(stageColors[1].ElementAt(6).Key);
                    answers[1].Add(stageColors[1].ElementAt(10).Key);
                    answers[1].Add("Cyan");
                    break;
                case 4:
                    answers[1].Add(stageColors[1].ElementAt(9).Key);
                    answers[1].Add(stageColors[1].ElementAt(4).Key);
                    answers[1].Add("Orange");
                    answers[1].Add("Yellow");
                    break;
                case 5:
                    answers[1].Add("Lime");
                    answers[1].Add(stageColors[1].ElementAt(1).Key);
                    answers[1].Add("Azure");
                    answers[1].Add(stageColors[1].ElementAt(7).Key);
                    break;
                case 6:
                    answers[1].Add("Yellow");
                    answers[1].Add(stageColors[1].ElementAt(2).Key);
                    answers[1].Add(stageColors[1].ElementAt(5).Key);
                    answers[1].Add("Jade");
                    break;
                case 7:
                    answers[1].Add(stageColors[1].ElementAt(1).Key);
                    answers[1].Add("Rose");
                    answers[1].Add("Lime");
                    answers[1].Add(stageColors[1].ElementAt(0).Key);
                    break;
                case 8:
                    answers[1].Add(stageColors[1].ElementAt(6).Key);
                    answers[1].Add("Red");
                    answers[1].Add("Blue");
                    answers[1].Add(stageColors[1].ElementAt(8).Key);
                    break;
                case 9:
                    answers[1].Add("Green");
                    answers[1].Add(stageColors[1].ElementAt(7).Key);
                    answers[1].Add("Orange");
                    answers[1].Add(stageColors[1].ElementAt(5).Key);
                    break;
            }
        }

        // Missing color handling

        for (int i = 0; i < 8; i++)
        {
            if (answers[1][i] == missingColorList[1])
            {
                answers[1][i] = names[(Array.IndexOf(names, missingColorList[1]) + 6) % 12];
            }
        }

        // Answer generated

        Debug.LogFormat("[S #{0}] The answer is: {1}.", _moduleID, answers[1].Join(", "));

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 12; j++)
            {
                if (answers[1][i] == stageColors[1].ElementAt(j).Key)
                {
                    answerPositions[1].Add(j);
                    break;
                }
            }
        }

        Debug.LogFormat("[S #{0}] The positions of the answer are: {1}.", _moduleID, answerPositions[1].Join(", "));
    }

    void GenerateStage3()
    {
        // Initialize colors

        Debug.LogFormat("[S #{0}] =STAGE 3 GENERATION=", _moduleID);

        stageColors.Add(newStageColors(colors));
        string str = "The colors (positions 0-10) are: ";
        for (int i = 0; i < stageColors.ElementAt(2).Count; i++)
        {
            str += stageColors.ElementAt(2).ElementAt(i).Key + ", ";
        }
        Debug.LogFormat("[S #{0}] " + str.Substring(0, str.Length - 2) + ".", _moduleID);
        Debug.LogFormat("[S #{0}] The missing color is " + missingColor + ".", _moduleID);

        missingColorList[2] = missingColor;

        // Flashes

        for (int i = 0; i < 7; i++)
        {
            flashPositions[2].Add(Rnd.Range(0, 11));
            flashes[2].Add(stageColors.ElementAt(2).ElementAt(flashPositions[2][i]).Key);
        }

        Debug.LogFormat("[S #{0}] The flashes are: {1}.", _moduleID, flashes[2].Join(", "));
        Debug.LogFormat("[S #{0}] The positions of the flashes are: {1}.", _moduleID, flashPositions[2].Join(", "));

        // Stage 3 values
        List<int> L = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            int x = 0;
            Debug.LogFormat("[S #{0}] Flash {1} of Stage 3: {2} in position {3}. Value: {4} + {3} = {5}.", _moduleID, i + 1, flashes[2][i], flashPositions[2][i], Array.IndexOf(names, flashes[2][i]), flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]));
            x += flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]);
            i++;
            Debug.LogFormat("[S #{0}] Flash {1} of Stage 3: {2} in position {3}. Value: {4} + {3} = {5}.", _moduleID, i + 1, flashes[2][i], flashPositions[2][i], Array.IndexOf(names, flashes[2][i]), flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]));
            x += flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]);
            Debug.LogFormat("[S #{0}] The sum of the values in this pair is {1}.", _moduleID, x);
            L.Add(x);
        }
        for (int i = 6; i < 7; i++)
        {
            int x = 0;
            Debug.LogFormat("[S #{0}] Flash {1} of Stage 3: {2} in position {3}. Value: {4} + {3} = {5}.", _moduleID, i + 1, flashes[2][i], flashPositions[2][i], Array.IndexOf(names, flashes[2][i]), flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]));
            x += flashPositions[2][i] + Array.IndexOf(names, flashes[2][i]);
            x += R;
            Debug.LogFormat("[S #{0}] R is equal to " + R + ". The sum of R and the last value is {1}.", _moduleID, x);
            L.Add(x);
        }

        int[] missingValues = missingColorList.Select(x => Array.IndexOf(names, x)).ToArray();
        L.Add(missingValues.Sum() + S);

        Debug.LogFormat("[S #{0}] The colors missing from the module are: {1}. Value: {2} = {3}.", _moduleID, missingColorList.Join(", "), missingValues.Join(" + "), L.Last() - S);
        Debug.LogFormat("[S #{0}] S is equal to " + S + ". The sum of S and the missing color value is {1}.", _moduleID, L.Last());

        Debug.LogFormat("[S #{0}] The values in list L are: {1}.", _moduleID, L.Join(", "));

        double V = L.Average();

        Debug.LogFormat("[S #{0}] The mean of L is {1}.", _moduleID, V);

        double[] L2 = L.Select(x => Math.Abs(x - V)).ToArray();

        Debug.LogFormat("[S #{0}] The differences in list L are: {1}.", _moduleID, L2.Join(", "));

        L2 = L2.Select(x => x * x).ToArray();

        Debug.LogFormat("[S #{0}] The squares of the differences in list L are: {1}.", _moduleID, L2.Join(", "));

        V = L2.Sum();

        Debug.LogFormat("[S #{0}] The sum of the square differences is equal to {1}.", _moduleID, V);
        Debug.LogFormat("[S #{0}] {1} / 10 = {2}.", _moduleID, V, V / 4d);

        V = V / 4d;

        Debug.LogFormat("[S #{0}] The square root of {1} is {2}, which is our standard deviation.", _moduleID, V, Math.Sqrt(V));

        V = Math.Sqrt(V);
        while (V < 1e3)
        {
            V *= 10;
        }

        int A = (int)V;

        Debug.LogFormat("[S #{0}] A is equal to " + A + ".", _moduleID);

        int B = Bomb.GetModuleNames().Count(x => x.ContainsIgnoreCase("S"));

        //if (B == 1) Debug.LogFormat("[S #{0}] There is 1 module that contains an S.", _moduleID, B);

        Debug.LogFormat("[S #{0}] B is equal to " + A + " * (sqrt(" + S + " + 111.1)).", _moduleID);

        B = (int)(A * (Math.Sqrt(S + 111.1)));

        Debug.LogFormat("[S #{0}] B is equal to " + B + ".", _moduleID);

        int X = 1;
        int Y = 1;

        while (Y > 0)
        {
            X = A / B;
            Y = A % B;
            Debug.LogFormat("[S #{0}] " + A + " / " + B + " = " + X + " R " + Y + ".", _moduleID);
            S3 += X;
            A = B;
            B = Y;
            if (Y > 0) S3 += ",";
        }

        Debug.LogFormat("[S #{0}] S3 is currently \"" + S3 + "\".", _moduleID);

        if (Bomb.GetSolvableModuleNames().Contains("Decay"))
        {
            str = S3;
            S3 = "";

            for (int i = 0; i < str.Length; i++)
            {
                S3 += str[str.Length - i - 1];
            }
            Debug.LogFormat("[S #{0}] However, Decay is present, so S3 becomes \"{1}\".", _moduleID, S3);
        }

        S3 += ",,";

        Debug.LogFormat("[S #{0}] The submission sequence for Stage 3 is \"{1}\".", _moduleID, S3);
    }

    void modifyS(string modification, string st)
    {
        modifyS(modification + " " + st[Array.IndexOf(names, modification)]);
    }

    void modifyS(string modification)
    {
        string str2 = "";
        int temp = S;
        switch (modification)
        {
            // Red
            case "Red 0": // Do nothing
                break;
            case "Red 1": // Subtract the number from one hundred
                S = 100 - S;
                break;
            case "Red 2": // Add ten times the first digit
                S += 10 * (S / 10);
                break;
            case "Red 3": // Subtract the first digit
                S -= S / 10;
                break;
            // Orange
            case "Orange 0": // Add the digital root
                S += DR(S);
                break;
            case "Orange 1": // Replace the first digit with nine minus the second
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + "9876543210".IndexOf(str2[1]) + str2[1];
                S = int.Parse(str2);
                break;
            case "Orange 2": // Replace the first digit with the difference between digits
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + Math.Abs((S % 10) - (S / 10)) + str2[1];
                S = int.Parse(str2);
                break;
            case "Orange 3": // Replace the second digit with the first
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + str2[0] + str2[0];
                S = int.Parse(str2);
                break;
            // Yellow
            case "Yellow 0": // Double the number
                S *= 2;
                break;
            case "Yellow 1": // Add the number to fifty
                S += 50;
                break;
            case "Yellow 2": // Add nine minus the first digit
                S += 9 - S / 10;
                break;
            case "Yellow 3": // Add the second digit
                S += S % 10;
                break;
            // Lime
            case "Lime 0": // Add the product of the digits
                S += (S % 10) * (S / 10);
                break;
            case "Lime 1": // Add the sum of the first digit and the digital root
                S += (S / 10) + DR(S);
                break;
            case "Lime 2": // Add the sum of the second digit and the digital root
                S += (S % 10) + DR(S);
                break;
            case "Lime 3": // Subtract the higher digit
                S -= Math.Max(S % 10, S / 10);
                break;
            // Green
            case "Green 0": // Add the sum of the digits
                S += (S % 10) + (S / 10);
                break;
            case "Green 1": // Subtract eighteen minus the sum of the digits
                S -= 18 - (S % 10) - (S / 10);
                break;
            case "Green 2": // Add the higher digit
                S += Math.Max(S % 10, S / 10);
                break;
            case "Green 3": // Subtract the sum of the digits
                S += (S % 10) - (S / 10);
                break;
            // Jade
            case "Jade 0": // Add twice the digital root
                S += 2 * DR(S);
                break;
            case "Jade 1": // Add twice the sum of the digits
                S += 2 * ((S % 10) + (S / 10));
                break;
            case "Jade 2": // Add twice the first digit
                S += 2 * (S / 10);
                break;
            case "Jade 3": // Subtract twice the first digit
                S -= 2 * (S / 10);
                break;
            // Cyan
            case "Cyan 0": // Swap the digits
                S = 10 * (S % 10) + S / 10;
                break;
            case "Cyan 1": // Subtract the number from ninety-nine
                S = 99 - S;
                break;
            case "Cyan 2": // Add ten times the second digit
                S += 10 * (S % 10);
                break;
            case "Cyan 3": // Subtract the second digit
                S -= S % 10;
                break;
            // Azure
            case "Azure 0": // Subtract the digital root
                S -= DR(S);
                break;
            case "Azure 1": // Replace the second digit with nine minus the first
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + str2[0] + "9876543210".IndexOf(str2[0]);
                S = int.Parse(str2);
                break;
            case "Azure 2": // Replace the second digit with the difference between digits
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + str2[0] + Math.Abs((S % 10) - (S / 10));
                S = int.Parse(str2);
                break;
            case "Azure 3": // Replace the first digit with the second
                str2 = ("" + S).PadLeft(2, '0');
                str2 = "" + str2[1] + str2[1];
                S = int.Parse(str2);
                break;
            // Blue
            case "Blue 0": // Double the number and add one
                S = S * 2 + 1;
                break;
            case "Blue 1": // Subtract the number from fifty
                S = 50 - S;
                break;
            case "Blue 2": // Add nine minus the second digit
                S += 9 - S % 10;
                break;
            case "Blue 3": // Add the first digit
                S += S / 10;
                break;
            // Violet
            case "Violet 0": // Subtract the product of the digits
                S -= (S % 10) * (S / 10);
                break;
            case "Violet 1": // Add the sum of the first digit and the digital root
                S -= (S / 10) + DR(S);
                break;
            case "Violet 2": // Add the sum of the second digit and the digital root
                S -= (S % 10) + DR(S);
                break;
            case "Violet 3": // Subtract the higher digit
                S -= Math.Min(S % 10, S / 10);
                break;
            // Magenta
            case "Magenta 0": // Add eighteen minus the sum of the digits
                S += 18 - (S % 10) - (S / 10);
                break;
            case "Magenta 1": // Subtract nine minus the difference between digits
                S -= 9 - Math.Abs((S % 10) - (S / 10));
                break;
            case "Magenta 2": // Add the lower digit
                S += Math.Min(S % 10, S / 10);
                break;
            case "Magenta 3": // Subtract the difference between digits
                S -= Math.Abs((S % 10) - (S / 10));
                break;
            // Rose
            case "Rose 0": // Subtract twice the digital root
                S -= 2 * DR(S);
                break;
            case "Rose 1": // Subtract twice the sum of the digits
                S -= 2 * ((S % 10) + (S / 10));
                break;
            case "Rose 2": // Add twice the second digit
                S += 2 * (S % 10);
                break;
            case "Rose 3": // Subtract twice the second digit
                S -= 2 * (S % 10);
                break;
            default: // Throw an exception if a modification doesn't exist.
                Debug.LogFormat("[S #{0}] Attempted to use a modification that doesn't exist: " + modification + ". Throwing exception...", _moduleID, S);
                throw new Exception("Attempted to use a modification that doesn't exist: " + modification);
        }
        S = (S + 1000) % 100;
        Debug.LogFormat("[S #{0}] {1} Operation: {2} => {3}", _moduleID, modification.Split(' ')[0], ("" + temp).PadLeft(2, '0'), ("" + S).PadLeft(2, '0'));
    }

    IEnumerator PrepareColors()
    {
        isInteractable = false;
        int i = 0;
        if (inputsReceived)
        {
            foreach (MeshRenderer m in buttonMaterials)
            {
                m.material.color = new Color(0.5f, 0.5f, 0.5f);
                i++;
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield return new WaitForSeconds(0.5f);
        i = 0;
        foreach (MeshRenderer m in buttonMaterials)
        {
            m.material.color = stageColors.ElementAt(stage - 1).ElementAt(i).Value;
            i++;
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.4f);
        if (blinker == null)
        {
            blinker = StartCoroutine(Sequence());
        }
        yield break;
    }

    IEnumerator Sequence()
    {
        inSequence = true;
        isInteractable = true;

        binaryString = "";
        for (int i = 0; i < flashPositions[stage - 1].Count; i++)
        {
            binaryString += Rnd.Range(1, 3);
        }

        while (true)
        {
            for (int i = 0; i < flashPositions[stage - 1].Count; i++)
            {
                Light j = lights[flashPositions[stage - 1][i]];
                if (inputsReceived) Audio.PlaySoundAtTransform(flashes[stage - 1][i] + " " + binaryString[i], transform);
                j.enabled = true;
                yield return new WaitForSeconds(0.32f);
                j.enabled = false;
                yield return new WaitForSeconds(0.07f);
            }
            yield return new WaitForSeconds(0.88f);
        }
    }

    IEnumerator PressButton(KMSelectable button)
    {
        if (!isInteractable) yield break;

        int tempPress = 0;
        bool chk = false;
        inputsReceived = true;
        //Debug.LogFormat("[S #{0}] inSequence is {1}.", _moduleID, inSequence);
        if (inSequence)
        {
            if (blinker != null)
            {
                StopCoroutine(blinker);
                blinker = null;
            }
            inSequence = false;
        }
        for (int i = 0; i < 11; i++)
        {
            if (button == buttons[i])
            {
                if (buttons[i] == button)
                {
                    tempPress = i;
                    break;
                }
            }
        }

        Audio.PlaySoundAtTransform(stageColors[stage - 1].ElementAt(tempPress).Key + " " + Rnd.Range(1, 3), transform);

        Debug.LogFormat("[S #{0}] You pressed {1} in position {2}.", _moduleID, stageColors[stage - 1].ElementAt(tempPress).Key, tempPress);
        submission.Add(stageColors[stage - 1].ElementAt(tempPress).Key);

        for (int i = 0; i < 11; i++)
        {
            lights[i].enabled = false;
        }
        Light l = lights[tempPress];

        // Determine if it is time to check the submission
        if (stage == 1 && submission.Count() == 5) chk = true;
        if (stage == 2 && submission.Count() == 8) chk = true;
        if (stage == 3)
        {
            submission3 += "0123456789,"[tempPress];
            if (submission3.Length >= 2 && submission3.Substring(submission3.Length - 2) == ",,") chk = true;
        }

        if (chk)
        {
            isInteractable = false;
            bool willStrike = false;
            string str = "";
            if (stage != 3)
            {
                Debug.LogFormat("[S #{0}] You submitted the colors: {1}.", _moduleID, submission.Join(", "));

                for (int i = 0; i < submission.Count(); i++)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        if (submission[i] == stageColors[stage - 1].ElementAt(j).Key)
                        {
                            str += j + (i != submission.Count() - 1 ? ", " : "");
                            break;
                        }
                    }
                }

                Debug.LogFormat("[S #{0}] You submitted the positions: {1}.", _moduleID, str);

                for (int i = 0; i < submission.Count(); i++)
                {
                    if (submission[i] != answers[stage - 1][i])
                    {
                        willStrike = true;
                        break;
                    }
                }
            }
            else
            {
                Debug.LogFormat("[S #{0}] You submitted the sequence: {1}.", _moduleID, submission3);
                if (submission3 != S3) willStrike = true;
                submission3 = "";
            }
            if (willStrike)
            {
                Debug.LogFormat("[S #{0}] That submission was incorrect. Stepping back to Stage 1...", _moduleID, str);
                Module.HandleStrike();
                stage = 1;
            }
            else
            {
                Debug.LogFormat("[S #{0}] That submission was correct! {1}", _moduleID, stage == 3 ? "Module solved!" : "Advancing to Stage " + (stage + 1) + "...");
                if (stage == 3)
                {
                    isInteractable = false;
                    moduleSolved = true;
                    float f = 0.39f;
                    for (int i = 10; i >= 0; i--)
                    {
                        Light j = lights[i];
                        if (i != 10) Audio.PlaySoundAtTransform(stageColors[2].ElementAt(i).Key + " 2", transform);
                        j.enabled = true;
                        yield return new WaitForSeconds(f);
                        f /= 1.1f;
                    }
                    yield return new WaitForSeconds(1);
                    Audio.PlaySoundAtTransform(missingColorList[2] + " 1", transform);
                    Module.HandlePass();
                    yield break;
                }
                else
                {
                    stage++;
                    if (maxStage == 1 && stage == 2)
                    {
                        GenerateStage2();
                    }
                    else if (maxStage == 2 && stage == 3)
                    {
                        GenerateStage3();
                    }
                    if (maxStage < stage) maxStage = stage;
                }
            }
        }

        l.enabled = true;
        yield return new WaitForSeconds(0.24f);
        l.enabled = false;
        yield return new WaitForSeconds(0.07f);
        if (chk)
        {
            if (!moduleSolved)
            {
                submission.Clear();
                if (preparer != null)
                {
                    StopCoroutine(preparer);
                }

                preparer = StartCoroutine(PrepareColors());
            }
        }
    }

    private void HLButton(KMSelectable button)
    {
        if (!isInteractable)
        {
            modText = "";
            return;
        }
        int tempPress = 0;
        for (int i = 0; i < 11; i++)
        {
            if (button == buttons[i])
            {
                if (buttons[i] == button)
                {
                    tempPress = i;
                    break;
                }
            }
        }
        modText = stageColors[stage - 1].ElementAt(tempPress).Key;
    }

    private void HLEndButton(KMSelectable button)
    {
        modText = "";
    }

    private Dictionary<string, Color> newStageColors(Dictionary<string, Color> c)
    {
        Dictionary<string, Color> d = new Dictionary<string, Color>(c);
        Dictionary<string, Color> e = new Dictionary<string, Color>();

        for (int i = 0; i < 11; i++)
        {
            int x = Rnd.Range(0, d.Count);
            e.Add(d.ElementAt(x).Key, d.ElementAt(x).Value);
            d.Remove(d.ElementAt(x).Key);
            //printDictionary(d);
            //Debug.LogFormat("[S #{0}] New color added in position {1}: " + e.ElementAt(i).Key, _moduleID, i);
        }
        missingColor = d.ElementAt(0).Key;
        return e;
    }

    /*
    private void printDictionary(Dictionary<string, Color> d)
    {
        for (int i = 0; i < d.Count; i++)
        {
            Debug.LogFormat("[S #{0}] {1}", _moduleID, d.ElementAt(i).Key);
        }
    }
	*/

    int DR(int x) // Digital root
    {
        return (x - 1) % 9 + 1;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < 3; i++)
        {
            if (moduleSolved)
            {
                stageMaterials[i].material = stageOn;
            }
            else if (i > stage - 1)
            {
                stageMaterials[i].material = stageOff;
            }
            else if (i == stage - 1)
            {
                stageMaterials[i].material = Time.time % 0.814 < 0.407 && isInteractable ? stageOn : stageOff;
            }
            else
            {
                stageMaterials[i].material = stageOn;
            }
        }
        if (isInteractable && colorblindMode) colorblindText.text = modText;
        else colorblindText.text = "";
    }

    // Twitch Plays

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <0123456789T> [Press the buttons in these positions, where T is the 10th position.] // !{0} press <ROYLGJCABVMS> [Press the corresponding colors, where each color is pressed based on their first letter (except for Rose, which is S).
You can perform this command in a chain; for example: !{0} 94OYRTJ0] // !{0} colors [Give the names of the colors in the order of the module.]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        char ok = ' ';
        command = command.ToUpper();
        string[] parameters = command.Split(' ');
        yield return null;
        if (parameters.Length != 1 && parameters.Length != 2)
        {
            yield return "sendtochaterror That command doesn't exist!";
            yield break;
        }
        else if (command == "COLORS")
        {
            yield return "sendtochat The colors are: " + stageColors[stage - 1].Select(x => x.Key).ToArray().Join(", ") + ".";
            yield break;
        }
        else if (parameters[0] == "PRESS")
        {
            string validString = "ROYLGJCABVMS";
            if (missingColorList[stage - 1] == "Rose") validString = validString.Replace("S", "");
            else validString = validString.Replace(missingColorList[stage - 1][0] + "", "");
            for (int i = 0; i < parameters[1].Length; i++)
            {
                if (!("0123456789T" + validString).Contains(parameters[1][i]))
                {
                    ok = parameters[1][i];
                    break;
                }
            }
            if (ok != ' ')
            {
                yield return "sendtochaterror An invalid character was entered: " + ok;
                yield break;
            }
            else
            {
                float f = 0.34f;
                for (int i = 0; i < parameters[1].Length; i++)
                {
                    if ("0123456789T".Contains(parameters[1][i]))
                    {
                        yield return null;
                        buttons["0123456789T".IndexOf(parameters[1][i])].OnInteract();
                    }
                    else
                    {
                        yield return null;
                        buttons[stageColors[stage - 1].Select(x => x.Key == "Rose" ? 'S' : ("" + x.Key)[0]).ToArray().Join("").IndexOf(parameters[1][i])].OnInteract();
                    }
                    yield return "trycancel";
                    yield return new WaitForSeconds(f);
                    f /= 1.1f;
                }
            }
        }
        else
        {
            yield return "sendtochaterror That command doesn't exist!";
            yield break;
        }
    }
}