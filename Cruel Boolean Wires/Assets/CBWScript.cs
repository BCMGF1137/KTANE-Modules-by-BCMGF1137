using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CBWScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public TextMesh[] displayTexts;
    public KMSelectable[] wires;
    public KMSelectable[] displays;

    private tbool[] booleanValues = Enumerable.Range(0, 36).Select(_ => new tbool("F")).ToArray();
    private int stage = 0; // Max stages is 5

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
    }

    

    // Use this for initialization
    void Start () {

        Generate();
	}

    // Determining values
    void Generate ()
    {
        // ===PART 1: GENERATING BOOLEANS===

        Debug.LogFormat("[Cruel Boolean Wires #{0}] Value 0 is currently {1}.", _moduleID, booleanValues[0].toString());

        stage++;

        // Value 0
        if (stage % 3 == 0) booleanValues[0].setValue("U");
        else if (stage % 3 == 1) booleanValues[0].setValue("T");
        else booleanValues[0].setValue("F");

        // Value 1
        if (Bomb.GetModuleNames().Contains("Bamboozled Again") || Bomb.GetModuleNames().Contains("Ultimate Cycle") || Bomb.GetModuleNames().Contains("UltraStores")) booleanValues[1].setValue("T");
        else if (Bomb.GetModuleNames().Contains("Question Mark") || Bomb.GetModuleNames().Contains("Astrology")) booleanValues[1].setValue("U");
        else booleanValues[1].setValue("F");

        // Value 2
        
    }

    // Update is called once per frame
    void Update () {

	}

    // Class because I felt like it
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
                    value = "" + "TFU"[("TFU".IndexOf(value) - 1) % 3];
                    break;
                case "!": // T swaps with F
                    if (value != "U") value = "" + "TF"[("TF".IndexOf(value) + 1) % 2];
                    break;
                case "!+": // F swaps with U
                    if (value != "T") value = "" + "UF"[("UF".IndexOf(value) + 1) % 2];
                    break;
                case "!-": // U swaps with T
                    if (value != "F") value = "" + "UT"[("UT".IndexOf(value) + 1) % 2];
                    break;
            }
        }

        public string toString()
        {
            return value;
        }
    }
}
