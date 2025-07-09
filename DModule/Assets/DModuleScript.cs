using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class DModuleScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
	public KMSelectable deafShapeD;
	
	private bool _isSolved;
    private bool _OF;
    private bool _org = false;
    private bool _done = false;
    private string _finalInput;
	private string _expectedInput;
    private const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string base64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private int temp1 = 0;
    private string temp2 = "";
    private int D = 0;
    private int _D = 0;
    private int unicorn = 0;
    private int _storesCount = 0;
    private int _storesSolve = 0;

    private string TPString = "";

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
		
	}

    // Use this for initialization
    void Start () {
        Debug.LogFormat("[D #{0}] This module has been generated at Update 3.", _moduleID);
        D = digits.IndexOf(Bomb.GetSerialNumber()[0]) + digits.IndexOf(Bomb.GetSerialNumber()[1]) + digits.IndexOf(Bomb.GetSerialNumber()[2]) + 
            digits.IndexOf(Bomb.GetSerialNumber()[3]) + digits.IndexOf(Bomb.GetSerialNumber()[4]) + digits.IndexOf(Bomb.GetSerialNumber()[5]);

        Debug.LogFormat("[D #{0}] First Character: {1}={2}.",  _moduleID, Bomb.GetSerialNumber()[0], digits.IndexOf(Bomb.GetSerialNumber()[0]));
        Debug.LogFormat("[D #{0}] Second Character: {1}={2}.", _moduleID, Bomb.GetSerialNumber()[1], digits.IndexOf(Bomb.GetSerialNumber()[1]));
        Debug.LogFormat("[D #{0}] Third Character: {1}={2}.",  _moduleID, Bomb.GetSerialNumber()[2], digits.IndexOf(Bomb.GetSerialNumber()[2]));
        Debug.LogFormat("[D #{0}] Fourth Character: {1}={2}.", _moduleID, Bomb.GetSerialNumber()[3], digits.IndexOf(Bomb.GetSerialNumber()[3]));
        Debug.LogFormat("[D #{0}] Fifth Character: {1}={2}.",  _moduleID, Bomb.GetSerialNumber()[4], digits.IndexOf(Bomb.GetSerialNumber()[4]));
        Debug.LogFormat("[D #{0}] Sixth Character: {1}={2}.",  _moduleID, Bomb.GetSerialNumber()[5], digits.IndexOf(Bomb.GetSerialNumber()[5]));

        if (D < 100) {
            _expectedInput = Convert.ToString(D);
            _expectedInput = "0" + _expectedInput;
        } else {
            _expectedInput = Convert.ToString(D);
        }

        Debug.LogFormat("[D #{0}] Your D is equal to {1}. That's the first part of the module.", _moduleID, _expectedInput);
        Debug.LogFormat("[D #{0}] D modulo 3 is equal to {1}.", _moduleID, D%3);

        temp1 = 2*(D%3);

        Debug.LogFormat("[D #{0}] We are looking at characters {1} and {2} of the serial number.", _moduleID, temp1 + 1, temp1 + 2);

        temp2 = "" + Bomb.GetSerialNumber()[temp1] + Bomb.GetSerialNumber()[temp1+1];

        _OF = false;

        for (int i = 0; i < Bomb.GetModuleIDs().Count; i++)
        {
            if (Bomb.GetModuleIDs()[i] == "omegaForget")
            {
                _OF = true;
            }
        }

        if (_OF)
        {
            temp2 = "" + Bomb.GetSerialNumber()[temp1 + 1] + Bomb.GetSerialNumber()[temp1];
            Debug.LogFormat("[D #{0}] There is an OmegaForget, so we will swap the digits.", _moduleID);
        }
        else {
            Debug.LogFormat("[D #{0}] There is no OmegaForget, so we will continue as usual.", _moduleID);
        }

        Debug.LogFormat("[D #{0}] Our current concatenation is \"{1}\".", _moduleID, temp2);

        _D = (base64.IndexOf(temp2[0])*64 + base64.IndexOf(temp2[1])) %1000;

        _expectedInput = "" + _expectedInput + Convert.ToString(_D + 1000)[1] + Convert.ToString(_D + 1000)[2] + Convert.ToString(_D + 1000)[3];

        Debug.LogFormat("[D #{0}] Effectively, the obtained value is {1}.", _moduleID, _D);
        Debug.LogFormat("[D #{0}] The correct submission is {1}.", _moduleID, _expectedInput);

        deafShapeD.OnInteract += delegate () {

            deafShapeD.AddInteractionPunch(.5f);
            if (_isSolved == false)
            {
                Debug.LogFormat("[D #{0}] You pressed the D when the last digit of the timer was {1}.", _moduleID, ((int)Bomb.GetTime() % 10));
                _finalInput = _finalInput + Convert.ToString(((int)Bomb.GetTime() % 10));
            }
            
            return false;
		};

        for (int i = 0; i < Bomb.GetSolvableModuleIDs().Count; i++)
        {
            if (Bomb.GetSolvableModuleIDs()[i] == "simonStores" || Bomb.GetSolvableModuleIDs()[i] == "UltraStores") {
                _storesCount += 1;
            }
        }

// Unicorn rule :))))))

            if (Bomb.GetPortPlates().Where(plate => plate.Contains("DVI") && !plate.Contains("PS2") && !plate.Contains("RJ45") && !plate.Contains("StereoRCA")).Any())
            {
                unicorn += 1;
                Debug.LogFormat("[D #{0}] There is a port plate with only a DVI port on it.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[D #{0}] There are NO port plates with only a DVI port on them.", _moduleID);
            };
            if (Bomb.GetBatteryCount() == Bomb.GetBatteryHolderCount() && Bomb.GetBatteryCount() >= 1)
            {
                unicorn += 1;
                Debug.LogFormat("[D #{0}] There are no AA batteries and at least one D battery.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[D #{0}] Either there are AA batteries present or there are no batteries at all. In either case, I'm too lazy to check.", _moduleID);
            }
            if (Bomb.IsIndicatorPresent("SND") || Bomb.IsIndicatorPresent("IND"))
            {
                unicorn += 1;
                Debug.LogFormat("[D #{0}] There is an SND or IND indicator present.", _moduleID);
            }
            else
            {
                Debug.LogFormat("[D #{0}] There are NO SND or IND indicators present.", _moduleID);
            }

            Debug.LogFormat("[D #{0}] Of the 3 special rules, {1} applied.", _moduleID, unicorn);

            if (unicorn >= 2) {
                Debug.LogFormat("[D #{0}] The unicorn rule applies. Get rekt! Get no hope! >:D", _moduleID);
                Debug.LogFormat("[D #{0}] In all seriousness, make sure you solve ALL instances of Simon Stores or UltraStores before this one.", _moduleID);
            } else {
                Debug.LogFormat("[D #{0}] The unicorn rule doesn't apply. You don't have to solve Simon Stores or UltraStores before D (although it's completely optional).", _moduleID);
            }

        // End of unicorn rule :))))))

        // Organization Compatibility

         for (int i = 0; i < Bomb.GetSolvableModuleIDs().Count; i++)
         {
             if ((Bomb.GetSolvableModuleIDs()[i] == "organizationModule") && !_org)
                {
                if (unicorn >= 2)
                {
                    Debug.LogFormat("[D #{0}] Actually... uhhhhhhh...", _moduleID);
                    Debug.LogFormat("[D #{0}] Disregard what I just said because there's an Organization (which if the rule was kept the module would be marked with a red triangle on the Repository of Manual Pages, which I think it is by now)." +
                        "You can freely solve this module without having to solve Simon Stores or UltraStores.", _moduleID);
                }
                unicorn = 0;
                _org = true;
                }
         }


        _finalInput = "";
		_isSolved = false;
	}
	
	// Update is called once per frame
	void Update () {

        if (!_isSolved)
        {
            _storesSolve = 0;

            for (int i = 0; i < Bomb.GetSolvedModuleIDs().Count; i++)
            {
                if (Bomb.GetSolvedModuleIDs()[i] == "simonStores" || Bomb.GetSolvedModuleIDs()[i] == "UltraStores")
                {
                    _storesSolve += 1;
                }
            }
            if (unicorn >= 2 && _storesSolve == _storesCount && !_done && _storesCount > 0)
            {
                Debug.LogFormat("[D #{0}] All instances of Simon Stores and UltraStores have been solved, so now you can freely solve D.", _moduleID);
                _done = true;
            }

        }

		if (_finalInput.Length == 6 && !_isSolved) {
			if (_finalInput != _expectedInput) {
				Debug.LogFormat("[D #{0}] You submitted \"" + _finalInput + "\", which is NOT the expected input of \"" + _expectedInput + "\". Try again. D:", _moduleID);
				Module.HandleStrike();
				_finalInput = "";
			} else {
                if (_storesSolve == _storesCount || unicorn < 2) {
                    Debug.LogFormat("[D #{0}] You submitted \"" + _finalInput + "\", which is correct. Module solved! :D", _moduleID);
                    Module.HandlePass();
                    _isSolved = true;
                } else {
                    Debug.LogFormat("[D #{0}] You submitted \"" + _finalInput + "\", which normally is correct, but you still have Simon Stores and UltraStores modules to solve. Come back to me when you've done all that. D:<", _moduleID);
                        Module.HandleStrike();
                        _finalInput = "";
                    
                }
			}
		}
	}

    // TWITCH PLAYS.

        private bool isInputValid(string someStringIDK)
    {
        int numThing = 0;
        bool preformed = int.TryParse(someStringIDK, out numThing);
        if (preformed == true && (numThing >= 0 && numThing <= 9))
        {
            return true;
        }
        return false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <X> [Presses the D when the last digit of the timer is X, where X is a digit. You can also input six digits, but not if you already inputted one or more digits.] | !{0} clearInput [Removes all digits from the submission. Useful in the event you or someone else makes a mistake.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
       command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');

        Match match = Regex.Match(command, @"^submit \d$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (parameters.Length == 2 && isInputValid(parameters[1]))
            {
                yield return null;
                int timepress = 0;
                int.TryParse(parameters[1], out timepress);

                yield return "sendtochat We are pressing the D when the last digit of the timer is " + timepress + "!";

                while (timepress != ((int)Bomb.GetTime() % 10))
                {
                    yield return "trycancel Press cancelled.";
                    yield return new WaitForSeconds(.1f);
                }
                deafShapeD.OnInteract();
            }
            else
            {
                yield return "sendtochaterror That response is invalid! Please provide a single digit as a parameter.";
            }
        }
         match = Regex.Match(command, @"^submit \d{6}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (_finalInput.Length == 0) {
                if (parameters.Length == 2 && parameters[1].Length == 6)
                {
                    TPString = ""; // Checks if it's all digits
                    for (int i = 0; i < 6; i++)
                    {
                        if ("0123456789".Contains(parameters[1][i]))
                        {
                            TPString = "" + TPString + parameters[1][i];
                        } else
                        {
                            TPString = "" + TPString + "X";
                        }
                    }

                    if (TPString.Contains("X")) {
                        yield return "sendtochaterror That response is invalid! Please provide six digits as a parameter.";
                    } else
                    {
                        yield return null;
                        yield return "sendtochat Submitting " + parameters[1] + " into the module!";
                        for (int i = 0; i < 6; i++)
                        {
                            while (parameters[1][i].ToString() != ((int)Bomb.GetTime() % 10).ToString())
                            {
                                yield return "trycancel Presses cancelled. Note that presses are still saved into the module!";
                                yield return new WaitForSeconds(.1f);
                            }
                            deafShapeD.OnInteract();
                        }
                    }

                }
                else
                {
                    yield return "sendtochaterror That response is invalid! Please provide six digits as a parameter.";
                }
            } else
            {
                yield return "sendtochaterror The \"submit\" command cannot be used if you already inputted at least one digit!";
            }
        }
         match = Regex.Match(command, @"^clearInput$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            yield return null;
            _finalInput = "";
            yield return "sendtochat Module input cleared!";
            Debug.LogFormat("[D #{0}] Input cleared due to reset command.", _moduleID);
        }
    }

    IEnumerator TwitchHandleForcedSolve() // My first autosolver, 6 months later :)
    {
        Debug.LogFormat("[D #{0}] Autosolving (TP).", _moduleID);
        yield return null;
        _finalInput = "";
        if (_isSolved)
        {
            yield break;
        }
        _finalInput = "";
        for (int i = 0; i < 6; i++)
        {
            while (_expectedInput[i].ToString() != ((int)Bomb.GetTime() % 10).ToString())
            {
                yield return new WaitForSeconds(.1f);
            }
            deafShapeD.OnInteract();
        }
    }
}