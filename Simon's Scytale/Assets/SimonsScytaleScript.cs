using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class SimonsScytaleScript : MonoBehaviour {

    static int _moduleIDCounter = 1;
    int _moduleID;

    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    // Physical stuff (in unity)
    public KMSelectable[] buttons;
    public Renderer[] buttonMaterials;
    public TextMesh[] displays;
    public Transform cylinder;

    public Material[] lightTypes;

    // Readonly variables
    private static readonly string _ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string[] _MODULES = "D,Simon's Sums,Cruel Boolean Wires,Modulo Maze,S,Terminology".Split(',');

    // Keyboard support
    private KeyCode[] TypableKeys =
    {
        KeyCode.R, KeyCode.Y, KeyCode.G, KeyCode.C, KeyCode.B, KeyCode.M
    };

    // IEnumerators
    private IEnumerator sequence;

    // Initialize colors
    private static readonly string[] colorNames = {"Red", "Yellow", "Green", "Cyan", "Blue", "Magenta"};
    private static readonly string[] alteredColorNames = { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };
    private Dictionary<string, Color> colors = colorNames.Select((n, i) => new
    {
        Name = n,
        Color = Color.HSVToRGB(i / 6f, 1, 1)
    }).ToDictionary(x => x.Name, x => x.Color);
    private string[] modColors = colorNames.Select(x => x).ToArray();

    // Do we invert the rotation?
    private bool invertRotation = false;
    private float rotation = 0f;
    private float rotateSpeed = 0f;

    private int[] validColors = new int[3];
    private int[] keys;
    private int sequenceLength;

    private string colorSequence;
    private string answer;

    private bool isInteractable = true; // Can we interact with the buttons?
    private int inputCount = 0; // How many inputs have we submitted into the module?

    private bool moduleSolved = false;
    private float elapsed = 0f;

    private bool moduleFocused = false; // Are we focused on the module? Necessary for keyboard support.
    private bool twitchPlaysStruck = false;

#pragma warning disable 414
    private string TwitchHelpMessage = @"!{0} XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX [Submit a sequence of 30 colors. They MUST be letters from RGBCMY.]
!{0} colors [List the colors on the module from top-left to bottom-right. Useful for colorblindness.]
NOTE: This command dynamically changes depending on the number of flashes.";
#pragma warning restore 414

    // ==Physical Methods==

    void Awake()
    {
        _moduleID = _moduleIDCounter++;

        GetComponent<KMSelectable>().OnFocus += delegate {
            moduleFocused = true;
        };
        GetComponent<KMSelectable>().OnDefocus += delegate {
            moduleFocused = false;
        };

        sequence = FlashSequence();

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
    }

    // Use this for initialization
    void Start () {
        sequenceLength = Rnd.Range(20,31);
        
        rotateSpeed = Rnd.Range(1.5f, 2.2f);

        // Step 1: The Binary Sequence
        bool[] binarySequence = Bomb.GetSerialNumber().Select(x => _ALPHABET.IndexOf(x) >= 5 && _ALPHABET.IndexOf(x) <= 21).ToArray();
        Debug.LogFormat("[Simon's Scytale #{0}] The initial binary sequence is {1}.", _moduleID, binarySequence.Select(x => x ? 1 : 0).Join(""));

        for (int i = 0; i < 6; i++)
        {
            string module = _MODULES[i];
            if (Bomb.GetModuleNames().Contains(module))
            {
                Debug.LogFormat("[Simon's Scytale #{0}] \"{1}\" IS present. Bit {2} is flipped.", _moduleID, module, i + 1);
                binarySequence[i] = !binarySequence[i];
            }
            else
            {
                Debug.LogFormat("[Simon's Scytale #{0}] \"{1}\" is NOT present.", _moduleID, module);
            }
        }

        //binarySequence = new bool[] { false, true, true, true, true, true };

        Debug.LogFormat("[Simon's Scytale #{0}] The final binary sequence is {1}.", _moduleID, binarySequence.Select(x => x ? 1 : 0).Join(""));

        // Step 2: Valid Colors
        if (binarySequence.Count(x => x) == 6) // Case A: The binary sequence is 111111.
        {
            Debug.LogFormat("[Simon's Scytale #{0}] Row 1 applies.", _moduleID);
            if (Bomb.GetBatteryCount() % 2 == 1) // Case A1: Odd batteries
            {
                // The first 3 colors are valid.
                validColors = new int[] { 1, 2, 3 };
                Debug.LogFormat("[Simon's Scytale #{0}] There are an odd number of batteries.", _moduleID);
            }
            else // Case A2: Even batteries
            {
                // The last 3 colors are valid.
                validColors = new int[] { 4, 5, 6 };
                Debug.LogFormat("[Simon's Scytale #{0}] There are an even number of batteries.", _moduleID);
            }
        }
        else if (binarySequence.Count(x => x) == 0) // Case B: The binary sequence is 000000.
        {
            Debug.LogFormat("[Simon's Scytale #{0}] Row 2 applies.", _moduleID);

            // Same parity as the number of ports
            if (Bomb.GetPortCount() % 2 == 1) // Case B1: Odd ports
            {
                validColors = new int[] { 1, 3, 5 };
                Debug.LogFormat("[Simon's Scytale #{0}] There are an odd number of ports.", _moduleID);
            }
            else // Case B2: Even ports
            {
                validColors = new int[] { 2, 4, 6 };
                Debug.LogFormat("[Simon's Scytale #{0}] There are an even number of ports.", _moduleID);
            }
        }
        else if (binarySequence.Count(x => x) == 3) // Case C/D: 3 1s and 3 0s
        {
            if (Bomb.GetOnIndicators().Count() != Bomb.GetOffIndicators().Count()) // Case C: There are NOT equal lit/unlit indicators.
            {
                Debug.LogFormat("[Simon's Scytale #{0}] Row 3 applies.", _moduleID);

                if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count()) // Case C1: More lit indicators
                {
                    // Same positions as 1s
                    validColors = binarySequence.Select((x, i) => x ? (i + 1) : 0).Where(x => x != 0).ToArray();
                    Debug.LogFormat("[Simon's Scytale #{0}] There are more lit indicators than unlit indicators.", _moduleID);
                }
                else // Case C2: More unlit indicators
                {
                    validColors = binarySequence.Select((x, i) => !x ? (i + 1) : 0).Where(x => x != 0).ToArray();
                    Debug.LogFormat("[Simon's Scytale #{0}] There are more unlit indicators than lit indicators.", _moduleID);
                }
            }
            else // Case D: There ARE equal lit/unlit indicators.
            {
                Debug.LogFormat("[Simon's Scytale #{0}] Row 4 applies.", _moduleID);

                if (Bomb.GetIndicators().Count() == 0) // Case D1: No indicators
                {
                    // 1st, 6th, and second 0
                    validColors = new int[] { 1, binarySequence.Select((x, i) => !x ? (i + 1) : 0).Where(x => x != 0).ToArray()[1], 6 };
                    Debug.LogFormat("[Simon's Scytale #{0}] There are no indicators.", _moduleID);
                }
                else // Case D2: Indicators present
                {
                    // 1st, 6th, and second 1
                    validColors = new int[] { 1, binarySequence.Select((x, i) => x ? (i + 1) : 0).Where(x => x != 0).ToArray()[1], 6 };
                    Debug.LogFormat("[Simon's Scytale #{0}] Indicators are present.", _moduleID);
                }
            }
        }
        else if (binarySequence.Count(x => x) == 1 || (binarySequence.Count(x => x) == 5)) // Case E: There is a single 1 or 0.
        {
            Debug.LogFormat("[Simon's Scytale #{0}] Row 5 applies.", _moduleID);
            if (binarySequence.Count(x => x) == 1) // Case E1: The lone bit is a 1
            {
                // Colors BEFORE the 1
                int x = Array.IndexOf(binarySequence, true) + 6;
                validColors = new int[] { (x - 1) % 6 + 1, (x - 2) % 6 + 1, (x - 3) % 6 + 1};
                validColors = validColors.OrderBy(xx => xx).ToArray();
                Debug.LogFormat("[Simon's Scytale #{0}] The lone bit is a 1.", _moduleID);
            }
            else // Case E1: The lone bit is a 0
            {
                // Colors AFTER the 0
                int x = Array.IndexOf(binarySequence, true) + 6;
                validColors = new int[] { x % 6 + 1, (x + 1) % 6 + 1, (x + 2) % 6 + 1};
                validColors = validColors.OrderBy(xx => xx).ToArray();
                Debug.LogFormat("[Simon's Scytale #{0}] The lone bit is a 0.", _moduleID);
            }
        }
        else // Case F/G: 2 1s or 2 0s
        {
            if (binarySequence[0]) // Case F: The first bit is a 1.
            {
                Debug.LogFormat("[Simon's Scytale #{0}] Row 6 applies.", _moduleID);

                string str = binarySequence.Select(x => x ? 1 : 0).Join("");
                List<string> grayCodes = new List<string> {str};
                // Apply gray codes repeatedly

                while (str.Count(x => x == '1') != 3)
                {
                    string j = "1";
                    for (int i = 1; i < 6; i++)
                    {
                        j += j[i - 1] == str[i] ? "0" : "1";
                    }
                    str = j;
                    grayCodes.Add(str);
                }
                validColors = str.Select((x, i) => x == '1' ? i + 1 : 0).Where(x => x != 0).ToArray();

                Debug.LogFormat("[Simon's Scytale #{0}] The first bit is a 1. Gray codes: {1}.", _moduleID, grayCodes.Join(" => "));
            }
            else // Case G: The first bit is a 0.
            {
                Debug.LogFormat("[Simon's Scytale #{0}] Row 7 applies.", _moduleID);

                // For my sanity, we're gonna make 1 the invalid colors.
                string str;
                if (binarySequence.Count(x => x) == 4)
                {
                    str = binarySequence.Select(x => x ? 0 : 1).Join("");
                }
                else
                {
                    str = binarySequence.Select(x => x ? 1 : 0).Join("");
                }

                int[] invalidColors = new int[]{0, 0, 0};
                invalidColors[0] = str.IndexOf("1") + 1;
                invalidColors[1] = str.Substring(invalidColors[0]).IndexOf("1") + invalidColors[0] + 1;
                int n = Bomb.GetPortPlateCount() % 4;

                //Debug.LogFormat("[Simon's Scytale #{0}] {1}.", _moduleID, str.Select((x, i) => x != '1' ? (i + 1) : 0).Where(x => x != 0).Join(""));
                invalidColors[2] = str.Select((x, i) => x != '1' ? (i + 1) : 0).Where(x => x != 0).ToArray()[n];
                invalidColors = invalidColors.OrderBy(x => x).ToArray();
                validColors = (new int[] { 1, 2, 3, 4, 5, 6 }).Where(x => !invalidColors.Contains(x)).ToArray();
                Debug.LogFormat("[Simon's Scytale #{0}] The first bit is a 1. Invalid colors are in positions {1}, {2}, and {3}.", _moduleID, invalidColors[0], invalidColors[1], invalidColors[2]);
            }
        }

        Debug.LogFormat("[Simon's Scytale #{0}] The valid colors' positions are {1}, {2}, and {3}.", _moduleID, validColors[0], validColors[1], validColors[2]);

        // Step 3: Conversion To Transpositions

        /*
        Rows correspond to the position of the color on the module.
        Columns correspond to the colors themselves, in RGBCMY.
         */
        int[,] colorTable = new int[,]
        {
            {-6,-4,+3,+2,-7,+5 },
            {+2,-3,-7,+5,-6,+4 },
            {+5,+7,-4,-6,+2,-3 },
            {-7,+6,+5,-4,-3,+2 },
            {+3,-5,-2,+7,+4,-6 },
            {-4,+2,+6,-3,+5,-7 },
        };

        bool generated = false;
        string initialSequence = _ALPHABET.Substring(0, sequenceLength);

        keys = new int[3];
        int[] origValidColors = validColors.Select(x => x).ToArray(); // Create new reference for validColors to prevent the below algorithm from exceptioning on attempt 2

        while (!generated)
        {
            modColors = modColors.Shuffle(); // Randomize colors
            invertRotation = Rnd.Range(0f, 2f) < 1f; // Randomize direction

            for (int i = 0; i < 6; i++)
            {
                buttonMaterials[i].material.color = colors[modColors[i]];
            }
            //Debug.LogFormat("[Simon's Scytale #{0}] The module's colors are {1}.", _moduleID, modColors.Join(", "));
            //Debug.LogFormat("[Simon's Scytale #{0}] The module is rotating {1}WARDS.", _moduleID, invertRotation ? "UP" : "DOWN");

            string predictSequence = initialSequence;
            validColors = origValidColors.Select(x => x - 1).ToArray(); // Convert the positions of the valid colors to zero-based indices.
            if (invertRotation) validColors = validColors.Reverse().ToArray(); // Go from bottom to top if the rotation is inverted.

            /*
             So I need to get...
             - RYGCBM
             - "What is the color at that position?"    modColors[validColors[0]]
             - Convert the color to a position.         colorNames.IndexOf(modColors[validColors[0]])
             */
             
            keys = validColors.Select(x => colorTable[x, Array.IndexOf(alteredColorNames, modColors[x])]).ToArray();
            
            for (int i = 0; i < 3; i++)
            {
                predictSequence = Scytale(predictSequence, keys[i]);
            }

            //Debug.LogFormat("[Simon's Scytale #{0}] The sequence is {1}.", _moduleID, predictSequence);

            // Prevent the predicted sequence from doing nothing
            if (predictSequence != initialSequence) generated = true;
            else Debug.LogFormat("[Simon's Scytale #{0}] Generation failed! Generating again...", _moduleID, modColors.Join(", "));
        }

        Debug.LogFormat("[Simon's Scytale #{0}] The module's colors are {1}.", _moduleID, modColors.Join(", "));
        Debug.LogFormat("[Simon's Scytale #{0}] The module is rotating {1}WARDS.", _moduleID, invertRotation ? "UP" : "DOWN");
        Debug.LogFormat("[Simon's Scytale #{0}] The keys are {1} (in that order).", _moduleID, keys.Select(x => x < 0 ? "" + x : "+" + x).Join(", "));

        colorSequence = "RGBCMY"; // The flashing sequence. GUARANTEES one of each color.

        while (colorSequence.Length < sequenceLength) // Append a color to the sequence until of required length.
        {
            colorSequence += "RGBCMY".PickRandom();
        }

        colorSequence = colorSequence.ToCharArray().Shuffle().Join("");

        Debug.LogFormat("[Simon's Scytale #{0}] The flashing sequence is {1}.", _moduleID, colorSequence);

        answer = colorSequence;

        for (int i = 0; i < 3; i++)
        {
            Debug.LogFormat("[Simon's Scytale #{0}] Sequence after {1}: {2} -> {3}.", _moduleID, keys[i] > 0 ? "+" + keys[i] : "" + keys[i],answer, Scytale(answer, keys[i]));
            answer = Scytale(answer, keys[i]);
        }

        Debug.LogFormat("[Simon's Scytale #{0}] The submission sequence is {1}.", _moduleID, answer);

        displays[1].text = "" + sequenceLength;

        // Dynamic TP Help Message
        string xrayCharacters = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX".Substring(0, sequenceLength);
        TwitchHelpMessage = "!{0} " + xrayCharacters + " [Submit a sequence of " + sequenceLength + " colors. They MUST be letters from RGBCMY.]\n!{0} colors [List the colors on the module from top-left to bottom-right. Useful for colorblindness.]";

        StartCoroutine(sequence);
    }

    private void GetNewAnswer() // Modify the answer after a strike.
    {
        colorSequence = answer;
        Debug.LogFormat("[Simon's Scytale #{0}] The new flashing sequence is {1}.", _moduleID, colorSequence);

        for (int i = 0; i < 3; i++)
        {
            Debug.LogFormat("[Simon's Scytale #{0}] Sequence after {1}: {2} -> {3}.", _moduleID, keys[i] > 0 ? "+" + keys[i] : "" + keys[i], answer, Scytale(answer, keys[i]));
            answer = Scytale(answer, keys[i]);
        }

        Debug.LogFormat("[Simon's Scytale #{0}] The new submission sequence is {1}.", _moduleID, answer);

        StartCoroutine(sequence);
    }

    // Flash the sequence.
    private IEnumerator FlashSequence()
    {
        int flashPos = 0;
        Color32 colorStored;
        while (true)
        {
            int x = 0;
            if (flashPos == sequenceLength)
            {
                flashPos = -1;
                displays[0].text = "--";
            }
            else
            {
                x = Array.IndexOf(modColors, colorNames["RYGCBM".IndexOf(colorSequence[flashPos])]);
                colorStored = buttonMaterials[x].GetComponent<MeshRenderer>().material.color;
                buttonMaterials[x].material = lightTypes[1];
                buttonMaterials[x].GetComponent<MeshRenderer>().material.color = colorStored;
                displays[0].text = ("" + (flashPos + 1)).PadLeft(2, '0');
            }
            yield return new WaitForSeconds(120f / 140f);
            flashPos++;

            for (int i = 0; i < 6; i++)
            {
                colorStored = buttonMaterials[i].GetComponent<MeshRenderer>().material.color;
                buttonMaterials[i].material = lightTypes[0];
                buttonMaterials[i].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            if (flashPos == sequenceLength)
            {
                displays[0].text = "--";
            }

            yield return new WaitForSeconds(20f / 140f);
        }
    }

    // Pressing a button
    private IEnumerator PressButton(KMSelectable button)
    {
        StopCoroutine(sequence);

        if (isInteractable)
        {
            bool struck = false;

            inputCount++;
            displays[0].text = ("" + inputCount).PadLeft(2, '0');

            isInteractable = false;

            button.AddInteractionPunch(0.2f);

            Color32 colorStored;
            colorStored = button.GetComponent<MeshRenderer>().material.color;
            button.GetComponent<MeshRenderer>().material = lightTypes[1];
            button.GetComponent<MeshRenderer>().material.color = colorStored;

            // Check for incorrect input
            string reqColor = colorNames["RYGCBM".IndexOf(answer[inputCount - 1])];
            //Debug.LogFormat("[Simon's Scytale #{0}] The module requested {1}.", _moduleID, reqColor);
            string inputColor = modColors[Array.IndexOf(buttons, button)];
            //Debug.LogFormat("[Simon's Scytale #{0}] You pressed " + inputColor + ".", _moduleID);

            Audio.PlaySoundAtTransform("Sound_" + inputColor, transform);

            if (reqColor != inputColor)
            {
                struck = true;
                twitchPlaysStruck = true;
                Debug.LogFormat("[Simon's Scytale #{0}] For input {1}, the module requested {2}, but you pressed {3}. Strike!", _moduleID, inputCount, reqColor, inputColor);
                Module.HandleStrike();
            }
            
            yield return new WaitForSeconds(50f / 140f);

            for (int i = 0; i < 6; i++)
            {
                colorStored = buttonMaterials[i].GetComponent<MeshRenderer>().material.color;
                buttonMaterials[i].material = lightTypes[0];
                buttonMaterials[i].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            if (struck) // Change the answer on a strike.
            {
                yield return new WaitForSeconds(50f / 140f);
                inputCount = 0;
                GetNewAnswer();
                isInteractable = true;
            }
            //else if (inputCount == 1) // Code for when the module is solved.
            else if (inputCount == sequenceLength) // The submission has been exhausted. Module solved!
            {
                isInteractable = false;
                moduleSolved = true;
                Debug.LogFormat("[Simon's Scytale #{0}] You have correctly inputted all {1} colors. Solved!", _moduleID, sequenceLength);
                yield return new WaitForSeconds(.3f);
                StartCoroutine(SolveAnimation());
            }
            else
            {
                isInteractable = true;
            }
        }
    }

    // When a button is highlighted, change the display to what is being highlighted.
    private void HLButton(KMSelectable button)
    {
        if (!moduleSolved)
        {
            int buttonIndex = Array.IndexOf(buttons, button);
            displays[1].text = modColors[buttonIndex].Substring(0, 2).ToUpper();
        }
    }

    private void HLEndButton(KMSelectable button)
    {
        if (!moduleSolved) displays[1].text = "" + sequenceLength;
    }

    // Solve animation
    IEnumerator TimeFix()
    {
        while (true)
        {
            yield return null;
            elapsed += Time.deltaTime;
            //Debug.LogFormat("[Simon's Scytale #{0}] elapsed={1}.", _moduleID, elapsed);
        }
    }

    IEnumerator SolveAnimation()
    {
        StartCoroutine(TimeFix());
        Audio.PlaySoundAtTransform("Solve", transform);

        Color32 colorStored;
        displays[0].text = "" + sequenceLength;
        displays[1].text = "" + sequenceLength;
        

        for (int i = 0; i < 24; i++)
        {
            colorStored = buttons[i % 6].GetComponent<MeshRenderer>().material.color;
            buttons[i % 6].GetComponent<MeshRenderer>().material = lightTypes[1];
            buttons[i % 6].GetComponent<MeshRenderer>().material.color = colorStored;
            switch (i)
            {
                case 12:
                    displays[0].text = "--";
                    displays[1].text = "SI";
                    break;
                case 15:
                    displays[0].text = "MO";
                    displays[1].text = "N'";
                    break;
                case 18:
                    displays[0].text = "SS";
                    displays[1].text = "CY";
                    break;
                case 21:
                    displays[0].text = "TA";
                    displays[1].text = "LE";
                    break;
                default:
                    break;
            }
            yield return new WaitUntil(() => elapsed >= (i + 1) * 20 / 140f);
            for (int j = 0; j < 6; j++)
            {
                colorStored = buttonMaterials[j].GetComponent<MeshRenderer>().material.color;
                buttonMaterials[j].material = lightTypes[0];
                buttonMaterials[j].GetComponent<MeshRenderer>().material.color = colorStored;
            }
            //Debug.LogFormat("[Simon's Scytale #{0}] i={1}.", _moduleID, i);
        }

        // Solve the module
        Module.HandlePass();
        for (int j = 0; j < 6; j++)
        {
            colorStored = buttonMaterials[j].GetComponent<MeshRenderer>().material.color;
            buttonMaterials[j].material = lightTypes[1];
            buttonMaterials[j].GetComponent<MeshRenderer>().material.color = colorStored;
        }

        displays[0].text = "GG";
        displays[1].text = "M8";

        // Victory animation
        yield return new WaitUntil(() => elapsed >= 26 * 20 / 140f);

        string solvePositions = "123455432100"; // Infinitely loop through this string
        int iteration = 0;

        while (true)
        {
            for (int j = 0; j < 6; j++)
            {
                colorStored = buttonMaterials[j].GetComponent<MeshRenderer>().material.color;
                buttonMaterials[j].material = lightTypes[0];
                buttonMaterials[j].GetComponent<MeshRenderer>().material.color = colorStored;
            }

            int pos = _ALPHABET.IndexOf(solvePositions[iteration % 12]);
            colorStored = buttonMaterials[pos].GetComponent<MeshRenderer>().material.color;
            buttonMaterials[pos].material = lightTypes[1];
            buttonMaterials[pos].GetComponent<MeshRenderer>().material.color = colorStored;

            yield return new WaitUntil(() => elapsed >= (iteration + 27) * 20 / 140f);
            iteration++;
        }
    }

    // ==Internal Methods==

    private string Scytale(string str, int key)
    {
        if (key > 0) return EncryptScytale(str, key);
        else return DecryptScytale(str, -key);
    }

    // Encrypt scytale transposition
    private string EncryptScytale(string str, int key)
    {
        string[] scytale = new string[key];
        //Debug.LogFormat("[Simon's Scytale #{0}] Length {1}.", _moduleID, str.Length);

        for (int i = 0; i < str.Length; i++)
        {
            scytale[i % key] += "-";
        }
        int charsUsed = 0;
        for(int row = 0; row < key; row++)
        {
            //Debug.LogFormat("[Simon's Scytale #{0}] Position {1}.", _moduleID, scytale[row].Length);
            scytale[row] = str.Substring(charsUsed, scytale[row].Length);
            //Debug.LogFormat("[Simon's Scytale #{0}] {1}.", _moduleID, scytale[row]);
            charsUsed += scytale[row].Length;
        }
        string result = "";
        for (int i = 0; i < str.Length; i++)
        {
            result += scytale[i % key][i / key];
        }
        return result;
    }

    private string DecryptScytale(string str, int key)
    {
        string[] scytale = new string[key];
        for (int i = 0; i < str.Length; i++)
        {
            scytale[i % key] += str[i];
        }
        return scytale.Join("");
    }

    // Keyboard support
    private void EvaluateKeyPresses()
    {
        if (!moduleFocused || moduleSolved) return; // DO NOT evaluate key presses if solved or not focused.

        for (int i = 0; i < TypableKeys.Length; i++)
        {
            if (Input.GetKeyDown(TypableKeys[i]))
            {
                string color = colorNames[i]; // Get the name of the color
                int index = Array.IndexOf(modColors, color);
                buttons[index].OnInteract();
                return;
            }
        }
    }

    // Update is called once per frame
    void Update () {

        EvaluateKeyPresses();

        // Rotate the scytale
        // Original: -180,0,0
        rotation = (rotation + (invertRotation ? -1f : 1f) * rotateSpeed);
        cylinder.transform.localEulerAngles = new Vector3(-180, 0, rotation);

        if (moduleSolved)
        {
            rotateSpeed /= 1.002f;
        }
	}

    // Twotch Plays

    public IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToUpperInvariant();

        if (command == "COLORS")
        {
            yield return "sendtochat The colors are: " + modColors.Join(", ") + ".";
        }
        else
        {
            Match match = Regex.Match(command, "^[RGBCMY]{" + sequenceLength + "}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                twitchPlaysStruck = false;

                foreach (char c in command)
                {
                    /*
                    I need to take the color sequence, convert letters to strings, find their positions on the buttons, and then press accordingly.
                    1. Take the color sequence and convert letters to strings.
                    2. Find their positions on the buttons.
                    3. Press accordingly.
                     */
                    string toPress = colorNames["RYGCBM".IndexOf(c)];
                    KMSelectable button = buttons[Array.IndexOf(modColors, toPress)];
                    button.OnInteract();
                    yield return new WaitUntil(() => !isInteractable);
                    yield return new WaitUntil(() => isInteractable);
                    if (twitchPlaysStruck) yield break;
                }
                yield return "solve";
            }
            else yield return "sendtochaterror The given submission sequence is invalid!";
        }
    }
}
