using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Message {
    private Dictionary<char, string> morseDictionary = new Dictionary<char, string>
    {
        {'E', "."},
        {'T', "-"},
        {'I', ".."},
        {'A', ".-"},
        {'N', "-."},
        {'M', "--"},
        {'S', "..."},
        {'U', "..-"},
        {'R', ".-."},
        {'W', ".--"},
        {'D', "-.."},
        {'K', "-.-"},
        {'G', "--."},
        {'O', "---"},
        {'H', "...."},
        {'V', "...-"},
        {'F', "..-."},
        {'L', ".-.."},
        {'P', ".--."},
        {'J', ".---"},
        {'B', "-..."},
        {'X', "-..-"},
        {'C', "-.-."},
        {'Y', "-.--"},
        {'Z', "--.."},
        {'Q', "--.-"},
    };
    private Dictionary<string, char> fractionatedDictionary;

    private string message;
    private string origin;

    public Message(string message, string origin) // Constructor
    {
        this.message = message;
        this.origin = origin;
    }

    public string GetMessage()
    {
        return message;
    }

    public string GetModule()
    {
        return origin;
    }

    /// <summary>
    /// Encrypts the object's message using a Fractionated Morse Cipher.
    /// </summary>
    public string EncryptMessage(string key) // Encrypt the message using the key
    {
        key = key.ToUpper();

        string _ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        _ALPHABET = key + _ALPHABET.Where(x => !key.Contains(x)).Join("");

        /* Brainstorming:
        - Step 0: Take the original message, convert to uppercase, and remove all characters other than spaces or letters.
            - (Hyphens are treated as spaces)
        - Step 1: Convert the message to morse code.
        - Step 2: Split the message into trigrams (groups of 3), appending slashes if the message is not a multiple of 3 in length.
        - Step 3: Construct the alphabet (already handled).
        - Step 4: Place the alphabet above a 3x26 table.
        - Step 5: Use the table to turn the final morse into letters.
        */

        // Step 0
        string ciphertext = FilterMessage();

        // Step 1
        ciphertext = EncryptMorse(ciphertext);
        
        // Step 2
        while (ciphertext.Length % 3 != 0)
        {
            ciphertext += "/";
        }

        // Step 4
        fractionatedDictionary = _ALPHABET.Select((letter, i) => new
        {
            Morse = "" + ".-/"[i / 9] + ".-/"[(i / 3) % 3] + ".-/"[i % 3],
            Letter = letter
        }).ToDictionary(x => x.Morse, x => x.Letter);

        // Step 5
        string temp = ciphertext + " ";
        ciphertext = "";
        while (temp.Length > 1)
        {
            ciphertext += fractionatedDictionary[temp.Substring(0, 3)];
            temp = temp.Substring(3);
        }

        // End of encryption.
        return ciphertext;
    }

    /// <summary>
    /// Encrypts a message into Morse Code.
    /// </summary>
    public string EncryptMorse(string message) 
    {
        string morse = "";
        foreach (char c in message)
        {
            if (c == ' ') morse += "/";
            else morse += morseDictionary[c] + "/";
        }
        morse = morse.Substring(0, morse.Length - 1);
        return morse;
    }

    /// <summary>
    /// Converts the message to uppercase and removes characters other than spaces and letters. Hyphens are substituted with spaces.
    /// </summary>
    public string FilterMessage()
    {
        return message.ToUpper().Replace('-', ' ').Where(x => " ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(x)).Join("");
    }
}
