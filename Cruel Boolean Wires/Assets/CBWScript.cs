using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class CBWScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public TextMesh[] displayTexts; // What the displays show
    public KMSelectable[] wires; // Wire selectables
    public KMSelectable[] displays; // Display selectables
    public Transform wireMech; // Wire mechanism position
    public Transform coverPlane; // Cover position
    public MeshRenderer[] wireTextures; // "What color are the wires?"
    public Material[] materials; // Possible wire colors (RGB)
    public GameObject[] uncutWires; // Uncut wires (hide when cut)
    public GameObject[] cutWires; // Cut wires (hide when uncut)

    private string day = DateTime.Now.DayOfWeek.ToString();
    private int day2 = DateTime.Now.Day;

    private tbool[] booleanValues = Enumerable.Range(0, 36).Select(_ => new tbool("F")).ToArray();

    private string leftBoolean = ""; // Left screen
    private string rightBoolean = ""; // Middle screen
    private int startPos = 0; // Right screen
    private tbool stageColor = new tbool("F"); // Stage color (RGB = FTU)

    private string leftLetter = "?"; // Left stored letter
    private string rightLetter = "?"; // Middle stored letter
    private string solution = ""; // Solution

    private int startTime; // Bomb's starting time

    private tbool leftBool = new tbool("F"); // Left boolean
    private tbool rightBool = new tbool("F"); // Right boolean

    private int stage = 0; // Max stages is 5
    private int attempt = 0; // No limit to # of attempts

    private string prevStage = "??"; // Previous stage; carries over strikes
    private string summary = ""; // Every truth value for that attempt

    // Internal placeholder variables
    private int temp = 0;
    private int temp2 = 0;
    private long temp64 = 0;
    private bool chk = false;
    private string str = "";

    private bool mechChk = false;

    private bool isActivated = false;
    private bool isAnimating = false; // Used to stop stuff
    private bool moduleSolved = false;
    private bool[] wireStates = Enumerable.Range(0, 20).Select(_ => false).ToArray();

    // Things for booleans
    private int[] lucasNumbers = {2, 1, 3, 4, 7, 11, 18, 29, 47, 76, 123, 199, 322, 521};
    private int[] fibonacciNumbers = {0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377};
    private string[] extremes = {"bamboozledAgain","beanboozledAgain","BellOfTioModule","cruelSynesthesia","decay",
"identifrac","mislocation","TheOctadecayotton","omegaForget","rngCrystal","spectreMaze"
,"simonSwindles","UltraStores","vigilantPsycho"};
    private string[] vanillas = {"BigButton","NeedyCapacitor","Venn","Keypad","NeedyKnob",
"Maze","Memory","Morse","Password","Simon","NeedyVentGas"
,"WhosOnFirst","Wires","WireSequence"};

    private string[] idcs = { "lgndMorseIdentification", "boozleglyphIdentification", "PlantIdentification", "PickupIdentification", "EmotiguyIdentification",
        "arsGoetiaIdentification", "miiIdentification", "xelCustomerIdentification", "spongebobBirthdayIdentification",
    "DrDoctorModule", "Phosphorescence", "phones"};
    private string[] ucrs = { "WordSearchModule", "ultimateCipher", "Alphabetize", "greenArrowsModule", "yellowArrowsModule", "sphere"
    , "unfairsRevenge", "WhosOnFirst", "WhatsOnSecond", "blueArrowsModule", "orangeCipher", "unfairCipher", "romanArtModule"};

    /* Operators (I am suffocating right now)
    The setup looks like this:
    
       A
      fut
     f036
    Bu147
     t258

    If you want to turn the Venn diagrams into truth tables, be my guest! :)
    */

    private string operators = "⋀⋁⊻→↔↓|←&⊥△✕＋⊕⇥⇤⇨⇦스≫≪±∓ㄒ∅◇＝≠";

    private string[] truthValues = {
        "ffffuufut", "futuutttt", "futuuutuf", "tuftuuttt",
        "tufuuufut", "tufuuffff", "ttttuutuf", "tttuutfut",
        "ffuffuuut", "fufutufuf", "ftutufuft", "ftutftutu",
        "fftfutttt", "fuuuttutt", "tfftufttt", "tttfutfft",
        "uffuffttt", "uutfftfft", "fuuuuuuut", "uttfutffu",
        "ufftufttu", "ffufututt", "ttutufuff", "tutufutut",
        "uftfuftfu", "ufufututu", "tufutufut", "futufutuf"
    };

    private string[] submissions = {
    //  "00000000011111111112", "00000000011111111112", "00000000011111111112", "00000000011111111112"
    //  "12345678901234567890", "12345678901234567890", "12345678901234567890", "12345678901234567890"
        "00000000000000001100", "00000000000011000000", "00110000000011000000", "00001100000000000101",
        "00001100000000001111", "00000000001100000011", "00000000001100000000", "00001100000000001010",
        "11100000000011110001", "00110000001100000000", "00001100000000001100", "00000000000011110000",
        "00001100001100000000", "00001100001100001111", "00001100110000000101", "00001111000000001010",
        "00001000001100000101", "00000100001100001010", "00110000000000001100", "00000000000010010101",
        "00000000000001101010", "00111100001100000000", "11001100001100000000", "11000000001100000000",
        "00000000000001011111", "00000000000000001111", "11110000000000000000", "11110000000001010000"
    };

    void Awake()
    {
        _moduleID = _moduleIdCounter++;

        foreach (KMSelectable disp in displays)
        {
            disp.OnInteract += delegate ()
            {
                DisplayPress(disp);
                return false;
            };
        }

        foreach (KMSelectable wr in wires)
        {
            wr.OnInteract += delegate ()
            {
                WireCut(wr);
                return false;
            };
        }
    }

    // Use this for initialization
    void Start () {
        for (int i = 0; i < 20; i++)
        {
            uncutWires[i].gameObject.SetActive(true);
            cutWires[i].gameObject.SetActive(true);
        }

        switch (UnityEngine.Random.Range(0, 6))
        {
            case 0:
                displayTexts[0].text = "C";
                displayTexts[1].text = "B";
                displayTexts[2].text = "W";
                break;
            case 1:
                displayTexts[0].text = "BU";
                displayTexts[1].text = "TW";
                displayTexts[2].text = "HY";
                break;
            case 2:
                displayTexts[0].text = "RE";
                displayTexts[1].text = "AD";
                displayTexts[2].text = "Y?";
                break;
            case 3:
                displayTexts[0].text = "*D";
                displayTexts[1].text = "EA";
                displayTexts[2].text = "D*";
                break;
            case 4:
                displayTexts[0].text = "OH";
                displayTexts[1].text = "NO";
                displayTexts[2].text = "...";
                break;
            case 5:
                displayTexts[0].text = "JU";
                displayTexts[1].text = "ST";
                displayTexts[2].text = "NO";
                break;
        }

        startTime = (int) (Bomb.GetTime() / 60);
        wireMech.transform.localPosition = new Vector3(0f, 0.045f, 0.192f); // default Y = 0.145
        coverPlane.transform.localPosition = new Vector3(0f, 0.145f, 0.192f); // default X = 0.8144
        coverPlane.transform.localScale = new Vector3(0.1616f, 1f, 0.12f); // default X = 0.0016
        Module.OnActivate += Initialize;
	}

    void Initialize()
    {
        isActivated = true;
        Generate();
    }

    void DisplayPress(KMSelectable disp)
    {
        StartCoroutine(DisplayPress2(disp));
    }
    IEnumerator DisplayPress2(KMSelectable disp) // Submission/reset stuff
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        disp.AddInteractionPunch(0.1f);
        if (!isAnimating && isActivated)
        {
            isAnimating = true;
            StartCoroutine(HideWires());
            while (mechChk) yield return new WaitForSeconds(0.1f);

            yield return new WaitForSeconds(0.3f);

            switch (Array.IndexOf(displays, disp))
            {
                case 0:
                    string x = "";
                    for (int i = 0; i < 20; i++)
                    {
                        x += wireStates[i] ? "0" : "1";
                    }

                    string y = "";
                    if (submissions.Contains(x)) y = "\"" + operators[Array.IndexOf(submissions, x)] + "\"";
                    else y = "NOTHING";

                    Debug.LogFormat("[Cruel Boolean Wires #{0}] >>> SUBMITTED: " + x + "; RESEMBLES " + y, _moduleID);

                    if (x == solution) // Correct answer
                    {
                        if (stage != 5) // Stage advance
                        {
                            Debug.LogFormat("[Cruel Boolean Wires #{0}] >>> CORRECT SUBMISSION; ADVANCING STAGE", _moduleID);
                            Audio.PlaySoundAtTransform("Good", transform);
                            displayTexts[0].text = "" + stage;
                            displayTexts[1].text = "/";
                            displayTexts[2].text = "5";

                            foreach (var wire in wires)
                            {
                                if (wireStates[Array.IndexOf(uncutWires, wire.gameObject)])
                                {
                                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                                    wire.AddInteractionPunch(0.1f);

                                    wire.gameObject.SetActive(true);
                                    cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(false);
                                    wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = false;
                                    yield return new WaitForSeconds(0.1f);
                                }
                            }

                            yield return new WaitForSeconds(0.3f);

                            displayTexts[0].text = "";
                            displayTexts[1].text = "";
                            displayTexts[2].text = "";

                            yield return new WaitForSeconds(0.3f);

                            Generate();
                            break;
                        }
                        else // Module solved
                        {
                            moduleSolved = true;
                            Debug.LogFormat("[Cruel Boolean Wires #{0}] >>> FIVE STAGES PASSED; MODULE SOLVED", _moduleID);
                            Audio.PlaySoundAtTransform("Awesome", transform);

                            switch (UnityEngine.Random.Range(0, 6))
                            {
                                case 0:
                                    displayTexts[0].text = "SO";
                                    displayTexts[1].text = "LV";
                                    displayTexts[2].text = "ED";
                                    break;
                                case 1:
                                    displayTexts[0].text = "DO";
                                    displayTexts[1].text = "NE";
                                    displayTexts[2].text = "x)";
                                    break;
                                case 2:
                                    displayTexts[0].text = "IT";
                                    displayTexts[1].text = "GO";
                                    displayTexts[2].text = "OD";
                                    break;
                                case 3:
                                    displayTexts[0].text = "GG";
                                    displayTexts[1].text = "!!";
                                    displayTexts[2].text = ":O";
                                    break;
                                case 4:
                                    displayTexts[0].text = "NI";
                                    displayTexts[1].text = "CE";
                                    displayTexts[2].text = ":D";
                                    break;
                                case 5:
                                    displayTexts[0].text = "BY";
                                    displayTexts[1].text = "BC";
                                    displayTexts[2].text = "MG";
                                    break;
                            }
                            foreach (var wire in wires)
                            {
                                if (! wireStates[Array.IndexOf(uncutWires, wire.gameObject)])
                                {
                                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                                    wire.AddInteractionPunch(0.1f);

                                    wire.gameObject.SetActive(false);
                                    cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(true);
                                    wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = true;
                                    yield return new WaitForSeconds(0.1f);
                                }
                            }

                            foreach (var m in wireTextures)
                            {
                                m.material = materials[1];
                            }

                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wires[13].transform);
                            wires[13].AddInteractionPunch(0.1f);

                            wires[13].gameObject.SetActive(true);
                            cutWires[13].gameObject.SetActive(false);
                            wireStates[Array.IndexOf(uncutWires, wires[13].gameObject)] = false;
                            yield return new WaitForSeconds(0.1f);

                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wires[15].transform);
                            wires[15].AddInteractionPunch(0.1f);

                            wires[15].gameObject.SetActive(true);
                            cutWires[15].gameObject.SetActive(false);
                            wireStates[Array.IndexOf(uncutWires, wires[15].gameObject)] = false;
                            yield return new WaitForSeconds(0.1f);

                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wires[7].transform);
                            wires[7].AddInteractionPunch(0.1f);

                            wires[7].gameObject.SetActive(true);
                            cutWires[7].gameObject.SetActive(false);
                            wireStates[Array.IndexOf(uncutWires, wires[14].gameObject)] = false;

                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

                            float duration = 0.2f;
                            float elapsed = 0f;

                            while (elapsed < duration)
                            {
                                coverPlane.transform.localPosition = new Vector3((elapsed / duration) * 0.8144f, 0.145f, 0.192f);
                                coverPlane.transform.localScale = new Vector3((0.1616f - (elapsed / duration) * 0.16f), 1f, 0.12f);
                                yield return null;
                                elapsed += Time.deltaTime;
                            }
                            coverPlane.transform.localPosition = new Vector3(0.8144f, 0.145f, 0.192f);
                            coverPlane.transform.localScale = new Vector3(0.0016f, 1f, 0.12f);

                            duration = 0.2f;
                            elapsed = 0f;

                            while (elapsed < duration)
                            {
                                wireMech.transform.localPosition = new Vector3(0f, 0.045f + (elapsed / duration) * 0.1f, 0.192f);
                                yield return null;
                                elapsed += Time.deltaTime;
                            }
                            wireMech.transform.localPosition = new Vector3(0f, 0.145f, 0.192f);

                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                            Module.HandlePass();
                        }

                        break;
                    }
                    else // Strike
                    {
                        Debug.LogFormat("[Cruel Boolean Wires #{0}] >>> INCORRECT SUBMISSION; MODULE STRIKED", _moduleID);
                        Module.HandleStrike();
                        if (stage != 5)
                        {
                            switch (UnityEngine.Random.Range(0, 6))
                            {
                                case 0:
                                    displayTexts[0].text = "ST";
                                    displayTexts[1].text = "RI";
                                    displayTexts[2].text = "KE";
                                    break;
                                case 1:
                                    displayTexts[0].text = "NO";
                                    displayTexts[1].text = "PE";
                                    displayTexts[2].text = ":)";
                                    break;
                                case 2:
                                    displayTexts[0].text = "WR";
                                    displayTexts[1].text = "ON";
                                    displayTexts[2].text = "G!";
                                    break;
                                case 3:
                                    displayTexts[0].text = "FA";
                                    displayTexts[1].text = "IL";
                                    displayTexts[2].text = "ED";
                                    break;
                                case 4:
                                    displayTexts[0].text = "NO";
                                    displayTexts[1].text = "?!";
                                    displayTexts[2].text = ":(";
                                    break;
                                case 5:
                                    displayTexts[0].text = "HA";
                                    displayTexts[1].text = "RD";
                                    displayTexts[2].text = ":)";
                                    break;
                            }
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 6))
                            {
                                case 0:
                                    displayTexts[0].text = "CL";
                                    displayTexts[1].text = "OS";
                                    displayTexts[2].text = "E.";
                                    break;
                                case 1:
                                    displayTexts[0].text = "AL";
                                    displayTexts[1].text = "MO";
                                    displayTexts[2].text = "ST";
                                    break;
                                case 2:
                                    displayTexts[0].text = "NE";
                                    displayTexts[1].text = "AR";
                                    displayTexts[2].text = "LY";
                                    break;
                                case 3:
                                    displayTexts[0].text = "GI";
                                    displayTexts[1].text = "TG";
                                    displayTexts[2].text = "UD";
                                    break;
                                case 4:
                                    displayTexts[0].text = "GO";
                                    displayTexts[1].text = "BA";
                                    displayTexts[2].text = "CK";
                                    break;
                                case 5:
                                    displayTexts[0].text = "WA";
                                    displayTexts[1].text = "ST";
                                    displayTexts[2].text = "ED";
                                    break;
                            }

                            Audio.PlaySoundAtTransform("Stage 5 Fail", transform);
                            displays[0].AddInteractionPunch(50f);
                        }
                        foreach (var wire in wires)
                        {
                            if (wireStates[Array.IndexOf(uncutWires, wire.gameObject)])
                            {
                                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                                wire.AddInteractionPunch(0.1f);

                                wire.gameObject.SetActive(true);
                                cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(false);
                                wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = false;
                                yield return new WaitForSeconds(0.1f);
                            }
                        }

                        yield return new WaitForSeconds(0.3f);

                        displayTexts[0].text = "";
                        displayTexts[1].text = "";
                        displayTexts[2].text = "";
                        stage -= 1;

                        yield return new WaitForSeconds(0.3f);

                        Generate();

                        break;
                    }
                case 1:
                    
                    displayTexts[0].text = "" + stage;
                    displayTexts[1].text = "/";
                    displayTexts[2].text = "5";

                    foreach (var wire in wires)
                    {
                        if (wireStates[Array.IndexOf(uncutWires, wire.gameObject)])
                        {
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                            wire.AddInteractionPunch(0.1f);

                            wire.gameObject.SetActive(true);
                            cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(false);
                            wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = false;
                            yield return new WaitForSeconds(0.1f);
                        }
                    }

                    yield return new WaitForSeconds(0.3f);

                    displayTexts[0].text = "";
                    displayTexts[1].text = "";
                    displayTexts[2].text = "";

                    yield return new WaitForSeconds(0.3f);

                    StartCoroutine(ShowWires(leftBoolean, rightBoolean, (100 + startPos).ToString().Substring(1, 2)));
                    break;
                case 2:
                    
                    switch (UnityEngine.Random.Range(0, 6))
                    {
                        case 0:
                            displayTexts[0].text = "RE";
                            displayTexts[1].text = "RO";
                            displayTexts[2].text = "LL";
                            break;
                        case 1:
                            displayTexts[0].text = "RE";
                            displayTexts[1].text = "SE";
                            displayTexts[2].text = "T!";
                            break;
                        case 2:
                            displayTexts[0].text = "AG";
                            displayTexts[1].text = "AI";
                            displayTexts[2].text = "N?";
                            break;
                        case 3:
                            displayTexts[0].text = "BA";
                            displayTexts[1].text = "DR";
                            displayTexts[2].text = "NG";
                            break;
                        case 4:
                            displayTexts[0].text = "IG";
                            displayTexts[1].text = "UE";
                            displayTexts[2].text = "SS";
                            break;
                        case 5:
                            displayTexts[0].text = "OK";
                            displayTexts[1].text = "TH";
                            displayTexts[2].text = "EN";
                            break;

                    }

                    Debug.LogFormat("[Cruel Boolean Wires #{0}] >>> MODULE VOLUNTARILY RESET", _moduleID);
                    Audio.PlaySoundAtTransform("You lose", transform);

                    foreach (var wire in wires)
                    {
                        if (wireStates[Array.IndexOf(uncutWires, wire.gameObject)])
                        {
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                            wire.AddInteractionPunch(0.1f);

                            wire.gameObject.SetActive(true);
                            cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(false);
                            wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = false;
                            yield return new WaitForSeconds(0.1f);
                        }
                    }

                    yield return new WaitForSeconds(0.3f);

                    displayTexts[0].text = "";
                    displayTexts[1].text = "";
                    displayTexts[2].text = "";
                    stage = 0;

                    yield return new WaitForSeconds(0.3f);

                    Generate();
                    break;
            }
        }
        yield return null;
    }

    void WireCut(KMSelectable wire)
    {
        if (isAnimating || mechChk || wireStates[Array.IndexOf(uncutWires, wire.gameObject)]) return;

        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
        wire.AddInteractionPunch(0.1f);

        wire.gameObject.SetActive(false);
        cutWires[Array.IndexOf(uncutWires, wire.gameObject)].gameObject.SetActive(true);
        wireStates[Array.IndexOf(uncutWires, wire.gameObject)] = true;
    }

    // Determining values
    void Generate()
    {
        // ===PART 1: GENERATING BOOLEANS===
        // Braces to free up space on Microsoft Visual Studio; making the logging for this part was AIDS
        attempt++;
        stage++;
        prevStage = leftLetter + rightLetter;

        Debug.LogFormat("[Cruel Boolean Wires #{0}] <<<< Attempt {1} (Stage {2}/5) >>>>", _moduleID, attempt, stage);
        Debug.LogFormat("[Cruel Boolean Wires #{0}] ==BOOLEAN GENERATION==", _moduleID);
        {
            // ==NUMBERS==

            Debug.LogFormat("[Cruel Boolean Wires #{0}] (The previous stage was \"" + prevStage + "\". The bomb was started on day " + day2 + " of the month, and on a " + day + ".)", _moduleID);
            // Value 0
            if (stage % 3 == 0)
            {
                booleanValues[0].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 0 is UNKNOWN because the current stage is " + stage + ", which is a multiple of 3.", _moduleID);
            }
            else if (stage % 3 == 1)
            {
                booleanValues[0].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 0 is TRUE because the current stage is " + stage + ", which is 1 more than a multiple of 3.", _moduleID);
            }
            else
            {
                booleanValues[0].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 0 is FALSE because the current stage is " + stage + ", which is 1 less than a multiple of 3.", _moduleID);
            }

            // Value 1
            if (Bomb.GetModuleNames().Contains("Bamboozled Again") || Bomb.GetModuleNames().Contains("Ultimate Cycle") || Bomb.GetModuleNames().Contains("UltraStores"))
            {
                booleanValues[1].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 1 is TRUE because the answer for Password Generator would be *DEAD*.", _moduleID);
            }
            else if (Bomb.GetModuleNames().Contains("Question Mark") || Bomb.GetModuleNames().Contains("Astrology"))
            {
                booleanValues[1].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 1 is UNKNOWN because the answer for Password Generator would contain a question mark or asterisk but also would NOT be *DEAD*.", _moduleID);
            }
            else
            {
                booleanValues[1].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 1 is FALSE because the answer for Password Generator would contain an ampersand.", _moduleID);
            }

            // Value 2
            if (Bomb.GetSerialNumber()[5] == '2' || Bomb.GetSerialNumber()[5] == '3' || Bomb.GetSerialNumber()[5] == '5' || Bomb.GetSerialNumber()[5] == '7')
            {
                booleanValues[2].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 2 is FALSE because the last digit of the serial number is prime.", _moduleID);
            }
            else if (Bomb.GetSerialNumber()[5] == '0' || Bomb.GetSerialNumber()[5] == '1' || Bomb.GetSerialNumber()[5] == '4' || Bomb.GetSerialNumber()[5] == '9')
            {
                booleanValues[2].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 2 is TRUE because the last digit of the serial number is square.", _moduleID);
            }
            else
            {
                booleanValues[2].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 2 is UNKNOWN because the last digit of the serial number is neither square nor prime.", _moduleID);
            }

            // Value 3
            if (prevStage == "??")
            {
                booleanValues[3].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 3 is UNKNOWN because there is no previous stage.", _moduleID);
            }
            else
            {
                temp = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(prevStage[0])
                    + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(prevStage[1]);

                if (temp % 3 == 0)
                {
                    booleanValues[3].setValue("U");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 3 is UNKNOWN because the sum of the previous display's base-36 equivalents is " + temp + ", which is a multiple of 3.", _moduleID);
                }
                else if (temp % 3 == 1)
                {
                    booleanValues[3].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 3 is TRUE because the sum of the previous display's base-36 equivalents is " + temp + ", which is 1 more than a multiple of 3.", _moduleID);
                }
                else
                {
                    booleanValues[3].setValue("F");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 3 is FALSE because the sum of the previous display's base-36 equivalents is " + temp + ", which is 1 less than a multiple of 3.", _moduleID);
                }
            }

            // Value 4
            switch (day)
            {
                case "Monday":
                    booleanValues[4].setValue("F");
                    break;
                case "Friday":
                    booleanValues[4].setValue("F");
                    break;
                case "Saturday":
                    booleanValues[4].setValue("T");
                    break;
                case "Sunday":
                    booleanValues[4].setValue("T");
                    break;
                default:
                    booleanValues[4].setValue("U");
                    break;
            }
            switch (booleanValues[4].toString())
            {
                case "T":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 4 is TRUE because the bomb was started on a " + day + ".", _moduleID);
                    break;
                case "F":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 4 is FALSE because the bomb was started on a " + day + ".", _moduleID);
                    break;
                case "U":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 4 is UNKNOWN because the bomb was started on a " + day + ".", _moduleID);
                    break;
            }

            // Value 5
            if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count())
            {
                booleanValues[5].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 5 is FALSE because there are more lit than unlit indicators.", _moduleID);
            }
            else if (Bomb.GetOnIndicators().Count() < Bomb.GetOffIndicators().Count())
            {
                booleanValues[5].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 5 is UNKNOWN because there are more unlit than lit indicators.", _moduleID);
            }
            else
            {
                booleanValues[5].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 5 is TRUE because there are equal amounts of unlit and lit indicators.", _moduleID);
            }

            // Value 6
            if (Bomb.GetPortCount()==0)
                booleanValues[6].setValue("F");
            else if (Bomb.GetSolvedModuleIDs().Count() % Bomb.GetPortCount() == 0)
                booleanValues[6].setValue("U");
            else booleanValues[6].setValue("T");

            switch (booleanValues[6].toString())
            {
                case "T":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 6 is TRUE because the number of solved modules ({1}) is NOT a multiple of the number of ports.", _moduleID, Bomb.GetSolvedModuleNames().Count());
                    break;
                case "F":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 6 is FALSE because there are no ports.", _moduleID);
                    break;
                case "U":
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 6 is UNKNOWN because the number of solved modules ({1}) is a multiple of the number of ports.", _moduleID, Bomb.GetSolvedModuleNames().Count());
                    break;
            }

            // Value 7
            if ("BCDEGKPTVZ".Contains(Bomb.GetSerialNumber()[3]) == "BCDEGKPTVZ".Contains(Bomb.GetSerialNumber()[4]))
            {
                booleanValues[7].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 7 is TRUE because the boolean of the \"Unknown\" column matches the boolean of the \"False\" column.", _moduleID);
            }
            else if ("BCDEGKPTVZ".Contains(Bomb.GetSerialNumber()[3]))
            {
                booleanValues[7].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 7 is FALSE because the fourth character of the serial number exists in Two Bits while the fifth does not.", _moduleID);
            }
            else
            {
                booleanValues[7].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 7 is UNKNOWN because the fifth character of the serial number exists in Two Bits while the fourth does not.", _moduleID);
            }

            // Value 8
            temp = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[0]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[1]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[2]) +
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[3]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[4]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[5]);
            if (temp < 72)
            {
                booleanValues[8].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 8 is FALSE because the sum of the base-36 digits in the serial number (" + temp + ") is less than 72.", _moduleID);
            }
            else if (temp > 106)
            {
                booleanValues[8].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 8 is TRUE because the sum of the base-36 digits in the serial number (" + temp + ") is greater than 106.", _moduleID);
            }
            else
            {
                booleanValues[8].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 8 is UNKNOWN because the sum of the base-36 digits in the serial number (" + temp + ") is in the range 72-106.", _moduleID);
            }

            // Value 9
            if (Bomb.GetSerialNumberLetters().Count() == 4)
            {
                booleanValues[9].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 9 is FALSE because there are 4 letters and 2 digits in the serial number.", _moduleID);
            }
            else if (Bomb.GetSerialNumberLetters().Count() == 3)
            {
                booleanValues[9].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 9 is UNKNOWN because there are 3 letters and 3 digits in the serial number.", _moduleID);
            }
            else
            {
                booleanValues[9].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value 9 is TRUE because there are 2 letters and 4 digits in the serial number.", _moduleID);
            }

            // ==LETTERS==

            // Value A
            temp64 = 0;
            for (int i = 0; i < 6; i++)
            {
                temp64 = temp64 * 36 + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[i]);
            }
            temp = (int)(temp64 % 9157);
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (The serial number in base-36 is " + temp64 + ", and is " + temp + " when taken modulo 9157.)", _moduleID);
            if (temp > 7919)
            {
                booleanValues[10].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value A is UNKNOWN because the resulting number (" + temp + ") is outside the scope of this module's prime number checker.", _moduleID);
            }
            else
            {
                chk = false;
                for (int i = 2; i < 90; i++)
                {
                    if (temp % i == 0 && temp != i)
                    {
                        chk = true;
                        break;
                    }
                }
                if (!chk && temp != 0)
                {
                    booleanValues[10].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value A is TRUE because the resulting number (" + temp + ") is prime.", _moduleID);
                }
                else
                {
                    booleanValues[10].setValue("F");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value A is FALSE because the resulting number (" + temp + ") is not prime.", _moduleID);
                }
            }

            // Value B
            temp = 0;
            temp2 = 0;
            for (int i = 0; i < Bomb.GetModuleIDs().Count; i++)
            {
                if (extremes.Contains(Bomb.GetModuleIDs()[i])) temp++;
                if (vanillas.Contains(Bomb.GetModuleIDs()[i])) temp2++;
            }
            chk = false;
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (There is/are " + temp + " extreme module(s) and " + temp2 + " vanilla module(s).)", _moduleID);
            foreach (int i in lucasNumbers) {
                if (Math.Abs(temp * temp2 - i) <= 20) chk = true;
            }
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (The resulting product is " + (temp * temp2) + ".)", _moduleID);
            if (temp * temp2 > 600)
            {
                booleanValues[11].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value B is UNKNOWN because the resulting number (" + (temp * temp2) + ") is outside the scope of this module's Lucas number checker.", _moduleID);
            }
            else if (chk)
            {
                booleanValues[11].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value B is TRUE because the resulting number (" + (temp * temp2) + ") is within 20 of a Lucas number.", _moduleID);
            }
            else
            {
                booleanValues[11].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value B is FALSE because the resulting number (" + (temp * temp2) + ") is not within 20 of a Lucas number.", _moduleID);

            }

            // Value C
            if (Bomb.GetBatteryHolderCount() == 0)
            {
                booleanValues[12].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value C is UNKNOWN because the lack of battery holders caused this module to attempt to divide by zero.", _moduleID);
            }
            else if ((double)Bomb.GetBatteryCount() / (double)Bomb.GetBatteryHolderCount() < 1.4999)
            {
                booleanValues[12].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value C is TRUE because the battery count divided by the battery holder count is " + (double)Bomb.GetBatteryCount() / (double)Bomb.GetBatteryHolderCount() + ", which is less than 1.5.", _moduleID);
            }
            else
            {
                booleanValues[12].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value C is FALSE because the battery count divided by the battery holder count is " + (double)Bomb.GetBatteryCount() / (double)Bomb.GetBatteryHolderCount() + ", which is not less than 1.5.", _moduleID);
            }

            // Value D
            temp = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[0]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[1]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[2]) +
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[3]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[4]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[5]);
            if (lucasNumbers.Contains(temp) || fibonacciNumbers.Contains(temp))
            {
                booleanValues[13].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value D is TRUE because the sum of the base-36 digits in the serial number (" + temp + ") is either a Lucas number or a Fibonacci number.", _moduleID);
            }
            else
            {
                booleanValues[13].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value D is FALSE because the sum of the base-36 digits in the serial number (" + temp + ") is neither a Lucas number or a Fibonacci number.", _moduleID);
            }

            // Value E
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (There is/are " + Bomb.GetSolvedModuleIDs().Count + " solved module(s) and " + (Bomb.GetSolvableModuleIDs().Count - Bomb.GetSolvedModuleIDs().Count) + " unsolved module(s))", _moduleID);
            if (Bomb.GetSolvedModuleIDs().Count() * 2 > Bomb.GetSolvableModuleIDs().Count)
            {
                booleanValues[14].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value E is TRUE because there are more solved than unsolved modules.", _moduleID);
            }
            else
            {
                booleanValues[14].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value E is FALSE because there are less solved than unsolved modules.", _moduleID);
            }
            // Value F
            temp = 0;
            if (Bomb.IsIndicatorPresent("FRK") || Bomb.IsIndicatorPresent("FRQ"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (In F, the Red rule applies.)", _moduleID);
            }
            if (Bomb.GetSerialNumber().Contains("F") || Bomb.GetSerialNumber().Contains("0") || Bomb.GetSerialNumber().Contains("X"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (In F, the Purple rule applies.)", _moduleID);
            }

            if (temp == 0)
            {
                booleanValues[15].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value F is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[15].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value F is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[15].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value F is TRUE because both rules apply.", _moduleID);
            }

            // Value G
            chk = true;
            for (int i = 0; i < Bomb.GetModuleNames().Count(); i++)
            {
                if (Bomb.GetModuleNames()[i].Length == 1)
                {
                    chk = false;
                    break;
                }
            }
            if (chk)
            {
                booleanValues[16].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value G is TRUE because there are no one-character modules on the bomb.", _moduleID);
            }
            else
            {
                booleanValues[16].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value G is FALSE because there are one-character modules on the bomb.", _moduleID);
            }
            // Value H
            temp = 0;
            if (Bomb.GetSerialNumber().Contains("H"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (The serial number contains an H.)", _moduleID);
            }
            if (!Bomb.IsIndicatorPresent("SIG"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (There is no SIG indicator.)", _moduleID);
            }
            if (temp == 0)
            {
                booleanValues[17].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value H is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[17].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value H is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[17].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value H is TRUE because both rules apply.", _moduleID);
            }

            // Value I
            if (prevStage == "??")
            {
                booleanValues[18].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value I is UNKNOWN because there is no previous stage.", _moduleID);
            }
            else if (Bomb.GetSerialNumber().Contains(prevStage[0]) || Bomb.GetSerialNumber().Contains(prevStage[1]))
            {
                booleanValues[18].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value I is TRUE because the previous display had a serial number character.", _moduleID);
            }
            else
            {
                booleanValues[18].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value I is FALSE because neither previous display had a serial number character.", _moduleID);
            }

            // Value J
            temp = 0;
            if (Bomb.GetModuleNames().Contains("Boolean Wires"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (Boolean Wires is present.)", _moduleID);
            }
            if (Bomb.GetModuleNames().Contains("RGB Arithmetic"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (RGB Arithmetic is present.)", _moduleID);
            }

            if (temp == 0)
            {
                booleanValues[19].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value J is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[19].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value J is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[19].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value J is TRUE because both rules apply.", _moduleID);
            }

            // Value K
            for (int i = 2; i >= 0; i--)
            {
                if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(Bomb.GetSerialNumber()[i])) temp = i;
            }
            if ("ACEFJKMOQRSUXZ".Contains(Bomb.GetSerialNumber()[temp]))
            {
                booleanValues[20].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value K is TRUE because the corresponding Set 1 Boozleglyph with the first character of the serial number (" + Bomb.GetSerialNumber()[temp] + ") is a square.", _moduleID);
            }
            else if ("BGNPTVW".Contains(Bomb.GetSerialNumber()[temp]))
            {
                booleanValues[20].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value K is UNKNOWN because the corresponding Set 1 Boozleglyph with the first character of the serial number (" + Bomb.GetSerialNumber()[temp] + ") is a triangle.", _moduleID);
            }
            else
            {
                booleanValues[20].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value K is FALSE because the corresponding Set 1 Boozleglyph with the first character of the serial number (" + Bomb.GetSerialNumber()[temp] + ") is empty.", _moduleID);
            }

            // Value L
            if (Bomb.IsIndicatorPresent("BOB"))
            {
                if (Bomb.IsIndicatorOn("BOB"))
                {
                    booleanValues[21].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value L is TRUE because a lit BOB indicator is present.", _moduleID);
                }
                else
                {
                    booleanValues[21].setValue("U");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value L is UNKNOWN because an unlit BOB indicator is present.", _moduleID);
                }
            }
            else
            {
                booleanValues[21].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value L is FALSE because there is no BOB indicator.", _moduleID);
            }
            // Value M
            if (Bomb.IsIndicatorPresent("SND"))
            {
                if (Bomb.IsIndicatorOn("SND"))
                {
                    booleanValues[22].setValue("U");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value M is UNKNOWN because a lit SND indicator is present.", _moduleID);
                }
                else
                {
                    booleanValues[22].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value M is TRUE because an unlit SND indicator is present.", _moduleID);
                }
            }
            else
            {
                booleanValues[22].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value M is FALSE because there is no SND indicator.", _moduleID);
            }

            // Value N
            if (day2 > 28)
            {
                booleanValues[23].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value N is UNKNOWN because the day of the month the bomb was activated on (" + day2 + ") is outside the scope of this module's date checker.", _moduleID);
            }
            else if (day2 % 2 == 1)
            {
                booleanValues[23].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value N is TRUE because the day of the month the bomb was activated on (" + day2 + ") is odd.", _moduleID);
            }
            else
            {
                booleanValues[23].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value N is FALSE because the day of the month the bomb was activated on (" + day2 + ") is even.", _moduleID);
            }

            // Value O
            temp = 0;
            if (Bomb.GetModuleNames().Contains("OmegaForget"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (OmegaForget is present.)", _moduleID);
            }
            if (Bomb.GetModuleNames().Contains("OmegaDestroyer"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (OmegaDestroyer is present.)", _moduleID);
            }

            if (temp == 0)
            {
                booleanValues[24].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value O is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[24].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value O is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[24].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value O is TRUE because both rules apply.", _moduleID);
            }

            // Value P
            if (day2 > 28)
            {
                booleanValues[25].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value P is UNKNOWN because the day of the month the bomb was activated on (" + day2 + ") is outside the scope of this module's date checker.", _moduleID);
            }
            else if (prevStage == "??")
            {
                booleanValues[25].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value P is UNKNOWN because there is no previous stage.", _moduleID);
            }
            else if (prevStage.Contains("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[day2]))
            {
                booleanValues[25].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value P is TRUE because the day of the month the bomb was activated on, in base-36 (" + day2 + " = " + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[day2] + "), exists in the previous stage.", _moduleID);
            }
            else
            {
                booleanValues[25].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value P is TRUE because the day of the month the bomb was activated on, in base-36 (" + day2 + " = " + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[day2] + "), does not exist in the previous stage.", _moduleID);
            }

            // Value Q
            if (Bomb.GetModuleNames().Count >= 47)
            {
                booleanValues[26].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Q is TRUE because there are 47 or more modules.", _moduleID);
            }
            else
            {
                booleanValues[26].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Q is FALSE because there are less than 47 modules.", _moduleID);
            }

            // Value R
            temp = 0;
            if (!Bomb.GetModuleNames().Contains("hexOS"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (hexOS is not present.)", _moduleID);
            }
            if (!Bomb.GetModuleNames().Contains("Worse Venn Diagram"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (Worse Venn Diagram is not present.)", _moduleID);
            }

            if (temp == 0)
            {
                booleanValues[27].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value R is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[27].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value R is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[27].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value R is TRUE because both rules apply.", _moduleID);
            }

            // Value S
            temp2 = Bomb.GetModuleNames().Count(name => name.Contains("Simon"));
            //temp2 = 7;
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (There are " + temp2 + " modules with the word \"Simon\" in them.)", _moduleID);
            chk = false;
            if (temp2 < 2) chk = true;
            for (int i = 2; i < 400; i++)
            {
                if (temp2 % i == 0 && temp2 != i)
                {
                    chk = true; // If chk, the number is not prime
                    break;
                }
            }
            temp = 0;
            if (!chk)
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (" + temp2 + " is a prime number.)", _moduleID);
            }
            if (Bomb.GetModuleNames().Contains("Simon's Ultimate Showdown"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (Simon's Ultimate Showdown is present.)", _moduleID);
            }
            if (temp == 0)
            {
                booleanValues[28].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value S is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[28].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value S is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[28].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value S is TRUE because both rules apply.", _moduleID);
            }

            // Value T
            chk = false;
            foreach (string module in idcs)
            {
                if (Bomb.GetModuleIDs().Contains(module)) chk = true;
            }
            if (chk)
            {
                booleanValues[29].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value T is TRUE because there is a module directly mentioned in Identification Crisis that is present.", _moduleID);
            }
            else if (Bomb.GetModuleIDs().Contains("identificationCrisis"))
            {
                booleanValues[29].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value T is UNKNOWN because Identification Crisis is present, but none of its other mentions are present.", _moduleID);
            }
            else
            {
                booleanValues[29].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value T is FALSE because no modules related to Identification Crisis are present.", _moduleID);
            }

        // Value U
        chk = false;
        foreach (string module in ucrs)
        {
            if (Bomb.GetModuleIDs().Contains(module)) chk = true;
        }
            if (chk)
            {
                booleanValues[30].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value U is TRUE because there is a module directly mentioned in Unfair's Cruel Revenge that is present.", _moduleID);
            }
            else if (Bomb.GetModuleIDs().Contains("unfairsRevengeCruel"))
            {
                booleanValues[30].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value U is UNKNOWN because Unfair's Cruel Revenge is present, but none of its other mentions are present.", _moduleID);
            }
            else
            {
                booleanValues[30].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value U is FALSE because no modules related to Unfair's Cruel Revenge are present.", _moduleID);
            }

            // Value V
            if (Bomb.GetPortCount() < Bomb.GetBatteryCount())
            {
                booleanValues[31].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value V is TRUE because the number of ports is less than the number of batteries.", _moduleID);
            }
            else
            {
                booleanValues[31].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value V is FALSE because the number of ports is not less than the number of batteries.", _moduleID);
            }

        // Value W
        temp = 0;
            if (Bomb.GetModuleNames().Contains("D"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (D is present.)", _moduleID);
            }
            if (Bomb.GetModuleNames().Contains("Simon's Sums"))
            {
                temp++;
                Debug.LogFormat("[Cruel Boolean Wires #{0}] (Simon's Sums is present.)", _moduleID);
            }

            if (temp == 0)
            {
                booleanValues[32].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value W is FALSE because neither rule applies.", _moduleID);
            }
            else if (temp == 1)
            {
                booleanValues[32].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value W is UNKNOWN because exactly one rule applies.", _moduleID);
            }
            else
            {
                booleanValues[32].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value W is TRUE because both rules apply.", _moduleID);
            }

            // Value X
            temp64 = (int)Math.Pow(1.08, (double)("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[0]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[1]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[2]) +
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[3]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[4]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[5])));
            Debug.LogFormat("[Cruel Boolean Wires #{0}] (1.08 raised to the power of the sum of the base-36 digits in the serial number ({1}) results in ~" + temp64 + ".)", _moduleID, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[0]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[1]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[2]) +
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[3]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[4]) + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[5]));

            temp64 %= 10;
            temp = (int)temp64;

            if (temp == 1 || temp == 2 || temp == 3 || temp == 4 || temp == 7)
            {
                booleanValues[33].setValue("T");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value X is TRUE because the last digit of the result (" + temp64 + ") is in the Fibonacci sequence.", _moduleID);
            }
            else
            {
                booleanValues[33].setValue("F");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value X is FALSE because the last digit of the result (" + temp64 + ") is not in the Fibonacci sequence.", _moduleID);
            }

        // Value Y
        temp = Bomb.GetModuleIDs().Count;
        temp2 = 0;
            if (temp > 600)
            {
                booleanValues[34].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Y is UNKNOWN because the number of modules is outside the scope of the module's Lucas and Fibonacci number checkers.", _moduleID);
            }
            else
            {
                chk = false;
                foreach (int i in fibonacciNumbers)
                {
                    if (Math.Abs(Bomb.GetModuleIDs().Count - i) <= 4)
                    {
                        chk = true;
                        break;
                    }
                }
                if (chk)
                {
                    temp2++;
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] (The number of modules is within 4 of a Fibonacci number.)", _moduleID);
                }

                chk = false;
                foreach (int i in lucasNumbers)
                {
                    if (Math.Abs(Bomb.GetModuleIDs().Count - i) <= 2)
                    {
                        chk = true;
                        break;
                    }
                }
                if (!chk)
                {
                    temp2++;
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] (The number of modules is not within 2 of a Lucas number.)", _moduleID);
                }
                temp = temp2;
                if (temp == 0)
                {
                    booleanValues[34].setValue("F");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Y is FALSE because neither rule applies.", _moduleID);
                }
                else if (temp == 1)
                {
                    booleanValues[34].setValue("U");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Y is UNKNOWN because exactly one rule applies.", _moduleID);
                }
                else
                {
                    booleanValues[34].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Y is TRUE because both rules apply.", _moduleID);
                }
            }

            // Value Z
            if (startTime > 600)
            {
                booleanValues[35].setValue("U");
                Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Z is UNKNOWN because the bomb's starting time in minutes (" + startTime + ") is outside the scope of the module's Lucas and Fibonacci number checkers.", _moduleID);
            }
            else
            {
                chk = false;
                foreach (int i in fibonacciNumbers)
                {
                    if (Math.Abs(startTime - i) <= 5) chk = true;
                }
                foreach (int i in lucasNumbers)
                {
                    if (Math.Abs(startTime - i) <= 5) chk = true;
                }
                if (chk)
                {
                    booleanValues[35].setValue("T");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Z is TRUE because the bomb's starting time in minutes (" + startTime + ") is within 5 of a Fibonacci number or Lucas number.", _moduleID);
                }
                else
                {
                    booleanValues[35].setValue("F");
                    Debug.LogFormat("[Cruel Boolean Wires #{0}] > Value Z is FALSE because the bomb's starting time in minutes (" + startTime + ") is not within 5 of a Fibonacci number or Lucas number.", _moduleID);
                }
            }
    }
        // ===PART 2: GENERATING STAGE===
        summary = "";
        for (int i = 0; i < 36; i++)
        {
            summary += booleanValues[i].toString();
        }
        Debug.LogFormat("[Cruel Boolean Wires #{0}] >> SUMMARY OF GENERATED VALUES: {1}", _moduleID, summary);

        Debug.LogFormat("[Cruel Boolean Wires #{0}] ==STAGE GENERATION==", _moduleID);

        leftBoolean = "" + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[UnityEngine.Random.Range(0, 36)];
        rightBoolean = "" + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[UnityEngine.Random.Range(0, 36)];
        startPos = UnityEngine.Random.Range(1, 29);

        leftLetter = leftBoolean;
        rightLetter = rightBoolean;

        switch (UnityEngine.Random.Range(0, 3)) // Wire colors
        {
            case 0:
                stageColor.setValue("F");
                foreach (var m in wireTextures)
                {
                    m.material = materials[0];
                }
                break;
            case 1:
                stageColor.setValue("U");
                foreach (var m in wireTextures)
                {
                    m.material = materials[2];
                }
                break;
            case 2:
                stageColor.setValue("T");
                foreach (var m in wireTextures)
                {
                    m.material = materials[1];
                }
                break;
        }

        // Applying unary operators

        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                leftBoolean += "+";
                break;
            case 1:
                leftBoolean += "-";
                break;
            default:
                break;
        }

        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                rightBoolean += "+";
                break;
            case 1:
                rightBoolean += "-";
                break;
            default:
                break;
        }

        switch (UnityEngine.Random.Range(0, 2))
        {
            case 0:
                leftBoolean = "!" + leftBoolean;
                break;
            case 1:
                break;
        }

        switch (UnityEngine.Random.Range(0, 2))
        {
            case 0:
                rightBoolean = "!" + rightBoolean;
                break;
            case 1:
                break;
        }

        Debug.LogFormat("[Cruel Boolean Wires #{0}] The displays are [" + leftBoolean + "][" + rightBoolean + "][" + (100 + startPos).ToString().Substring(1,2) + "], and the wires are {1}.", _moduleID, 
            stageColor.toString() == "F" ? "red" : stageColor.toString() == "T" ? "green" : "blue"
            );

        Debug.LogFormat("[Cruel Boolean Wires #{0}] The left character is {1}, and the right character is {2}.", _moduleID, leftLetter, rightLetter);

        leftBool.setValue(booleanValues["0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(leftLetter)].toString());
        rightBool.setValue(booleanValues["0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(rightLetter)].toString());

        Debug.LogFormat("[Cruel Boolean Wires #{0}] Without unary operators, {1} evaluates to {3}, and {2} evaluates to {4}.", _moduleID, leftLetter, rightLetter, leftBool.toWord(), rightBool.toWord());

        str = "";
        if (leftBoolean.Contains("!")) str += "!";
        if (leftBoolean.Contains("+")) str += "+";
        if (leftBoolean.Contains("-")) str += "-";
        leftBool.modify(str);

        str = "";
        if (rightBoolean.Contains("!")) str += "!";
        if (rightBoolean.Contains("+")) str += "+";
        if (rightBoolean.Contains("-")) str += "-";
        rightBool.modify(str);

        Debug.LogFormat("[Cruel Boolean Wires #{0}] With unary operators, {1} evaluates to {3}, and {2} evaluates to {4}.", _moduleID, leftBoolean, rightBoolean, leftBool.toWord(), rightBool.toWord());
        Debug.LogFormat("[Cruel Boolean Wires #{0}] The resulting booleans are {1}{2}. We want the booleans to return with {3}.", _moduleID, leftBool.toString(), rightBool.toString(), stageColor.toString());

        // Finding valid operators

        temp = startPos - 1;
        temp2 = "FUT".IndexOf(rightBool.toString()) * 3 + "FUT".IndexOf(leftBool.toString());

        Debug.LogFormat("[Cruel Boolean Wires #{0}] The starting position (right display) is " + startPos + ", which results in the operator \"" + operators[startPos - 1] + "\".", _moduleID);

        while (! (truthValues[temp][temp2].ToString().ToUpper() == stageColor.toString()))
        {
            Debug.LogFormat("[Cruel Boolean Wires #{0}] The evaluation of operator " + (temp + 1) + " (" + operators[temp] + ") is " + truthValues[temp][temp2].ToString().ToUpper() + ", which does not match the intended boolean of " + stageColor.toString() + ". Moving to next...", _moduleID);
            temp = (temp + 1) % 28;
        }

        Debug.LogFormat("[Cruel Boolean Wires #{0}] The evaluation of operator " + (temp + 1) + " (" + operators[temp] + ") is " + truthValues[temp][temp2].ToString().ToUpper() + ", which matches the intended boolean of " + stageColor.toString() + "! This is our submission.", _moduleID);

        solution = submissions[temp];

        str = "";
        for (int i = 1; i < 21; i++)
        {
            if (solution[i - 1] == '0')
            {
                str = str + i + ", ";
            }
        }
        str = str.Substring(0, str.Length - 2);
        Debug.LogFormat("[Cruel Boolean Wires #{0}] The following wires must be cut: " + str, _moduleID);

        str = "";
        for (int i = 1; i < 21; i++)
        {
            if (solution[i - 1] == '1')
            {
                str = str + i + ", ";
            }
        }
        str = str.Substring(0, str.Length - 2);
        Debug.LogFormat("[Cruel Boolean Wires #{0}] The following wires must NOT be cut: " + str, _moduleID);

        Debug.LogFormat("[Cruel Boolean Wires #{0}] >> YOUR SUBMISSION SHOULD LOOK LIKE THIS: " + solution, _moduleID);

        StartCoroutine(ShowWires(leftBoolean, rightBoolean, (100 + startPos).ToString().Substring(1, 2)));
    }

    IEnumerator ShowWires(string d1, string d2, string d3) // Wire reveal animation
    {
        isAnimating = true;
        for (int i = 0; i < 20; i++)
        {
            uncutWires[i].gameObject.SetActive(true);
            cutWires[i].gameObject.SetActive(true);
        }
        d1 = FormatZeros(d1);
        d2 = FormatZeros(d2);
        d3 = FormatZeros(d3);
        displayTexts[0].text = "";
        displayTexts[1].text = "";
        displayTexts[2].text = "";
        yield return new WaitForSeconds(0.5f);
        displayTexts[0].text = d1;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.1f);
        displayTexts[1].text = d2;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.1f);
        displayTexts[2].text = d3;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            coverPlane.transform.localPosition = new Vector3((elapsed / duration) * 0.8144f, 0.145f, 0.192f);
            coverPlane.transform.localScale = new Vector3((0.1616f - (elapsed / duration) * 0.16f), 1f, 0.12f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        coverPlane.transform.localPosition = new Vector3(0.8144f, 0.145f, 0.192f);
        coverPlane.transform.localScale = new Vector3(0.0016f, 1f, 0.12f);

        duration = 0.2f;
        elapsed = 0f;

        while (elapsed < duration)
        {
            wireMech.transform.localPosition = new Vector3(0f, 0.045f + (elapsed / duration) * 0.1f, 0.192f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        wireMech.transform.localPosition = new Vector3(0f, 0.145f, 0.192f);

        isAnimating = false;
    }
    
    IEnumerator HideWires() // Wire reveal animation
    {
        mechChk = true;

        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            wireMech.transform.localPosition = new Vector3(0f, 0.045f + (1 - elapsed / duration) * 0.1f, 0.192f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        wireMech.transform.localPosition = new Vector3(0f, 0.045f, 0.192f);

        duration = 0.2f;
        elapsed = 0f;

        while (elapsed < duration)
        {
            coverPlane.transform.localPosition = new Vector3((1 - elapsed / duration) * 0.8144f, 0.145f, 0.192f);
            coverPlane.transform.localScale = new Vector3((0.1616f - (1 - elapsed / duration) * 0.16f), 1f, 0.12f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        coverPlane.transform.localPosition = new Vector3(0f, 0.145f, 0.192f);
        coverPlane.transform.localScale = new Vector3(0.1616f, 1f, 0.12f);

        displayTexts[0].text = "";
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.1f);
        displayTexts[1].text = "";
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.1f);
        displayTexts[2].text = "";
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
        yield return new WaitForSeconds(0.1f);

        mechChk = false;
    }

    string FormatZeros (string x) // All resulting zeros will have slashes
    {
        string y = "";
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] == '0') y += "Ø";
            else y += x[i];
        }
        return y;
    }

    // Update is called once per frame
    void Update () {

	}

    // Class because then I can think about ternary booleans
    class tbool
    {
        private string value;
        
        public tbool(string x)
        {
            if ((x != "T") && !(x != "F") && !(x != "U")) value = "F";
            else value = x;
        }

        public void setValue(string x)
        {
            if ((x != "T") && !(x != "F") && !(x != "U")) value = "F";
            else value = x;
        }

        public void modify(string function)
        {
            switch (function)
            {
                case "": // Nothing = do nothing (duh...)
                    break;
                case "+": // T->F->U->T
                    value = "" + "TFU"[("TFU".IndexOf(value) + 1) % 3];
                    break;
                case "-": // T->U->F->T
                    value = "" + "TFU"[("TFU".IndexOf(value) + 2) % 3];
                    break;
                case "!": // T swaps with F
                    if (! value.Equals("U")) value = "" + "FT"["TF".IndexOf(value)];
                    break;
                case "!+": // F swaps with U
                    if (!value.Equals("T")) value = "" + "FU"["UF".IndexOf(value)];
                    break;
                case "!-": // U swaps with T
                    if (!value.Equals("F")) value = "" + "TU"["UT".IndexOf(value)];
                    break;
            }
        }

        public string toString()
        {
            return value;
        }

        public string toWord()
        {
            switch (value)
            {
                case "T":
                    return "true";
                case "F":
                    return "false";
                default:
                    return "unknown";
            }
        }
    }

    // Twitch Plays :))))))
    private bool isWireValid(string a)
    {
        int temp1 = 0;
        bool preformed = int.TryParse(a, out temp1);
        return (preformed == true && (temp1 >= 1 && temp1 <= 20));
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cut <XYZWVU...> [Cut the specified wires (integers in the range 1-20; invalid numbers are skipped), ACCORDING TO THE APPENDIX. Multiple wire cuts can be chained.] // 
!{0} press <D> [Press the display in that position (left/middle/right).]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (parameters[0].EqualsIgnoreCase("press"))
        {
            yield return null;
            if (parameters[1].EqualsIgnoreCase("left"))
            {
                displays[0].OnInteract();
            }
            else if (parameters[1].EqualsIgnoreCase("middle"))
            {
                displays[1].OnInteract();
            }
            else if (parameters[1].EqualsIgnoreCase("right"))
            {
                displays[2].OnInteract();
            }
            else yield return "sendtochaterror That display doesn't exist!";
        }
        else if (parameters[0].EqualsIgnoreCase("cut"))
        {
            yield return null;
            int wireToCut = 0;
            for (int i = 1; i < parameters.Length; i++)
            {
                if (isWireValid(parameters[i]))
                {
                    yield return null;
                    int.TryParse(parameters[i], out wireToCut);
                    wires[wireToCut - 1].OnInteract();
                }
            }
        }
        else yield return "sendtochaterror That command doesn't exist!";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[Cruel Boolean Wires #{0}] Autosolving...", _moduleID);
        yield return null;
        DisplayPress(displays[1]);
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() => !isAnimating);
        while (!moduleSolved)
        {
            for (int i = 0; i < 20; i++)
            {
                if (solution[i] == '0')
                    WireCut(wires[i]);
            }
            DisplayPress(displays[0]);
            yield return new WaitUntil(() => (!isAnimating || moduleSolved));
        }
    }
    // NOTE TO AXODEAU: If you make Selectaholic v2 or something, this module contains 23 selectables :)
}
