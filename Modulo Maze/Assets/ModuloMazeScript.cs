using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class ModuloMazeScript : MonoBehaviour {

    static int _moduleIDCounter = 1;
    int _moduleID;

    // Public variables
    
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public MeshRenderer[] lightMaterials;
    public MeshRenderer[] darkMaterials;
    public TextMesh[] displays;
    public KMSelectable[] arrows;

    // Private variables

    private int[,,] numbers =
    {
        {
            {123456789,536870912,694201337,107073645,300607630,387420489,999999937,314159265,},
            {575360377,693147180,107089272,249671583,668697120,110121132,765876987,723518202,},
            {131175320,598421052,442534320,712152325,696969696,135792468,100000000,741852963,},
            {142857142,742185396,610983610,112358132,301029995,587389241,338767493,137032963,},
            {570901557,316645697,700025764,634365634,248163264,927138244,122537500,918273645,},
            {369258147,500000000,116612994,856714178,770679848,610185421,707106781,835449785,},
            {543999678,439286205,179769313,999999999,123165144,268435455,100101102,999998997,},
            {433494437,599074578,359382420,479001600,999950884,316227766,165580141,987654321,},
        },
        {
            {214748364,742140251,627876556,130133763,438252165,204118008,560165106,524567116,},
            {182157108,585759291,111111111,747653702,384088425,258324263,123581321,608998907,},
            {123454321,742085809,549382716,122522400,510152025,436843313,309160393,662343750,},
            {132468750,392781243,461123993,110998910,288657941,668716161,538155684,205729257,},
            {451931892,816645697,714212835,309431985,904493600,128899892,255075100,169343516,},
            {544332211,218122150,548696840,816243240,465825544,988211769,149162536,612182430,},
            {887641867,698970004,429615927,743030421,176457270,858764812,496481100,333666999,},
            {365634365,751836735,123498765,135798642,124789653,146897532,257914190,138532110,},
        },
        {
            {696759006,905697844,230311103,649728076,834729156,769693318,864208642,420420420,},
            {966771429,102563898,989940351,618094567,248015873,611024109,829220721,135791357,},
            {729784841,994924126,334525266,728382715,371960709,454756609,284753857,496130341,},
            {525252525,371502244,400441484,339169357,685134170,444889332,235711131,948584405,},
            {146891012,152399025,539853990,265573747,101643241,769230769,701388689,135791113,},
            {215346978,847858478,383827090,941685896,225256289,402615083,345676543,365636671,},
            {659861091,291129149,435849042,176366828,288804781,152182067,686193202,172195665,},
            {721956656,142857143,193185113,187231325,561439334,114715126,102470729,127512000,},
        },
        {
            {568795307,659258263,154091885,174261807,400939866,707242517,188082729,101010101,},
            {599131575,953502316,281476625,277471328,488281250,313538439,538538101,587399824,},
            {303603054,570069270,112370464,215414082,711554733,871557427,371816138,216795867,},
            {652653654,885534336,482569096,601815023,156561154,202502020,315229419,172351820,},
            {581624697,976913950,259319679,121110987,563354690,695044568,121229150,637971511,},
            {646471609,575222039,118117116,725741562,886792452,400160060,101001000,572899616,},
            {690204512,493553415,174524064,382725000,100349676,474785954,502341056,890123456,},
            {746268656,114300523,252110142,212019801,751325007,132934038,122333444,926052826,},
        },
        {
            {553635370,761819572,576789200,235921564,555262893,861068738,410583593,992510034,},
            {583279850,194352396,294927593,534968177,301740931,123569042,345823199,838759384,},
            {513694319,415823058,760235931,103591928,769010253,776139513,943258238,265937349,},
            {683919304,667105934,593103589,286078592,468106264,305832693,908573238,235623891,},
            {193848599,301953843,104769347,753929482,564353103,312642543,602395199,888156328,},
            {113151345,583295011,252590210,102358468,789089130,967195312,810363252,954836092,},
            {432980415,891205250,573057439,438583492,392019681,486178092,683112043,178539510,},
            {348393920,615395288,464341909,184525130,342951108,915186083,603583429,328562831,},
        },
        {
            {161109900,252119101,343128302,434137503,525146704,616155905,797165106,888174307,},
            {951289827,142299028,233208229,324217430,415226631,596235832,687245033,778254208,},
            {841369726,932378947,123388148,214397349,395306550,486315751,577324934,668334109,},
            {731449625,822458846,913468059,194477260,285486461,376495652,467404835,558414010,},
            {621529524,712538745,893547958,984557163,175566362,266575553,357584736,448593911,},
            {511609423,692618644,783627857,874637056,965646255,156655454,247664637,338673812,},
            {491789322,582798543,673707742,764716941,855726140,946735339,137744538,228753713,},
            {381869221,472878420,563887619,654896818,745806017,836815216,927824415,118833614,},
        },
        {
            {572326583,295619290,133164163,493651969,247682041,107188277,374097532,177323244,},
            {708137692,380847010,181203515,727555916,393258626,188370018,763636753,416457152,},
            {201869031,832332195,461090578,228198571,968868776,551445203,282784024,126127161,},
            {460717441,227976603,967704500,550666181,282306691,125866568,459505223,227255700,},
            {963924681,548138056,280758381,125021847,455579495,224923277,951711094,539979182,},
            {275769590,122305916,442996429,217470148,912846639,514122388,260041659,113803394,},
            {404000104,194603848,795244053,436919775,213883596,894235017,501798118,252590542,},
            {109808189,385892954,184112251,742166833,402631787,193808140,791198333,434293451,},
        }
    };
    private string[,,] mazes =
    {
        { // Maze 0
            {
                "R","LD","R","LDR","LD","R","LR","LD"
            },
            {
                "RD","ULR","LR","ULD","UR","DL","DR","ULD"
            },
            {
                "UD","D","D","UR","L","UR","LU","UD"
            },
            {
                "UR","LU","UR","LDR","LD","D","R","ULD"
            },
            {
                "DR","L","RD","UL","UD","URD","LR","ULD"
            },
            {
                "URD","LR","LU","D","U","U","D","UD"
            },
            {
                "UR","LDR","LR","URDL","LR","LDR","LU","UD"
            },
            {
                "R","LUR","L","U","R","LUR","LR","UL"
            },
        },
        { // Maze 1
            {
                "RD","LR","LR","LD","DR","LR","LDR","L"
            },
            {
                "UR","LDR","L","UR","LUR","L","URD","LD"
            },
            {
                "D","U","DR","LDR","L","D","U","UD"
            },
            {
                "UR","LDR","LU","UD","DR","LUR","LD","UD"
            },
            {
                "D","U","D","UD","DU","DR","LURD","LU"
            },
            {
                "UR","LDR","LU","UR","LU","UD","UR","LD"
            },
            {
                "DR","LUR","LD","DR","LD","U","D","UD"
            },
            {
                "U","R","LUR","LU","UR","LR","LUR","LU"
            },
        },
        { // Maze 2
            {
                "DR","LD","D","DR","LR","LR","LD","D"
            },
            {
                "UD","UR","LUR","LUD","DR","LR","UL","UD"
            },
            {
                "UD","DR","LD","U","UR","LR","LD","UD"
            },
            {
                "UD","U","UD","R","LR","LD","UR","LU"
            },
            {
                "URD","LR","UDLR","LR","ULD","UR","LR","LD"
            },
            {
                "UD","D","UD","D","UR","LD","D","UD"
            },
            {
                "UD","UD","UD","UR","LR","LU","URD","LU"
            },
            {
                "UR","LU","UR","LR","L","R","ULR","L"
            },
        },
        { // Maze 3
            {
                "D","DR","LDR","LD","DR","LD","DR","LD"
            },
            {
                "UD","UD","UD","UD","UD","U","UD","U"
            },
            {
                "URD","LU","U","UD","URD","LR","LUD","D"
            },
            {
                "UR","L","D","UD","UD","D","UR","LU"
            },
            {
                "DR","L","URD","LURD","LUD","UR","LDR","LD"
            },
            {
                "URD","LR","LU","U","UR","LD","U","UD"
            },
            {
                "UR","LR","LR","LD","DR","LU","D","UD"
            },
            {
                "R","LR","LR","UL","UR","LR","LUR","LU"
            },
        },
        { // Maze 4
            {
                "D","DR","LR","LR","LD","DR","LR","L"
            },
            {
                "UR","ULD","R","LD","UD","UR","LR","LD"
            },
            {
                "R","LUR","L","UD","URD","LR","LR","ULD"
            },
            {
                "DR","LR","LDR","LU","UD","DR","L","UD"
            },
            {
                "UDR","LD","U","D","UD","UR","LDR","LU"
            },
            {
                "UD","UR","LD","UR","UDLR","L","UR","LD"
            },
            {
                "UR","LD","UR","LD","UD","R","LR","LU"
            },
            {
                "R","LUR","L","UR","LUR","LR","LR","L"
            },
        },
        { // Maze 5
            {
                "DR","LR","LR","LR","LR","LR","LR","LD"
            },
            {
                "UR","LR","LR","LR","LR","LD","D","UD"
            },
            {
                "DR","LR","LR","LR","LD","UD","UD","UD"
            },
            {
                "UD","DR","LR","L","UR","ULD","UD","UD"
            },
            {
                "UD","UR","LR","LR","LD","U","UD","UD"
            },
            {
                "RU","LD","DR","LD","URD","LD","UD","UD"
            },
            {
                "DR","ULDR","ULD","U","UD","UD","UD","UD"
            },
            {
                "U","U","UR","LR","LU","UR","LU","U"
            },
        },
        { // Maze 6
            {
                "DR","LR","LD","DR","LR","LR","LR","LD"
            },
            {
                "UR","LD","UR","LUR","LD","DR","LD","UD"
            },
            {
                "D","UR","L","D","UD","UD","UDR","LU"
            },
            {
                "UR","LR","LR","LURD","LU","U","UR","L"
            },
            {
                "R","LR","LD","UR","LR","LDR","LR","LD"
            },
            {
                "D","DR","LU","DR","LDR","LU","R","ULD"
            },
            {
                "UD","UR","LDR","LU","URD","LDR","L","UD"
            },
            {
                "UR","LR","LU","R","LU","U","R","LU"
            },
        },
    };

    private double R; // Colors. This takes a while to implement *and* is purely for the aesthetic.
    private double G;
    private double B;
    private double currentR;
    private double currentG;
    private double currentB;

    private int number;
    private int displayedNumber = 0;

    private int maze;
    private int position;
    private int end;

    private string solveText = "";

    private bool moduleSolved = false;
    private bool moduleActivated = false;
    private bool isInteractable = true;

    void Awake ()
    {
        _moduleID = _moduleIDCounter++;
        float H = Rnd.Range(0f, 1f);
        float S = Rnd.Range(0.5f, 0.8f);
        float V = Rnd.Range(0.8f, 1f);

        // Determining colors

        foreach (MeshRenderer m in lightMaterials)
        {
            m.material.color = Color.HSVToRGB(H, S, V);
        }
        foreach (MeshRenderer m in darkMaterials)
        {
            m.material.color = Color.HSVToRGB(H, S, V / 3);
        }
        foreach (TextMesh t in displays)
        {
            t.color = Color.HSVToRGB(H, S, V);
        }

        R = (Color.HSVToRGB(H, S, V).r * 255);
        G = (Color.HSVToRGB(H, S, V).g * 255);
        B = (Color.HSVToRGB(H, S, V).b * 255);
        currentR = (Color.HSVToRGB(H, S, V).r * 255);
        currentG = (Color.HSVToRGB(H, S, V).g * 255);
        currentB = (Color.HSVToRGB(H, S, V).b * 255);

        // Arrow handling

        foreach (KMSelectable arrow in arrows)
        {
            arrow.OnInteract += delegate ()
            {
                ArrowPress(arrow);
                return false;
            };
        }

        Module.OnActivate += delegate{
            moduleActivated = true;
        };
    }

    // Use this for initialization
    void Start () {
        Generate();
    }

    void Generate ()
    {
        number = Rnd.Range(999999999, int.MaxValue) + 1; // More specifically 2147483647 (I just know the number off the top of my head)

        Debug.LogFormat("[Modulo Maze #{0}] Your initial number is {1}.", _moduleID, number);
        Debug.LogFormat("[Modulo Maze #{0}] The sum of the digits in the serial number: {1}.", _moduleID, getDigitWork());
        Debug.LogFormat("[Modulo Maze #{0}] Subtracting the sum of the digits in the serial number: {1} - {2} = {3}.", _moduleID, number, Bomb.GetSerialNumberNumbers().Sum(), number - Bomb.GetSerialNumberNumbers().Sum());
        maze = (number - Bomb.GetSerialNumberNumbers().Sum()) % 7;
        Debug.LogFormat("[Modulo Maze #{0}] Taking modulo 7, to retrieve the maze: {1} % 7 = {2}.", _moduleID, number - Bomb.GetSerialNumberNumbers().Sum(), maze);
        Debug.LogFormat("[Modulo Maze #{0}] The sum of the letters in the serial number: {1}.", _moduleID, getLetterWork(true));
        Debug.LogFormat("[Modulo Maze #{0}] Subtracting the sum of the letters in the serial number: {1} - {2} = {3}.", _moduleID, number, int.Parse(getLetterWork(false)), number - int.Parse(getLetterWork(false)));
        position = (number - int.Parse(getLetterWork(false))) % 64;
        Debug.LogFormat("[Modulo Maze #{0}] Taking modulo 64, to retrieve the starting position: {1} % 64 = {2}.", _moduleID, number - int.Parse(getLetterWork(false)), position);

        Debug.LogFormat("[Modulo Maze #{0}] You are at " + convertPosition(position) + " in Maze {1}.", _moduleID, maze);

        Debug.LogFormat("[Modulo Maze #{0}] Calculation for ending position:", _moduleID);
        int exampleNum = number;
        int examplePos = position;

        char input1 = ' ';
        char input2 = ' ';

        string solution = "";

        while (exampleNum > 0)
        {
            int ix1 = maze;
            int ix2 = examplePos / 8;
            int ix3 = examplePos % 8;
            int ix4 = mazes[ix1, ix2, ix3].Length;
            
            input1 = input2;
            input2 = mazes[ix1, ix2, ix3][Rnd.Range(0, ix4)];

            for (int i = 0; i < 3; i++)
            {
                if (input1 == "URDL"[("URDL".IndexOf(input2) + 2) % 4]) // Attempt to avoid undoing directions
                {
                    input2 = mazes[ix1, ix2, ix3][Rnd.Range(0, ix4)];
                }
            }

            //Debug.LogFormat("[Modulo Maze #{0}] Attempting to go " + input2 + " from {1} {2} {3}.", _moduleID, ix1, ix2, ix3);

            switch (input2)
            {
                case 'U':
                    examplePos -= 8;
                    Debug.LogFormat("[Modulo Maze #{0}] Up to {1}: " + getModuloWork(exampleNum, numbers[maze, examplePos / 8, examplePos % 8], true), _moduleID, "" + convertPosition(examplePos));
                    break;
                case 'D':
                    examplePos += 8;
                    Debug.LogFormat("[Modulo Maze #{0}] Down to {1}: " + getModuloWork(exampleNum, numbers[maze, examplePos / 8, examplePos % 8], true), _moduleID, "" + convertPosition(examplePos));
                    break;
                case 'L':
                    examplePos--;
                    Debug.LogFormat("[Modulo Maze #{0}] Left to {1}: " + getModuloWork(exampleNum, numbers[maze, examplePos / 8, examplePos % 8], true), _moduleID, "" + convertPosition(examplePos));
                    break;
                case 'R':
                    examplePos++;
                    Debug.LogFormat("[Modulo Maze #{0}] Right to {1}: " + getModuloWork(exampleNum, numbers[maze, examplePos / 8, examplePos % 8], true), _moduleID, "" + convertPosition(examplePos));
                    break;
            }
            solution += input2;
            exampleNum = int.Parse(getModuloWork(exampleNum, numbers[maze, examplePos / 8, examplePos % 8], false));
        }

        end = examplePos;

        Debug.LogFormat("[Modulo Maze #{0}] The goal position is placed at " + convertPosition(end) + ".", _moduleID);
        Debug.LogFormat("[Modulo Maze #{0}] In summary, you are in Maze {1} and need to move from " + convertPosition(position) + " to " + convertPosition(end) + " with starting number " + number + ".", _moduleID, maze);
        Debug.LogFormat("[Modulo Maze #{0}] One possible solution: " + solution, _moduleID);
        StartCoroutine(goalDisplay());
    }

    // Extra methods for calculating stuff

    private string convertPosition(int p) // Converts the position from a number to X#
    {
        return "" + "ABCDEFGH"[p % 8] + (p / 8 + 1);
    }

    private string getModuloWork(int x, int y, bool b) // If b is true, return the string. Otherwise, return the int.
    {
        if (x > y)
        {
            return b ? x + " % " + y + " = " + (x % y) : (x % y).ToString();
        }
        else
        {
            return b ? y + " % " + x + " = " + (y % x) : (y % x).ToString();
        }
    }

    private string getLetterWork(bool b) // If b is true, return the string. Otherwise, return the int.
    {
        string x = "";
        int y = 0;
        for(int i = 0; i < 6; i++)
        {
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[i]) != -1)
            {
                x += Bomb.GetSerialNumber()[i] + "(" + ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[i]) + 1) + ") + ";
                y += "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumber()[i]) + 1;
            }
        }
        x = x.Substring(0, x.Length - 2) + "= " + y;
        return b? x:y.ToString();
    }

    private string getDigitWork()
    {
        string x = "";
        int y = 0;
        for (int i = 0; i < 6; i++)
        {
            if ("0123456789".IndexOf(Bomb.GetSerialNumber()[i]) != -1)
            {
                x += Bomb.GetSerialNumber()[i] + " + ";
                y += "0123456789".IndexOf(Bomb.GetSerialNumber()[i]);
            }
        }
        x = x.Substring(0, x.Length - 2) + "= " + y;
        return x;
    }

    private IEnumerator goalDisplay()
    {
        while (displays[1].text.Length > 0)
        {
            yield return new WaitForSeconds(0.1f);
            displays[1].text = displays[1].text.Substring(0, displays[1].text.Length - 1);
        }
        yield return new WaitForSeconds(1f);
        displays[1].text += "ABCDEFGH"[end % 8];
        yield return new WaitForSeconds(0.1f);
        displays[1].text += end / 8 + 1;
    }

    private void ArrowPress(KMSelectable arrow)
    {
        StartCoroutine(ActualArrowPress(arrow));
    }

    private IEnumerator ActualArrowPress(KMSelectable arrow) // Arrow presses
    {
        string[] directions = {"up","left","down","right"};
        int x = Array.IndexOf(arrows, arrow);

        arrow.AddInteractionPunch(0.1f);
        
        if (isInteractable)
        {
            if (!(mazes[maze, position / 8, position % 8]).Contains("ULDR"[x])) // Strike if you move into a wall
            {
                Audio.PlaySoundAtTransform("blackHole", transform);

                currentR = 128;
                currentG = 128;
                currentB = 128;

                isInteractable = false;
                number = 0;
                yield return new WaitUntil(() => displayedNumber == 0);
                yield return new WaitForSeconds(0.7f);
                Debug.LogFormat("[Modulo Maze #{0}] You attempted to move " + directions[x] + " from " + convertPosition(position) + ". A wall was hit. Strike! Regenerating module...", _moduleID);
                Module.HandleStrike();
                Generate();
                isInteractable = true;


                currentR = 255;
                currentG = 0;
                currentB = 0;
            }
            else
            {
                Audio.PlaySoundAtTransform("soundMove" + Rnd.Range(1, 8), transform);

                currentR = 255;
                currentG = 255;
                currentB = 255;

                int formerPos = position;
                switch ("ULDR"[Array.IndexOf(arrows, arrow)])
                {
                    case 'U':
                        position -= 8;
                        break;
                    case 'L':
                        position -= 1;
                        break;
                    case 'D':
                        position += 8;
                        break;
                    case 'R':
                        position += 1;
                        break;
                }

                string result = getModuloWork(number, numbers[maze, position / 8, position % 8], true);
                number = int.Parse(getModuloWork(number, numbers[maze, position / 8, position % 8], false));
                Debug.LogFormat("[Modulo Maze #{0}] You moved " + directions[x] + " from " + convertPosition(formerPos) + ". You are now at " + convertPosition(position) 
                    + ". Your number is now " + result + ".", _moduleID);
                if (number == 0)
                {
                    isInteractable = false;
                    yield return new WaitUntil(() => displayedNumber == 0);
                    yield return new WaitForSeconds(0.7f);
                    if (position != end)
                    {
                        Debug.LogFormat("[Modulo Maze #{0}] Your number reached 0. However, you are not at the goal position of {1}. Strike! Regenerating module...", _moduleID, convertPosition(end));
                        Module.HandleStrike();
                        Generate();
                        isInteractable = true;

                        currentR = 255;
                        currentG = 0;
                        currentB = 0;
                    }
                    else
                    {
                        Debug.LogFormat("[Modulo Maze #{0}] Your number reached 0, and you are at the goal position. Module solved!", _moduleID);
                        Module.HandlePass();

                        switch (Rnd.Range(0, 9))
                        {
                            case 0:
                                solveText = "BY BCMGF1137";
                                break;
                            case 1:
                                solveText = "POGGERS";
                                break;
                            case 2:
                                solveText = "+8 POINTS...?";
                                break;
                            case 3:
                                solveText = "THX JULIE :)";
                                break;
                            case 4:
                                solveText = "TOO EASY!";
                                break;
                            case 5:
                                solveText = "YOU'RE DONE!";
                                break;
                            case 6:
                                solveText = "MODULO MAZE";
                                break;
                            case 7:
                                solveText = "DINODANCE";
                                break;
                            case 8:
                                solveText = "LIVELINE.OGG";
                                break;
                        }

                        moduleSolved = true;
                        Audio.PlaySoundAtTransform("soundLvlCompleted", transform);

                        R = 0;
                        G = 255;
                        B = 0;

                        while (displays[1].text.Length > 0)
                        {
                            yield return new WaitForSeconds(0.1f);
                            displays[1].text = displays[1].text.Substring(0, displays[1].text.Length - 1);
                        }
                        yield return new WaitForSeconds(1f);
                        displays[1].text += "G";
                        yield return new WaitForSeconds(0.1f);
                        displays[1].text += "G";
                    }
                }
            }
        }
        yield return null;
    }

    // Update is called once per frame
    void Update () { 
        darkMaterials[0].material.SetTextureOffset("_MainTex", new Vector2((float)(Time.time / 1.9), (float)Math.Sin(Time.time / 5.3)));
        darkMaterials[1].material.SetTextureOffset("_MainTex", new Vector2((float)Math.Sin(Time.time / 2.2), (float)Math.Cos(Time.time / 4.4)));
        darkMaterials[2].material.SetTextureOffset("_MainTex", new Vector2((float)Math.Sin(Time.time / 8.3) * 2, (float)Math.Cos(Time.time / 7.3) * 2));

        if (moduleActivated)
        {
            //Debug.LogFormat("[Modulo Maze #{0}] The current color: {1} {2} {3}.", _moduleID, lightMaterials[0].color.r, lightMaterials[0].color.g, lightMaterials[0].color.b);
            //Debug.LogFormat("[Modulo Maze #{0}] The current color: {1} {2} {3}.", _moduleID, R, G, B);
            foreach (MeshRenderer m in lightMaterials)
            {
                m.material.color = new Color32((byte)currentR, (byte)currentG, (byte)currentB, 255);
            }
            foreach (MeshRenderer m in darkMaterials)
            {
                m.material.color = new Color32((byte)(currentR / 3), (byte)(currentG / 3), (byte)(currentB / 3), 255);
            }
            foreach (TextMesh t in displays)
            {
                t.color = new Color32((byte)currentR, (byte)currentG, (byte)currentB, 255);
            }

            currentR = (currentR * 0.98 + R * 0.02);
            currentG = (currentG * 0.98 + G * 0.02);
            currentB = (currentB * 0.98 + B * 0.02);

            displayedNumber = (int)(displayedNumber * 0.9 + number * 0.1);
            if (displayedNumber < number)
            {
                displayedNumber++;
            }
            if (displayedNumber > number)
            {
                displayedNumber--;
            }
            if (moduleSolved)
            {
                if (displays[0].text == "0")
                {
                    displays[0].text = "" + solveText[0];
                }
                else if (displays[0].text != solveText)
                {
                    displays[0].text += solveText[displays[0].text.Length];
                }
            }
            else
            {
                displays[0].text = ((int)(displayedNumber + 0.5)).ToString();
            }
        }
    }

    // Twitch Plays

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <ULDRUDLR> [Move in the directions provided (UDLR). You can perform the commands in a chain, e.g. !{0} RRRLLLDLULLLD]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        float x = 0.5f;
        command = command.ToUpper();
        yield return null;
        if (command.Length >= 10)
        {
            yield return "sendtochat DinoDance";
        }
        for (int i = 0; i < command.Length; i++)
        {
            if (!isInteractable)
            {
                yield return null;
                yield return "sendtochaterror For some reason, the module locked itself. All movements have been halted.";
                break;
            }
            if ("UDLR".IndexOf(command[i]) != -1)
            {
                switch (command[i])
                {
                    case 'U':
                        arrows[0].OnInteract();
                        break;
                    case 'L':
                        arrows[1].OnInteract();
                        break;
                    case 'D':
                        arrows[2].OnInteract();
                        break;
                    case 'R':
                        arrows[3].OnInteract();
                        break;
                }
                x /= 1.05f;

                yield return new WaitForSeconds(x);
            }
        }
    }
}
