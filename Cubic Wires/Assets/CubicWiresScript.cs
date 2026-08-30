using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class CubicWiresScript : MonoBehaviour {

    // Initialize variables

    // Module ID
    static int _moduleIDCounter = 1;
    int _moduleID;

    // Stuff we can see in Unity
    // KTANE stuff
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    // Unity stuff
    public GameObject wireCube; // Rotating cube
    public GameObject hatches;

    public TextMesh display;

    // IEnumerators
    private IEnumerator cubeRotations;
    private IEnumerator solveAnimation;

    private KMSelectable[] wireSelectables = new KMSelectable[12];

    // Readonly variables
    // Priority Strings and Rotations
    private static readonly string[] priorityStrings = "ROYLGJCABVMS,BCMGYROVJSAL,MLGRAYJSBCOV,GBLVYMOSRCAJ,ABCGJLMORSVY,CGVBRLSYAJMO,RYGCBMOLJAVS,OCLBGYMVRAJS,YSJBGVCAMLOR,GORYSBLAVCJM,CRAOBYVLMGSJ,MSVAJOGLYCBR,BRSVAMLGOJYC,VOCABLRYMSGJ".Split(',');
    private static readonly string[] rotations = "XY,XZ,YX,YZ,ZX,ZY,+X+Y+Z,+X+Z+Y,-X+Y-Z,-X+Z-Y,+X-Y-Z,-X-Z+Y,+X-Z-Y,-X-Y+Z".Split(',');
    private readonly Dictionary<string, string> rotationsToPriorities = rotations.Select((n, i) => new
    {
        Rotation = n,
        Priority = priorityStrings[i]
    }).ToDictionary(x => x.Rotation, x => x.Priority);

    // Colors
    private static readonly string[] colorNames = "Red Orange Yellow Lime Green Jade Cyan Azure Blue Violet Magenta Rose".Split(' ');
    private readonly Dictionary<string, Color> colors = colorNames.Select((n, i) => new
    {
        Name = n,
        Color = Color.HSVToRGB(i / 12f, 1, 1)
    }).ToDictionary(x => x.Name, x => x.Color);
    private string[] shuffledColors = new string[12];

    // Letter to Color
    private readonly Dictionary<char, string> lettersToColors = priorityStrings[0].Select((l, i) => new
    {
        Letter = l,
        Color = colorNames[i]
    }).ToDictionary(x => x.Letter, x => x.Color);

    // Vertex relations
    private readonly string alphabet = "123456789ABC"; // Represents numbers from 1-12
    private readonly string[] vertexRelations = "159,52A,369,64A,17B,27C,3B8,48C".Split(',');
    private readonly string[] axisRelations = "1234,5678,9ABC".Split(',');

    // Wire Variants
    private int[] wireVariants = new int[24];
    
    // Rotation Sequence
    private string[] rotationSequence = new string[11];

    // Answer
    private string answer = "";
    private List<string> answerList;

    // Have wires been cut?
    private bool[] isWireCut = new bool[12];

    // Solve animation time elapsed
    private float animTime = 0f;

    // Display text
    private string displayText = "12 left";
    private string displayTextSolve = "";

    // TP striking
    private bool TPStruck = false;

    // ==End of variable declaration==
    void Awake()
    {
        _moduleID = _moduleIDCounter++;
        for (int i = 0; i < 12; i++)
        {
            Transform selectable = wireCube.transform.GetChild(i + 1).GetChild(17);
            wireSelectables[i] = selectable.GetComponent<KMSelectable>();
            //int wireIndex = i;
            selectable.GetComponent<KMSelectable>().OnInteract += delegate ()
            {
                CutWire(selectable.name);
                return false;
            };
            selectable.GetComponent<KMSelectable>().OnHighlight += delegate ()
            {
                HLWire(selectable.GetComponent<KMSelectable>());
            };
            selectable.GetComponent<KMSelectable>().OnHighlightEnded += delegate ()
            {
                HLWireEnd();
            };
        }

        cubeRotations = RotateCube();
        solveAnimation = SolveAnim();
    }

    void HLWire(KMSelectable wire)
    {
        int wireIndex = Array.IndexOf(wireSelectables, wire);
        displayText = shuffledColors[wireIndex];
    }

    void HLWireEnd()
    {
        displayText = isWireCut.Count(x => !x) + " left";
    }

    // Use this for initialization
    void Start() {
        // Disable hatches
        for (int i = 0; i < 4; i++)
        {
            hatches.transform.GetChild(i).gameObject.SetActive(false);
        }

        // Wire appearances
        for (int i = 0; i < 24; i++)
        {
            if (i < 12) wireVariants[i] = Rnd.Range(0, 5);
            else wireVariants[i] = Rnd.Range(0, 4);
        }

        // Recolor wires
        shuffledColors = colorNames.Select(x => x).ToArray().Shuffle();
        for (int i = 0; i < 12; i++)
        {
            Transform t1 = wireCube.transform.GetChild(i + 1);

            for (int j = 0; j < t1.childCount; j++)
            {
                Transform t2 = t1.GetChild(j);
                if (t2.name.Contains("Cylinder"))
                {
                    t2.GetComponent<MeshRenderer>().material.color = colors[shuffledColors[i]];
                    t2.gameObject.SetActive(t2.name == ("Cylinder." + (229 + 16 * i + wireVariants[i])));
                }
                else if (t2.name.Contains("Metal"))
                {
                    t2.gameObject.SetActive(false);
                }
            }
        }

        // Determine the rotations
        for (int i = 0; i < 3; i++) rotationSequence[i] = rotations.Skip(6).PickRandom();
        for (int i = 3; i < 11; i++) rotationSequence[i] = rotations.Take(6).PickRandom();

        rotationSequence.Shuffle();
        //rotationSequence = "+X+Y+Z,+X+Z+Y,-X+Y-Z,-X+Z-Y,+X-Y-Z,+X-Z-Y,-X-Y+Z,-X-Z+Y".Split(',');
        Debug.LogFormat("[Cubic Wires #{0}] The rotations are: {1}.", _moduleID, rotationSequence.Join(", "));
        string[] ordinals = "first,second,third,fourth,fifth,sixth,seventh,eighth,ninth,tenth,eleventh,last".Split(',');

        // Determine the answer
        for (int i = 0; i < 11; i++)
        {
            Debug.LogFormat("[Cubic Wires #{0}] -----------------------", _moduleID);
            string priority = rotationsToPriorities[rotationSequence[i]];

            Debug.LogFormat("[Cubic Wires #{0}] The {1} rotation is {2}.", _moduleID, ordinals[i], rotationSequence[i]);
            Debug.LogFormat("[Cubic Wires #{0}] The priority string is {1}.", _moduleID, priority);
            if (rotationSequence[i].Length == 2) // 2-axis rotations
            {
                Debug.LogFormat("[Cubic Wires #{0}] The wires we need to address are {1} and {2}.", _moduleID, 
                    lettersToColors[priority[i]], lettersToColors[priority[i + 1]]
                    );
                char c1 = alphabet[Array.IndexOf(shuffledColors, lettersToColors[priority[i]])];
                char c2 = alphabet[Array.IndexOf(shuffledColors, lettersToColors[priority[i + 1]])];

                // Do the two wires share a vertex?
                if (vertexRelations.Any(x => x.Contains(c1) && x.Contains(c2)))
                {
                    Debug.LogFormat("[Cubic Wires #{0}] {1} and {2} *DO* share a vertex.", _moduleID,
                    lettersToColors[priority[i]], lettersToColors[priority[i + 1]]
                    );

                    answer += priority.First(x => !answer.Contains(x));
                }
                else
                {
                    Debug.LogFormat("[Cubic Wires #{0}] {1} and {2} do *NOT* share a vertex.", _moduleID,
                    lettersToColors[priority[i]], lettersToColors[priority[i + 1]]
                    );

                    answer += priority.SkipWhile(x => answer.Contains(x)).Skip(1).First(x => !answer.Contains(x));
                }
            }
            else // 3-axis rotations
            {
                string filteredPriority = priority.Where(x => !answer.Contains(x)).Join("");
                bool wireFound = false;
                Debug.LogFormat("[Cubic Wires #{0}] The priority string, only considering uncut wires, is {1}.", _moduleID, filteredPriority);
                for (int j = 0; j < filteredPriority.Length - 1; j++)
                {
                    Debug.LogFormat("[Cubic Wires #{0}] The wires we are currently looking at are {1} and {2}.", _moduleID,
                    lettersToColors[filteredPriority[j]], lettersToColors[filteredPriority[j + 1]]
                    );
                    
                    char c1 = alphabet[Array.IndexOf(shuffledColors, lettersToColors[filteredPriority[j]])];
                    char c2 = alphabet[Array.IndexOf(shuffledColors, lettersToColors[filteredPriority[j + 1]])];
                    if (axisRelations.Any(x => x.Contains(c1) && x.Contains(c2)))
                    {
                        Debug.LogFormat("[Cubic Wires #{0}] {1} and {2} *ARE* on the same axis.", _moduleID,
                        lettersToColors[filteredPriority[j]], lettersToColors[filteredPriority[j + 1]]
                        );
                        wireFound = true;
                        answer += filteredPriority[j + 1];
                        break;
                    }
                    else
                    {
                        Debug.LogFormat("[Cubic Wires #{0}] {1} and {2} are *NOT* on the same axis.", _moduleID,
                        lettersToColors[filteredPriority[j]], lettersToColors[filteredPriority[j + 1]]
                        );
                    }
                }
                if (!wireFound)
                {
                    Debug.LogFormat("[Cubic Wires #{0}] No color pairs were on the same axis.", _moduleID);
                    answer += filteredPriority[0];
                }
            }
            Debug.LogFormat("[Cubic Wires #{0}] The {1} wire to cut is {2}.", _moduleID, ordinals[i], lettersToColors[answer.Last()]);
        }

        // Final color
        answer += priorityStrings[0].First(x => !answer.Contains(x));
        Debug.LogFormat("[Cubic Wires #{0}] -----------------------", _moduleID);
        Debug.LogFormat("[Cubic Wires #{0}] The last wire to cut is {1}.", _moduleID, lettersToColors[answer.Last()]);
        Debug.LogFormat("[Cubic Wires #{0}] -----------------------", _moduleID);
        answerList = answer.Select(x => lettersToColors[x]).ToList();
        Debug.LogFormat("[Cubic Wires #{0}] In summary, you need to cut the wires in this order: {1}.", _moduleID, answerList.Join(", "));

        StartCoroutine(cubeRotations);
    }

    // Rotate the cube
    private IEnumerator RotateCube() {
        yield return null;
        int index = Rnd.Range(0, rotationSequence.Length);
        index = 0;
        float duration = Rnd.Range(2.5f,3f);
        yield return new WaitForSeconds(1f);
        while (true)
        {
            float elapsed = 0f;
            
            float angleX = 0;
            float angleY = 0;
            float angleZ = 0;

            float uhh = 69.44f; // I don't know why this is the correct value here

            switch (rotationSequence[index])
            {
                case "XY":
                    angleZ = -90;
                    break;
                case "YX":
                    angleZ = 90;
                    break;
                case "XZ":
                    angleY = -90;
                    break;
                case "ZX":
                    angleY = 90;
                    break;
                case "YZ":
                    angleX = -90;
                    break;
                case "ZY": 
                    angleX = 90; 
                    break;
                case "+X+Y+Z":
                    angleX = uhh;
                    angleY = -uhh;
                    angleZ = uhh;
                    break;
                case "+X+Z+Y":
                    angleX = -uhh;
                    angleY = uhh;
                    angleZ = -uhh;
                    break;
                case "-X+Y-Z":
                    angleX = uhh;
                    angleY = uhh;
                    angleZ = -uhh;
                    break;
                case "-X+Z-Y":
                    angleX = -uhh;
                    angleY = -uhh;
                    angleZ = uhh;
                    break;
                case "+X-Y-Z":
                    angleX = -uhh;
                    angleY = uhh;
                    angleZ = uhh;
                    break;
                case "-X-Z+Y":
                    angleX = uhh;
                    angleY = -uhh;
                    angleZ = -uhh;
                    break;
                case "+X-Z-Y":
                    angleX = uhh;
                    angleY = uhh;
                    angleZ = uhh;
                    break;
                case "-X-Y+Z":
                    angleX = -uhh;
                    angleY = -uhh;
                    angleZ = -uhh;
                    break;
                /*
                    */
                default: break;
            }

            //Debug.LogFormat("[Cubic Wires #{0}] Currently rotating: {1}.", _moduleID, rotationSequence[index]);

            while (elapsed < duration)
            {
                float t1 = (1 - Mathf.Cos(Mathf.PI * (elapsed / duration))) / 2.0f;
                float t2 = (1 - Mathf.Cos(Mathf.PI * ((elapsed + Time.deltaTime) / duration))) / 2.0f;
                wireCube.transform.localRotation = Quaternion.Euler((t2 - t1) * angleX, (t2 - t1) * angleY, (t2 - t1) * angleZ) * wireCube.transform.localRotation;
                /*
                wireCube.transform.localRotation = new Quaternion(
                    Mathf.Lerp(x1, x2, (1 - Mathf.Cos(Mathf.PI * (elapsed / duration))) / 2.0f),
                    Mathf.Lerp(y1, y2, (1 - Mathf.Cos(Mathf.PI * (elapsed / duration))) / 2.0f), 
                    Mathf.Lerp(z1, z2, (1 - Mathf.Cos(Mathf.PI * (elapsed / duration))) / 2.0f)
                );
                */
                //Debug.LogFormat("[Cubic Wires #{0}] {1}", _moduleID, wireCube.transform.localRotation.x);
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            /*
            Debug.LogFormat("[Cubic Wires #{0}] {1} {2} {3}", _moduleID, 
                wireCube.transform.localEulerAngles.x,
                wireCube.transform.localEulerAngles.y,
                wireCube.transform.localEulerAngles.z
                );
                */
            wireCube.transform.localEulerAngles = new Vector3(
                Mathf.Round(wireCube.transform.localEulerAngles.x / 90) * 90, 
                Mathf.Round(wireCube.transform.localEulerAngles.y / 90) * 90, 
                Mathf.Round(wireCube.transform.localEulerAngles.z / 90) * 90
                );
            yield return new WaitForSeconds(0.1f);
            index = (index + 1) % rotationSequence.Length;
            //break;
            if (index == 0) yield return new WaitForSeconds(duration / 1.5f);
        }
    }

    // Cutting a wire
	private void CutWire(string name)
    {
        int index = int.Parse(name.Substring(11,2)) - 13;
        if (isWireCut[index]) return; // Abort the method if the wire is already cut.
        Transform t1 = wireCube.transform.GetChild(index + 1);
        for (int i = 0; i < t1.childCount; i++)
        {
            Transform t2 = t1.GetChild(i);
            string[] validNames =
            {
                "Cylinder." + (225 + 16 * index + wireVariants[index + 12]),
                "Metal." + (309 + 22 * index + 2 * wireVariants[index + 12]),
                "Metal." + (310 + 22 * index + 2 * wireVariants[index + 12]),
            };
            
            if (validNames.Contains(t2.name))
            {
                t2.gameObject.SetActive(true);
            }
            else
            {
                if (t2.name.Contains("(1)"))
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, t2.transform);
                    t2.GetComponent<KMSelectable>().AddInteractionPunch(0.1f);
                }
                t2.gameObject.SetActive(false);
            }
        }

        isWireCut[index] = true;

        Debug.LogFormat("[Cubic Wires #{0}] You cut the {1} wire. {2}", _moduleID, shuffledColors[index],
            shuffledColors[index] == answerList[0] ? (answerList.Count != 1 ? "Keep going..." : "Module solved!") : "That was incorrect. Strike!");
        if (shuffledColors[index] != answerList[0])
        {
            Module.HandleStrike();
        }
        answerList.Remove(shuffledColors[index]);

        //if (answerList.Count == 11) // Playtesting
        if (answerList.Count == 0)
        {
            StopCoroutine(cubeRotations);
            StartCoroutine(solveAnimation);
        }
    }

    // BPM 110
    private IEnumerator SolveAnim()
    {
        yield return null;
        Audio.PlaySoundAtTransform("Solve", transform);

        StartCoroutine(SolveAnimWires());
        StartCoroutine(SolveAnimCube());
        StartCoroutine(SolveAnimVertices());
        StartCoroutine(SolveAnimHatches());
        StartCoroutine(SolveAnimText());

        while (animTime < 8.727f)
        {
            yield return null;
            animTime += Time.deltaTime;
        }

        Module.HandlePass();
    }
    
    // Retract the wires during the solve animation
    private IEnumerator SolveAnimWires()
    {
        int[] initialStages = new int[12];
        for (int i = 0; i < 12; i++)
        {
            initialStages[i] = -1 - i * 4;
        }
        initialStages.Shuffle();
        int[] retractStages = initialStages.Select(x => x).ToArray();
        int[,] randomNums = new int[12,3];
        for (int i = 0; i < 12; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                randomNums[0, 0] = Rnd.Range(0, 2);
            }
        }
        yield return new WaitUntil(() => animTime >= 0.545f);
        while (animTime < 8.727f)
        {
            float currentTime = animTime - 0.545f;
            float uhh2 = 0.0681818182f;
            //uhh2 = 1f;
            retractStages = initialStages.Select(x => (int)(x + Mathf.Floor(currentTime / uhh2) + 0.001f)).ToArray();
            for (int i = 0; i < 12; i++)
            {
                if (retractStages[i] >= 0)
                {
                    Transform t1 = wireCube.transform.GetChild(i + 1);
                    for (int j = 0; j < t1.childCount; j++)
                    {
                        Transform t2 = t1.GetChild(j);
                        if (retractStages[i] >= 4)
                        {
                            t2.gameObject.SetActive(false);
                        }
                        else
                        {
                            int randomNum = retractStages[i] == 3 ? 0 : randomNums[i, retractStages[i]];
                            string[] validNames =
                            {
                                "Cylinder." + (240 + 16 * i - randomNum - 2 * retractStages[i]),
                                "Metal." + (329 + 22 * i - 2 * randomNum - 4 * retractStages[i]),
                                "Metal." + (330 + 22 * i - 2 * randomNum - 4 * retractStages[i]),
                            };
                            t2.gameObject.SetActive(validNames.Contains(t2.name));
                        }
                    }
                }
            }
            yield return null;
        }
    }

    // Shrink the cube during the solve animation
    /*
      X = -36
      Y = 0
      Z = -45
    */
    private IEnumerator SolveAnimCube()
    {
        wireCube.transform.localEulerAngles = new Vector3(
            ((wireCube.transform.localEulerAngles.x % 360) + 360) % 360, 
            ((wireCube.transform.localEulerAngles.y % 360) + 360) % 360, 
            ((wireCube.transform.localEulerAngles.z % 360) + 360) % 360
        );
        float[] angles = {
            wireCube.transform.localEulerAngles.x,
            wireCube.transform.localEulerAngles.y,
            wireCube.transform.localEulerAngles.z
        };
        while (animTime < 8.727f)
        {
            int[] targetConsts;
            if (animTime < 2.1818182f)
            {
                targetConsts = new int[] { 180, 180, 180 };
            }
            else if (animTime < 4.3636364f)
            {
                targetConsts = new int[] { 144, 180, 45 };
            }
            else if (animTime < 6.54545454f)
            {
                targetConsts = new int[] { 90, 45, 0 };
            }
            else
            {
                targetConsts = new int[] { 0, 0, 0 };
            }
            angles = angles.Select((x, i) => (x - targetConsts[i]) / Mathf.Pow(12, Time.deltaTime) + targetConsts[i]).ToArray();
            wireCube.transform.localEulerAngles = new Vector3(angles[0], angles[1], angles[2]);

            // 30 -> -5
            if (animTime >= 6.54545454f && animTime < 8.18181818182f)
            {
                wireCube.transform.localPosition = new Vector3(0, -24.4444443629f * (animTime - 6.54545454f) + 36, 0);
            }
            else if (animTime >= 8.18181818182f)
            {
                wireCube.transform.localPosition = new Vector3(0, -5, 0);
            }
            yield return null;
        }
    }

    private IEnumerator SolveAnimVertices()
    {
        yield return null;
        yield return new WaitUntil(() => animTime >= 4.3636364f);

        // #14: +X, -Y, +Z
        Transform t1 = wireCube.transform.GetChild(0);
        float[,] initialPositions = new float[8,3];

        for (int i = 0; i < 8; i++)
        {
            initialPositions[i, 0] = t1.GetChild(i).localPosition.x;
            initialPositions[i, 1] = t1.GetChild(i).localPosition.y;
            initialPositions[i, 2] = t1.GetChild(i).localPosition.z;
        }

        while (animTime <= 6.545454545f)
        {
            float modifier = animTime - 4.3636364f;
            modifier *= Mathf.PI / 0.545454545f;
            modifier += Mathf.Abs(Mathf.Sin(modifier));
            modifier *= 0.085f / 4 / Mathf.PI;

            for (int i = 0; i < 8; i++)
            {
                Transform t2 = t1.GetChild(i);
                string[] operators = "+-+,+--,---,--+,-++,-+-,++-,+++".Split(',');
                if (operators[i].Length == 3)
                {
                    int[] mults = operators[i].Select(x => x == '+' ? 1 : -1).ToArray();
                    t2.localPosition = new Vector3(
                        initialPositions[i, 0] + mults[0] * modifier,
                        initialPositions[i, 1] + mults[1] * modifier,
                        initialPositions[i, 2] + mults[2] * modifier
                    );
                }
            }
            yield return null;
        }
    }

    private IEnumerator SolveAnimHatches()
    {
        yield return null;
        yield return new WaitUntil(() => animTime >= 7.5f);
        for (int i = 0; i < 4; i++)
        {
            hatches.transform.GetChild(i).gameObject.SetActive(true);
        }
        while (animTime < 7.636125f)
        {
            hatches.transform.localPosition = new Vector3(-20, (40 / 0.136125f * (animTime - 7.5f)) - 60, -18);
            yield return null;
        }
        hatches.transform.localPosition = new Vector3(-20, -20, -18);
        yield return new WaitUntil(() => animTime >= 8.1815625f);
        for (int i = 0; i < 4; i++)
        {
            Transform t1 = hatches.transform.GetChild(i).transform;

            while (animTime < 8.1815625f + 0.136359375f * (i + 1))
            {
                //Debug.LogFormat("[Cubic Wires #{0}] x: {1} \ny: {2}", _moduleID, animTime, 660.020625645f * (animTime - 8.1815625f - 0.136359375f * i) - 180);
                t1.localEulerAngles = new Vector3(
                    660.020625645f * (animTime - 8.1815625f - 0.136359375f * i) - 180,
                    t1.localEulerAngles.y, t1.localEulerAngles.z
                    );
                yield return null;
            }
            t1.localEulerAngles = new Vector3(-90, t1.localEulerAngles.y, t1.localEulerAngles.z);
        }
    }

    private IEnumerator SolveAnimText()
    {
        yield return null;
        string[] preSolveTexts =
        {
            "",
            "Made by",
            "BCMGF1137",
            "/19#5398",
            "Song Used:",
            "\"Hypercubes\"",
            "by Ryzmik",
            "",
        };
        string[] solveTexts =
        {
            "Cubic Wires",
            "0 left",
            "Breathe.",
            "Done! :)",
            "+20 points",
            "Not so easy.",
            "Solved!",
            "Disarmed!",
        };
        while (animTime < 8.727f)
        {
            displayTextSolve = preSolveTexts[(int)(animTime / 1.090875f)];
            yield return null;
        }
        displayTextSolve = solveTexts.PickRandom();
    }

    void Update()
    {
        if (isWireCut.Count(x => x) == 12)
        {
            display.text = displayTextSolve;
        }
        else
        {
            display.text = displayText;
        }
    }

    // Twitch Plays handling
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cut Blue Cyan Magenta Green [Cut the Blue, Cyan, Magenta, and Green wires in that order. No limit to how many wires can be cut. Chain with spaces.]"
    + @"!{0} highlight Red [Flash the Red wire in white and black for 2 seconds. You only get one wire.]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        string[] parameters = command.Split(' ');

        if (parameters.Length == 0)
        {
            yield return "sendtochaterror That command doesn't exist!";
        }
        else if (parameters[0] == "cut")
        {
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror A wire color wasn't given. What wire do you need to cut?";
                yield break;
            }
            else
            {
                parameters = parameters.Skip(1).ToArray();
                foreach (string p in parameters) // Prevent non-colors from being given
                {
                    if (!colorNames.Select(x => x.ToLowerInvariant()).Contains(p))
                    {
                        yield return "sendtochaterror The string \"" + p + "\" is not a color!";
                        yield break;
                    }
                }
                if (parameters.Distinct().Count() != parameters.Length) // Prevent duplicate colors from being given
                {
                    yield return "sendtochaterror The color \"" + parameters.First(x => parameters.Count(y => x == y) > 1) + "\" appears more than once. Please provide only distinct colors!";
                    yield break;
                }
                //yield return "sendtochat " + answerList.Select(y => y.ToLowerInvariant()).Join(" ");
                if (!parameters.All(x => answerList.Select(y => y.ToLowerInvariant()).Contains(x))) // Prevent already-cut colors from being given
                {
                    yield return "sendtochaterror The color \"" + parameters.First(x => !answerList.Select(y => y.ToLowerInvariant()).Contains(x)) + "\" has already been cut!";
                    yield break;
                }
                // By this point, we know all colors are valid, we now just need to convert them into indices and cut their wires

                string[] lowercaseColors = shuffledColors.Select(x => x.ToLowerInvariant()).ToArray();
                int[] indices = parameters.Select(x => Array.IndexOf(lowercaseColors, x)).ToArray();

                foreach (int index in indices)
                {
                    TPStruck = false;

                    Transform selectable = wireCube.transform.GetChild(index + 1).GetChild(17);
                    selectable.GetComponent<KMSelectable>().OnInteract();
                    if (TPStruck) yield break;
                    if (answerList.Count == 0) yield return "solve";

                    yield return new WaitForSeconds(0.5454545f);
                }
            }
        }
        else if (parameters[0] == "highlight")
        {
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror A wire color wasn't given. What wire do you need to highlight?";
            }
            else if (parameters.Length > 2)
            {
                yield return "sendtochaterror You can't highlight more than one wire! Please specify a color to highlight.";
            }
            else if (!colorNames.Select(x => x.ToLowerInvariant()).Contains(parameters[1]))
            {
                yield return "sendtochaterror The string \"" + parameters[1] + "\" is not a color!";
            }
            else
            {
                // Find the index of the given color
                string[] lowercaseColors = shuffledColors.Select(x => x.ToLowerInvariant()).ToArray();
                int index = Array.IndexOf(lowercaseColors, parameters[1]);

                Transform t1 = wireCube.transform.GetChild(index + 1);

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < t1.childCount; j++)
                    {
                        Transform t2 = t1.GetChild(j);
                        if (t2.name.Contains("Cylinder"))
                        {
                            t2.GetComponent<MeshRenderer>().material.color = Color.HSVToRGB(0, 0, 1);
                        }
                    }

                    yield return new WaitForSeconds(0.1f);

                    for (int j = 0; j < t1.childCount; j++)
                    {
                        Transform t2 = t1.GetChild(j);
                        if (t2.name.Contains("Cylinder"))
                        {
                            t2.GetComponent<MeshRenderer>().material.color = Color.HSVToRGB(0, 0, 0);
                        }
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                for (int j = 0; j < t1.childCount; j++)
                {
                    Transform t2 = t1.GetChild(j);
                    if (t2.name.Contains("Cylinder"))
                    {
                        t2.GetComponent<MeshRenderer>().material.color = colors[shuffledColors[index]];
                    }
                }
            }
        }
        else
        {
            yield return "sendtochaterror That command doesn't exist!";
        }

        // This was just extra code to test rotations
        /*
        float[] nums = command.Split(' ').Select(x => float.Parse(x)).ToArray();
        if (nums.Length == 3)
        {
            wireCube.transform.localEulerAngles = new Vector3(0, 0, 0);
            float duration = Random.Range(1.5f, 2f);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t1 = (1 - Mathf.Cos(Mathf.PI * (elapsed / duration))) / 2.0f;
                float t2 = (1 - Mathf.Cos(Mathf.PI * ((elapsed + Time.deltaTime) / duration))) / 2.0f;
                wireCube.transform.localRotation = Quaternion.Euler((t2 - t1) * nums[0], (t2 - t1) * nums[1], (t2 - t1) * nums[2]) * wireCube.transform.localRotation;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else yield break;
        */
    }
}
