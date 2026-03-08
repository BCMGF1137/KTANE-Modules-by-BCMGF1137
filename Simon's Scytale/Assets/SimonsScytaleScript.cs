using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using Rnd = UnityEngine.Random;

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

    private static readonly string _ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly string[] _MODULES = "D,Simon's Sums,Cruel Boolean Wires,Modulo Maze,S,Terminology".Split(',');

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
    private int sequenceLength;

    private string colorSequence;
    private string answer;

    // ==Physical Methods==

    void Awake()
    {
        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate ()
            {
                //StartCoroutine(PressButton(button));
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

        StartCoroutine(RotateScytale());

        // Step 1: The Binary Sequence
        bool[] binarySequence = Bomb.GetSerialNumber().Select(x => _ALPHABET.IndexOf(x) >= 5 && _ALPHABET.IndexOf(x) <= 20).ToArray();
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

        int[] keys = new int[3];

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
            validColors = validColors.Select(x => x - 1).ToArray(); // Convert the positions of the valid colors to zero-based indices.
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

        StartCoroutine(FlashSequence());
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

    // When a button is highlighted, change the display to what is being highlighted.
    private void HLButton(KMSelectable button)
    {
        int buttonIndex = Array.IndexOf(buttons, button);
        displays[1].text = modColors[buttonIndex].Substring(0, 2).ToUpper();
    }

    private void HLEndButton(KMSelectable button)
    {
        displays[1].text = "" + sequenceLength;
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


    private IEnumerator RotateScytale()
    {
        var duration = Rnd.Range(5f, 7f);
        while (true)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                cylinder.transform.localEulerAngles = new Vector3(-180f, 0f, Mathf.Lerp(0, 360, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }
}
