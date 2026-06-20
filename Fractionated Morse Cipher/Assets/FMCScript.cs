using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class FMCScript : MonoBehaviour
{

    static int _moduleIDCounter = 1;
    int _moduleID;

    // Module/Bomb stuff
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    // Module stuff that appears in Unity
    public Text displayText;
    public Text submissionText;
    public KMSelectable[] buttons;
    public Transform moduleTransform;

    // Coroutines
    private Coroutine flashingSubmission;

    // Readonly variables
    private readonly string _ALPHABET = "#ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Private variables
    private Message message;
    private char submitChar = '#'; // What are we trying to submit into the module?
    private string submission = "";
    private string keyword = "";
    private bool moduleSolved = false;

    // Twitch Plays easter egg
    private IDictionary<string, object> tpAPI;
    private bool tpMode;

    private void Awake()
    {
        _moduleID = _moduleIDCounter++;

        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate ()
            {
                StartCoroutine(PressButton(button));
                return false;
            };
        }
    }

    // Use this for initialization
    void Start()
    {
        Generate();

        flashingSubmission = StartCoroutine(FlashSubmission());
        
        GameObject tpAPIGameObject = GameObject.Find("TwitchPlays_Info");
        if (tpAPIGameObject != null)
        {
            tpAPI = tpAPIGameObject.GetComponent<IDictionary<string, object>>();
            Debug.LogFormat("[Fractionated Morse Cipher #{0}] Twitch Plays is active.", _moduleID);
        }
    }

    void Generate()
    {
        message = FMCData.messages.PickRandom();
        keyword = FMCData.keywords.PickRandom();
        Debug.LogFormat("[Fractionated Morse Cipher #{0}] Original plaintext: \"{1}\".", _moduleID, message.GetMessage());
        Debug.LogFormat("[Fractionated Morse Cipher #{0}] The module's ciphertext \"{1}\".", _moduleID, message.EncryptMessage(keyword));
        Debug.LogFormat("[Fractionated Morse Cipher #{0}] The utilized keyword is \"{1}\".", _moduleID, keyword.ToUpper());

        string special = "";
        // Determine the hint, i.e. the first few words
        if (Rnd.Range(0f, 1.0f) < 0.5f) // Case A: First few words
        {
            int wordCount = 0;
            do
            {
                wordCount++;
                special = message.FilterMessage().Split(' ').Take(wordCount).Join(" ");
            }
            while (message.EncryptMorse(special).Length < 15);
            
            if (wordCount == 1)
            {
                special = "The first word is \"" + special + "\".";
            }
            else
            {
                string[] wordNums = "zero one two three four five six seven eight nine ten".Split(' ');
                special = "The first " + wordNums[wordCount] + " words are \"" + special + "\".";
            }
        }
        else // Case B: Last few words
        {
            int wordCount = 0;
            do
            {
                wordCount++;
                special = message.FilterMessage().Split(' ').TakeLast(wordCount).Join(" ");
            }
            while (message.EncryptMorse(special).Length < 18);

            if (wordCount == 1)
            {
                special = "The last word is \"" + special + "\".";
            }
            else
            {
                string[] wordNums = "zero one two three four five six seven eight nine ten".Split(' ');
                special = "The last " + wordNums[wordCount] + " words are \"" + special + "\".";
            }
        }

        displayText.text = "Find the keyword for the following Fractionated Morse Cipher. It is a quote from an alternative manual of a KTANE module. "
            + special + "\n\n" + message.EncryptMessage(keyword) + " [" + message.EncryptMessage(keyword).Length + "]";

        //Debug.LogFormat("[Fractionated Morse Cipher #{0}] No spoilers...", _moduleID);
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Returns a string of random letters based on the given length.
    /// </summary>
    string Gibberish(int length)
    {
        string result = "";
        for (int i = 0; i < length; i++)
        {
            result += _ALPHABET.Where(x => x != '#').PickRandom();
        }
        return result;
    }

    IEnumerator FlashSubmission() // Flash the last character in the submission.
    {
        while (true)
        {
            while (Bomb.GetTime() % 0.5 >= 0.25)
            {
                submissionText.text = submission + submitChar;
                yield return null;
            }
            while (Bomb.GetTime() % 0.5 <= 0.25)
            {
                submissionText.text = submission;
                yield return null;
            }
        }
    }

    IEnumerator SolveAnimation()
    {
        // Immediate stuff to handle
        StopCoroutine(flashingSubmission);
        submissionText.text = "";
        Audio.PlaySoundAtTransform("Solve", transform);

        // Time-based solve anim
        float elapsed = 0f;

        // Internally modify the strength of the screenshake
        float shakeStrength = 0.004f;

        // Module rotation (massive)
        while (elapsed < 2f)
        {
            submissionText.text = "(yes)".Substring(0, (int)Math.Min(elapsed * 13, 5));
            displayText.text = message.EncryptMessage(keyword).Substring(0, (int)Mathf.Lerp(0f, message.EncryptMessage(keyword).Length, (elapsed - 1f) * 1.5f));
            moduleTransform.transform.localEulerAngles = new Vector3(0f, 45f, (float)(-180 * Math.Pow((elapsed - 2), 3)));
            yield return null;
            elapsed += Time.deltaTime;
        }
        moduleTransform.transform.localEulerAngles = new Vector3(0f, 45f, 0f);

        // Module shaking + message reveal
        while (elapsed < 8f)
        {
            float progress = Mathf.InverseLerp(2f, 8f, elapsed);
            displayText.text = ""
                + Gibberish((int)(message.EncryptMessage(keyword).Length * (1 - progress)))
                + message.GetMessage().Substring((int)(message.GetMessage().Length * (1 - progress)))
                ;
            if (elapsed > 7f)
            {
                shakeStrength = 0.004f * (8f - elapsed);
            }
            moduleTransform.transform.localPosition = new Vector3(Rnd.Range(-shakeStrength, shakeStrength), Rnd.Range(-shakeStrength, shakeStrength), Rnd.Range(-shakeStrength, shakeStrength));
            yield return null;
            elapsed += Time.deltaTime;
        }
        moduleTransform.transform.localPosition = new Vector3(0f, 0f, 0f);
        Module.HandlePass();

        string moduleReferenced = message.GetModule();
        if (moduleReferenced.Length > 11)
            moduleReferenced = moduleReferenced.Substring(0, 10) + "...";

        submissionText.text = "";
        for (int i = 0; i < moduleReferenced.Length; i++)
        {
            yield return new WaitForSeconds(0.1f);
            submissionText.text = moduleReferenced.Substring(0, 1 + i);
        }

        if (message.GetModule().Length <= 11) yield break;
        else
        {
            int maxPos = message.GetModule().Length - 10;
            while (true)
            {
                int currentPos = 0;
                yield return new WaitForSeconds(1f);
                while (currentPos + 1 < maxPos)
                {
                    currentPos++;
                    submissionText.text = "..." + message.GetModule().Substring(currentPos, 10) + "...";
                    yield return new WaitForSeconds(0.1f);
                }
                currentPos++;
                submissionText.text = "..." + message.GetModule().Substring(currentPos, 10);

                yield return new WaitForSeconds(1f);
                while (currentPos > 1)
                {
                    currentPos--;
                    submissionText.text = "..." + message.GetModule().Substring(currentPos, 10) + "...";
                    yield return new WaitForSeconds(0.1f);
                }
                currentPos--;
                submissionText.text = message.GetModule().Substring(currentPos, 10) + "...";
            }
        }
    }

    // Button handling
    IEnumerator PressButton(KMSelectable button)
    {

        // Identify the button that's being pressed
        int tempPress = 0;
        for (int i = 0; i < 4; i++)
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

        /*
        Buttons in order:
        +1, +3, +9, #
        */

        // Classic button stuff
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        button.AddInteractionPunch(0.5f);

        if (moduleSolved) yield break;

        if (tempPress == 0) // +1
        {
            submitChar = _ALPHABET[(_ALPHABET.IndexOf(submitChar) + 1) % 27];
        }
        else if (tempPress == 1) // +3
        {
            submitChar = _ALPHABET[(_ALPHABET.IndexOf(submitChar) + 3) % 27];
        }
        else if (tempPress == 2) // +9
        {
            submitChar = _ALPHABET[(_ALPHABET.IndexOf(submitChar) + 9) % 27];
        }
        else // #
        {
            if (submitChar != '#') // Add a letter to the submission
            {
                submission += submitChar;
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, transform);
            }
            if (submission.Length == 8 || submitChar == '#') // Submit the word, or do so automatically once 8 letters are entered
            {
                Debug.LogFormat("[Fractionated Morse Cipher #{0}] You submitted the keyword \"{1}\".", _moduleID, submission);

                // Test the submission
                bool isSubmissionValid = true;
                if (submission.Length != submission.Distinct().Count()) // The submission cannot contain duplicate letters.
                {
                    Debug.LogFormat("[Fractionated Morse Cipher #{0}] The submitted keyword \"{1}\" contains duplicate letters, which is not possible with how a Fractionated Morse Cipher works. Strike!", _moduleID, submission);
                    isSubmissionValid = false;
                }
                else
                {
                    isSubmissionValid = message.EncryptMessage(submission) == message.EncryptMessage(keyword);
                    if (!isSubmissionValid)
                    {
                        Debug.LogFormat("[Fractionated Morse Cipher #{0}] The submitted keyword \"{1}\" would encrypt the original message into \"{2}\", which is not the same as the original display. Strike!", _moduleID, submission, message.EncryptMessage(submission));
                    }
                }

                if (isSubmissionValid)
                {
                    moduleSolved = true;
                    Debug.LogFormat("[Fractionated Morse Cipher #{0}] The submitted keyword \"{1}\" is a valid keyword. Solving module...", _moduleID, submission);
                    button.AddInteractionPunch(999f);
                    if (tpAPI != null) tpAPI["ircConnectionSendMessage"] = "(yes)";
                    StartCoroutine(SolveAnimation());
                }
                else
                {
                    Module.HandleStrike();
                    submitChar = '#';
                    submission = "";
                }
            }
        }
        yield break;
    }

    // Twitch Plays
#pragma warning disable 414
    private string TwitchHelpMessage = "\"!{0} 1\", \"!{0} 3\", \"!{0} 9\", \"!{0} #\" [Press the buttons with these labels. Chain commands without spaces, e.g. \"!{0} 93#1#393#1391#9#133##\".]"
        + "\n\"!{0} read\" [Sends the displayed message to chat. Useful if the message is too unclear to read even with zooming.]" +
        "\n\"!{0} clear\" [Clears the submission without a strike. Useful if someone arrived before you, entered letters, and left.]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        if (command == "READ")
        {
            yield return "sendtochat " + displayText.text;
        }
        else if (command == "CLEAR")
        {
            yield return null;
            if (!moduleSolved)
            {
                submission = "";
                submitChar = '#';
                yield return "sendtochat The submission has been cleared.";
            }
            else
            {
                yield return "sendtochaterror The module is currently solving!";
            }
        }
        else
        {
            Match match = Regex.Match(command, "^[139#]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            if (match.Success)
            {
                foreach (char c in command)
                {
                    yield return null;
                    int buttonIndex = "139#".IndexOf(c);
                    if (buttonIndex != 3) // Are we NOT pressing #?
                    {
                        buttons[buttonIndex].OnInteract();
                    }
                    else // We are pressing # and risking a submission
                    {
                        if (submitChar == '#' || submission.Length == 7)
                        {
                            yield return "strike";
                            yield return "solve";
                            buttons[3].OnInteract();
                            yield break;
                        }
                        else buttons[3].OnInteract();
                    }

                    yield return "trycancel";
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                yield return "sendtochaterror That command does not exist!";
            }
        }
    }
}