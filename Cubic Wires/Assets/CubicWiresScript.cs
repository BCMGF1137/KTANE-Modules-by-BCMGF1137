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
    public GameObject[] hatches;
    public Material[] wireMaterials;

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
    private string[] shuffledColors;

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

    // Have wires been cut?
    private bool[] isWireCut = new bool[12];

    // End of variable declaration
    void Awake()
    {
        _moduleID = _moduleIDCounter++;
        for (int i = 0; i < 12; i++)
        {
            Transform selectable = wireCube.transform.GetChild(i + 1).GetChild(17);
            selectable.GetComponent<KMSelectable>().OnInteract += delegate ()
            {
                CutWire(selectable.name);
                return false;
            };
        }
    }

    // Use this for initialization
    void Start() {
        // Disable hatches
        foreach (GameObject hatch in hatches)
        {
            hatch.gameObject.SetActive(false);
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
            //wireMaterials[i].color = colors[shuffledColors[i]];
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
        Debug.LogFormat("[Cubic Wires #{0}] In summary, you need to cut the wires in this order: {1}.", _moduleID, answer.Select(x => lettersToColors[x]).Join(", "));

        StartCoroutine(RotateCube());
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

    // Cutting a wire (uh oh)
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
                t2.gameObject.SetActive(false);
            }
        }
    }

    // Twitch Plays handling
    public IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
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
