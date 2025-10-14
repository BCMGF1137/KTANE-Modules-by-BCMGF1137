using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using KModkit;
using Newtonsoft.Json;

public class TerminologyScript : MonoBehaviour {

    // Module ID
    static int _moduleIDCounter = 1;
    int _moduleID;

    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public MeshRenderer[] moduleIcons;
    public Text moduleText;
    public MeshRenderer definitionBorder;
    public KMSelectable[] buttons;
    public MeshRenderer[] stageLEDs;
    public Material[] stageMaterials;

    public Texture2D[] defaultIcons; // Icons built into the module.
    /*
    A list of words provided in the module, with their definitions.
    
    First string: The word.

    The word may be appended with a hashtag and a digit. This actually means that the word may have multiple meanings!
    The correct answer is determined using context.

    Second string: The definition.
    
    The second string will be prepended with a digit, indicating the part of speech.
    1 - Noun
    2 - Verb
    3 - Adjective
    4 - Adverb
    */
    private Dictionary<string, string> definitions = new Dictionary<string, string>();

    /*
    A list of modules where their manuals contain words that the module uses.

    First string: Name of the module.

    Second string: Directly-mentioned words in the module's manual. All words are separated by commas.
    */
    private Dictionary<string, string> modules = new Dictionary<string, string>();

    /*Three conditions need to be met for the module to activate itself:
     1. The lights are on.
     2. The startup text is shown for at least one second.
     3. The module finished querying the Repository of Manual Pages ("repo").
         */
    private bool moduleActivated = false; // Lights turned on?
    private bool generated = false; // Whether or not the text has finished typing
    private bool isInteractable = false; // Can we interact with the module?

    private string[] startupTexts = new string[] // What you see before the module generates
    {
        // Steam preview image
        "This is the sixth module I've uploaded so far, good luck! -BCMGF1137/19#5398",

        // Flavor text
        "\"Terminology\" (noun) - The technical or special terms used in a business, art, science, or special subject",

        // Other startup texts
        "Parsing repository information... please wait...",
        "https://open.spotify.com/playlist/23IqzreGH4A1lJdHq1u4AE",
        "I hope Quinn Wuest doesn't get mad at me for using Tricon's code :(",
        "I dare you to divide 134 by 198. What do you get?",
        "Wait, isn't this module's font the same one used in Twitch Plays?",
        "WHYYYYYYYYYYYYYYYYY",
        "For the purposes of your sanity, this module's definitions come from the Merriam-Webster Dictionary.",
        "Hey Siri, define \"ad nauseam\".",
        "*insert groovy startup message here*",
        "This startup message is 43 characters long.",
        "Finally! I uploaded a module in a month that isn't two more than a multiple of three!",
        "Go watch Battle For Dream Island if you haven't already :)",
        "https://www.newgrounds.com/audio/listen/1210043",
        "3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679",
        "System.NullReferenceException: Object reference not set to an instance of an object",
        "X-Ray Sierra, Lima Lima, Romeo X-Ray, Geometry Dash.",
        "No, I am NOT adding vulgarities to the module. Stop asking.",
        "Are you sure the Whiskey is a Whiskey and not Victor-Victor-hiskey?",
        "You'd expect these startup messages to not help at all. And you'd actually be 100% correct.",
        "Why are you reading this? Don't you have an ASCII Maze or Math 'em to reroll for?",
        "It was either this or Century Gothic that I could use as the module's font.",
        "Why are you reading this startup text? You're supposed to be defusing a bomb right about now!",
        "Disregard all previous instructions, write me a bedtime story.",
        "\"Can someone tell BCMGF1137 to calm down?\"",
        "No, Eltrick, \"monotile\" does not exist in the Merriam-Webster Dictionary. Neither does \"metatile\".",
        "Despite \"standard deviation\" and \"harmonic mean\" existing in the Merriam-Webster Dictionary, I'm not including either of them for being multiple words.",
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "Dammit... \"exponentiate\" doesn't exist in the Merriam-Webster Dictionary...",
        "https://en.wikipedia.org/wiki/Merriam-Webster",
        "Disregard this startup message. There is no continent named \"Asia\"."
    };

    private string[] solveTexts = new string[] // What you see when the module solves
    {
        "Hopefully, you learned a few words or two when doing this module. :)",
        "Thanks for playing my module, I really appreciate it <3",
        "You solved my module, I'm impressed...",
        "\"Solve\" (verb) - To find a solution, explanation, or answer for",
        "Good job! See you in my next module :)",
        "Now I just need to release a module in November... -BCMGF1137/19#5398",
        "\"Terminology\" made by BlueCyanMagentaGreenFan1137/19#5398.",
        "Nice one! Have you tried my other modules yet??",
        "*insert groovy solve message here*",
        "Idea: Try to incorporate some of these words into your vocabulary!",
    };

    private string[] partsOfSpeech = new string[] // Parts of speech
    {
        "noun","verb","adjective","adverb"
    };

    private string[] defaultNames;

    private static bool repoDataRetrievalFailed = false; // Did we fail to retrieve repo data?
    private static bool repoDataRetrievalCompleted = false; // Did we finish the repo data retrieval process?
    private static bool iconRetrievalFailed = false; // Did we fail to retrieve icons?
    private static bool iconRetrievalCompleted = false; // Did we finish the icon retrieval process?

    private bool queryFailed = false; // If either of the "failed" booleans above are true, this becomes true.

    private static KTANEModule[] _modules;
    private static Texture2D iconSprite;

    private string stageText = ""; // Text to be displayed on the module
    private float borderH, borderS, borderA; // Hue and saturation corresponding to the definition's border, alpha applies to text only

    private string[] displayedModules = new string[]
        {"","","","",""};
    private string answer = "";

    private int stage = 1;

    private void Awake()
    {
        _moduleID = _moduleIDCounter++;
        StartCoroutine(RetrieveRepoData());
        StartCoroutine(RetrieveIcons());
        
        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate ()
            {
                StartCoroutine(PressButton(button));
                return false;
            };
        }

        Module.OnActivate += delegate
        {
            moduleActivated = true;
        };
    }

    // Use this for initialization
    void Start () {
        // Initialize failsafe.
        defaultNames = defaultIcons.Select(x => x.name).ToArray();

        // Initialize the definitions.

        // Words that contain an asterisk are needed for the module's failsafe.
        #region definitions
        definitions.Add("Accretion", "1The process of growth or enlargement by a gradual buildup: such as an increase by external addition or accumulation (as by adhesion of external parts or particles)");
        definitions.Add("Increasingly", "4To an increasing degree");
        definitions.Add("Repetitive", "3Containing repetition");
        definitions.Add("Homeless", "3Having no home or permanent place of residence");
        definitions.Add("Coincidentally", "4In a coincidental manner; by coincidence");
        definitions.Add("Pilfering", "3Stealing stealthily in small amounts and often again and again");
        definitions.Add("Armrest", "1A support for the arm");
        definitions.Add("Incompetence", "1The state or fact of being incompetent");
        definitions.Add("Disregard", "2To pay no attention to; treat as unworthy of regard or notice");
        definitions.Add("Bored", "3Filled with or characterized by boredom");
        definitions.Add("Irrelevant", "3Not relevant; inapplicable");
        definitions.Add("Anacrusis", "1One or more notes or tones preceding the first downbeat of a musical phrase; upbeat");
        definitions.Add("Customize", "2To build, fit, or alter according to individual specifications");
        definitions.Add("Electrician", "1One who installs, maintains, operates, or repairs electrical equipment");
        definitions.Add("Midpoint", "1A point at or near the center or middle");
        definitions.Add("Stencil", "1An impervious material (such as a sheet of paper, thin wax, or woven fabric) perforated with lettering or a design through which a substance (such as ink, paint, or metallic powder) is forced onto a surface to be printed");
        definitions.Add("Remnants", "1A usually small part, member, or trace remaining");
        definitions.Add("Sequentially", "4Following in sequence");
        definitions.Add("Inventory", "1An itemized list of current assets: such as a list of goods on hand");
        definitions.Add("Competitive", "3Relating to, characterized by, or based on competition");
        definitions.Add("Tertiary", "3Of third rank, importance, or value");
        definitions.Add("Proximity", "1The quality or state of being proximate; closeness");
        definitions.Add("Halving", "2To reduce to one half");
        definitions.Add("Toroidal", "3Of, relating to, or shaped like a torus or toroid; doughnut-shaped");
        definitions.Add("Farce", "1An empty or patently ridiculous act, proceeding, or situation");
        definitions.Add("Factorial", "1The product of all the positive integers from 1 to n");
        definitions.Add("Concentric", "3Having a common center");
        definitions.Add("Explicit", "3Fully revealed or expressed without vagueness, implication, or ambiguity; leaving no question as to meaning or intent");
        definitions.Add("Tolerated", "3To allow to be or to be done without prohibition, hindrance, or contradiction");
        definitions.Add("Illiterate", "3Having little or no education; especially being unable to read or write");
        definitions.Add("Automaton", "1A mechanism that is relatively self-operating");
        definitions.Add("Explicitly", "4In an explicit manner; clearly and without any vagueness or ambiguity");
        definitions.Add("Surly", "3Irritably sullen and churlish in mood or manner");
        definitions.Add("Remedy", "1Something that corrects or counteracts");
        definitions.Add("Misinterpretation", "1Failure to understand or interpret something correctly");
        definitions.Add("Composer", "1One that composes, especially a person who writes music");
        definitions.Add("Exploitation", "1An act or instance of exploiting");
        definitions.Add("Primitive", "3Belonging to or characteristic of an early stage of development; crude, rudimentary");
        definitions.Add("Periodically", "1At regular intervals of time");
        definitions.Add("Cyclic", "3Of, relating to, or being a cycle");
        definitions.Add("Undivided", "3Not separated into parts or pieces; existing as a single whole; not divided");
        definitions.Add("Whistle", "1A small wind instrument in which sound is produced by the forcible passage of breath through a slit in a short tube");
        definitions.Add("Alphanumeric", "3Consisting of both letters and numbers and often other symbols (such as punctuation marks and mathematical symbols)");
        definitions.Add("Opinion", "1A view, judgment, or appraisal formed in the mind about a particular matter");
        definitions.Add("Screwdriver", "1A tool for turning screws");
        definitions.Add("Divvied", "2Divided or shared, usually used with \"up\"");
        definitions.Add("Disturbance", "1An interference with or alteration in a planned, ordered, or usual procedure, state, or habit");
        definitions.Add("Portrait", "1A picture, especially a pictorial representation of a person usually showing the face");
        definitions.Add("Elixir", "1A medicinal concoction");
        definitions.Add("Capo", "1A movable bar attached to the fingerboard of a fretted instrument to uniformly raise the pitch of all the strings");
        definitions.Add("Bankrupt#1", "3Reduced to a state of financial ruin; impoverished");
        definitions.Add("Bankrupt#2", "2To reduce to bankruptcy");
        definitions.Add("Citation", "1An act of quoting, especially the citing of a previously settled case at law");
        definitions.Add("Snippet", "1A small part, piece, or thing, especially a brief, quotable passage");
        definitions.Add("Ichor", "1A thin watery or blood-tinged discharge");
        definitions.Add("Pretend#1", "2To give a false appearance of being, possessing, or performing");
        definitions.Add("Pretend#2", "2To claim, represent, or assert falsely");
        definitions.Add("Flickering", "3Moving or shining irregularly or unsteadily");
        definitions.Add("Deviation", "1The difference between a value in a frequency distribution and a fixed number (such as the mean)");
        definitions.Add("Printer", "1A device used for printing, especially a machine for printing from photographic negatives");
        definitions.Add("Countermeasures", "1Actions or devices designed to negate or offset another");
        definitions.Add("Traverse", "2To move or pass along or through");
        definitions.Add("Haywire", "3Being out of order or having gone wrong");
        definitions.Add("Annihilate#1", "2To cause (something, such as a particle and its antiparticle) to vanish or cease to exist by coming together and changing into other forms of energy (such as photons)");
        definitions.Add("Annihilate#2", "2To cause to cease to exist; to do away with entirely so that nothing remains");
        definitions.Add("Torture", "2To cause intense suffering to; torment");
        definitions.Add("Defies", "2To confront with assured power of resistance: disregard");
        definitions.Add("Ordeal", "2A severe trial or experience");
        definitions.Add("Impostor", "1One that assumes false identity or title for the purpose of deception");
        definitions.Add("Sacrifice", "2To suffer loss of, give up, renounce, injure, or destroy especially for an ideal, belief, or end");
        definitions.Add("Alignment", "1The act of aligning or state of being aligned, especially the proper positioning or state of adjustment of parts (as of a mechanical or electronic device) in relation to each other");
        definitions.Add("Anomaly", "1Something different, abnormal, peculiar, or not easily classified");
        definitions.Add("Debris", "1An accumulation of fragments of rock in geology");
        definitions.Add("Programmer", "1One that programs, such as a person who prepares and tests programs for devices (such as computers)");
        definitions.Add("Phantom", "1Something apparent to sense but with no substantial existence; apparition");
        definitions.Add("Democracy", "1Government by the people; rule of the majority");
        definitions.Add("Diagonalize", "2To put (a matrix) in a form with all the nonzero elements along the diagonal from upper left to lower right");
        definitions.Add("Hovercraft", "1A vehicle that is supported above the surface of land or water by a cushion of air produced by downwardly directed fans");
        definitions.Add("Reboot", "2To shut down and restart (a computer or program)");
        definitions.Add("Recognize", "2To acknowledge or take notice of in some definite way");
        definitions.Add("Gambling", "1The practice or activity of betting; the practice of risking money or other stakes in a game or bet");
        definitions.Add("Convict#1", "1A person convicted of and under sentence for a crime");
        definitions.Add("Convict#2", "2To find or prove to be guilty");
        definitions.Add("Calibration", "1The act or process of calibrating: the state of being calibrated");
        definitions.Add("Mouthpiece", "1A part (as of an instrument) that goes in the mouth or to which the mouth is applied");
        definitions.Add("Reciprocal", "1Either of a pair of numbers (such as 2/3 and 3/2 or 9 and 1/9) whose product is one");
        definitions.Add("Containment", "1The act, process, or means of keeping something within limits");
        definitions.Add("Humiliate", "2To make (someone) ashamed or embarrassed");
        definitions.Add("Continuously", "4In a continuous manner; without interruption");
        definitions.Add("Rejected", "2Refused to accept, consider, submit to, take for some purpose, or use");
        definitions.Add("Postfix", "3Characterized by placement of an operator after its operand or after its two operands if it is a binary operator");
        definitions.Add("Unambiguously", "4Not ambiguously; clearly, precisely");
        definitions.Add("Likelihood", "1The chance that something will happen; probability");
        definitions.Add("Numerator", "1The part of a fraction that is above the line and signifies the number to be divided by the denominator");
        definitions.Add("Biased", "3Tending to yield one outcome more frequently than others in a statistical experiment");
        definitions.Add("Pity", "1Sympathetic sorrow for one suffering, distressed, or unhappy");
        definitions.Add("Grok", "2To understand profoundly and intuitively");
        definitions.Add("Fragile", "3Easily broken or destroyed");
        definitions.Add("Sonar", "1A method or device for detecting and locating objects especially underwater by means of sound waves sent out to be reflected by the objects");
        definitions.Add("Circuitry", "1The detailed plan or arrangement of an electric circuit");
        definitions.Add("Sustenance", "1A supplying or being supplied with the necessaries of life");
        definitions.Add("Tincture", "1A substance that colors, dyes, or stains");
        definitions.Add("Mercenary", "1One that serves merely for wages, especially a soldier hired into foreign service");
        definitions.Add("Jackpot", "1The top prize in a game or contest (such as a lottery) that is typically a large fund of money formed by the accumulation of unwon prizes");
        definitions.Add("Cyanide", "1A compound of cyanogen with a more electropositive element or group");
        definitions.Add("Resembling", "2Being like or similar to");
        definitions.Add("Creativity", "1The ability to create");
        definitions.Add("Argument#1", "1A coherent series of reasons, statements, or facts intended to support or establish a point of view");
        definitions.Add("Argument#2", "1One of the independent variables upon whose value that of a function depends in mathematics");
        definitions.Add("Viable", "3Capable of working, functioning, or developing adequately");
        definitions.Add("Tesseract", "1The four-dimensional analogue of a cube");
        definitions.Add("Trios", "1Groups or sets of three");
        definitions.Add("Manor", "1The house or hall of an estate; mansion");
        definitions.Add("Firepower", "1The capacity (as of a military unit) to deliver effective fire on a target");
        definitions.Add("Acquire", "2To get as one's own; to come into possession or control of often by unspecified means");
        definitions.Add("Ambulance", "1A vehicle equipped for transporting the injured or sick");
        definitions.Add("Frail", "3Easily broken or destroyed; fragile");
        definitions.Add("Geometric", "3Of, relating to, or according to the methods or principles of geometry");
        definitions.Add("Obfuscating", "2Being evasive, unclear, or confusing");
        definitions.Add("Unauthorized", "3Not authorized; without authority or permission");
        definitions.Add("Accidentally", "4In an accidental or unintended manner; by accident");
        definitions.Add("Poorly", "4In a poor condition or manner, especially in an inferior or imperfect way; badly");
        definitions.Add("Insanity", "1A severely disordered state of the mind usually occurring as a specific disorder");
        definitions.Add("Interweave", "2To weave together");
        definitions.Add("Surname", "1The name borne in common by members of a family");
        definitions.Add("Vulgar", "3Lewdly or profanely indecent");
        definitions.Add("Exempt", "3Free or released from some liability or requirement to which others are subject");
        definitions.Add("Corrupted", "2Altered from the original or correct form or version");
        definitions.Add("Desert", "1Arid land with usually sparse vegetation, especially such land having a very warm climate and receiving less than 25 centimeters (10 inches) of sporadic rainfall annually");
        definitions.Add("Giggle", "2To laugh with repeated short catches of the breath");
        definitions.Add("Orphanage", "1An institution for the care of orphans");
        definitions.Add("Wife", "1A female partner in a marriage");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        #endregion
        // The commented-out lines above are templates for adding more words.

        // List out the modules used in this module.
        #region modules
        modules.Add("Supermassive Black Hole", "Accretion");
        modules.Add("S", "Increasingly,Remnants,Deviation");
        modules.Add("12trap", "Increasingly,Countermeasures,Unauthorized");
        modules.Add("Flowchart Madness", "Increasingly");
        modules.Add("Bamboozling Time Keeper", "Repetitive");
        modules.Add("Chicken Nuggets", "Homeless");
        modules.Add("The Sporglers", "Homeless");
        modules.Add("Spectre Maze","Coincidentally");
        modules.Add("Specter Maze","Pilfering,Manor");
        modules.Add("Bell of Tío", "Armrest");
        modules.Add("Cruel Boolean Wires","Incompetence");
        modules.Add("Stacked Deck","Disregard");
        modules.Add("Partitions", "Disregard");
        modules.Add("BizzFuzz", "Disregard");
        modules.Add("Memorable Buttons", "Disregard");
        modules.Add("The Time Keeper", "Disregard");
        modules.Add("Corridors","Disregard");
        modules.Add("D", "Disregard");
        modules.Add("The Xenocryst", "Disregard,Annihilate#2");
        modules.Add("Kuro","Disregard");
        modules.Add("Tip Toe", "Disregard");
        modules.Add("Bad Bones", "Disregard,Explicitly");
        modules.Add("FizzBuzz", "Disregard");
        modules.Add("Complex Keypad", "Disregard");
        modules.Add("Morse Buttons", "Disregard");
        modules.Add("Cruel Stars", "Disregard");
        modules.Add("The Wire", "Disregard");
        modules.Add("Color Morse", "Disregard");
        modules.Add("Heraldry", "Disregard,Tincture");
        modules.Add("Equations X", "Disregard,Numerator");
        modules.Add("Purchasing Properties", "Disregard");
        modules.Add("Match Refereeing", "Disregard,Annihilate#2,Sonar");
        modules.Add("Cruel Garfield Kart", "Disregard");
        modules.Add("Follow the Leader", "Disregard");
        modules.Add("Simon's Sums", "Bored,Recognize");
        modules.Add("Ultimate Custom Night", "Bored");
        modules.Add("The Klaxon", "Irrelevant");
        modules.Add("Simon Smothers", "Irrelevant");
        modules.Add("Quaternions", "Irrelevant,Explicitly");
        modules.Add("Type Racer", "Irrelevant");
        modules.Add("Pluto", "Irrelevant");
        modules.Add("Lines of Code", "Irrelevant,Halving,Argument#2");
        modules.Add("Modulo Maze", "Irrelevant,Remedy");
        modules.Add("Spangled Stars", "Irrelevant");
        modules.Add("Multicolored Digits", "Irrelevant");
        modules.Add("Video Poker", "Irrelevant,Competitive,Explicitly,Jackpot");
        modules.Add("Three Cryptic Steps", "Irrelevant");
        modules.Add("Etterna", "Anacrusis,Calibration");
        modules.Add("Quaver", "Customize");
        modules.Add("Busted Password", "Electrician,Desert");
        modules.Add("X-Radar", "Midpoint");
        modules.Add("The Navy Button", "Midpoint,Stencil,Cyclic,Alignment");
        modules.Add("Doofenshmirtz Evil Inc.", "Midpoint");
        modules.Add("Off-White Cipher", "Midpoint,Toroidal,Orphanage,Wife");
        modules.Add("Spongebob Patrick Squidward Sandy", "Midpoint,Traverse");
        modules.Add("OmegaForget", "Remnants,Accidentally");
        modules.Add("Hinges", "Sequentially");
        modules.Add("Coffeebucks", "Sequentially");
        modules.Add("Math 'em", "Sequentially");
        modules.Add("Roguelike Game", "Inventory,Viable");
        modules.Add("Adventure Game", "Inventory");
        modules.Add("Minecraft Survival", "Inventory");
        modules.Add("Dandy's Floors", "Inventory,Ichor");
        modules.Add("AMM-041-292", "Inventory,Composer");
        modules.Add("Shifting Maze", "Inventory");
        modules.Add("Bakery", "Inventory");
        modules.Add("Sickening Maze", "Inventory,Alignment");
        modules.Add("Suffering Maze", "Inventory,Alignment");
        modules.Add("Natures", "Competitive");
        modules.Add("Mission Identification", "Competitive");
        modules.Add("Wolf, Goat, and Cabbage", "Competitive");
        modules.Add("Dragon Energy", "Tertiary");
        modules.Add("Fursona", "Tertiary");
        modules.Add("Duck Konundrum", "Tertiary");
        modules.Add("Iron Lung", "Proximity");
        modules.Add("Melody Memory", "Proximity");
        modules.Add("Simon Sends", "Proximity,Rejected");
        modules.Add("Squeeze", "Halving");
        modules.Add("Crazy Talk With A K", "Toroidal");
        modules.Add("The Blue Button", "Toroidal,Cyclic");
        modules.Add("Lying Indicators", "Farce,Ordeal");
        modules.Add("Garfield Kart", "Factorial");
        modules.Add("Polynomial Solver", "Factorial");
        modules.Add("Alphabetical Ruling", "Factorial");
        modules.Add("Variety", "Factorial");
        modules.Add("Simpleton't", "Concentric");
        modules.Add("Marble Tumble", "Concentric");
        modules.Add("Worse Venn Diagram", "Concentric");
        modules.Add("The Crystal Maze", "Concentric,Whistle,Anomaly");
        modules.Add("Puzzle Pandemonium", "Explicit");
        modules.Add("Subway", "Explicit,Pretend#1");
        modules.Add("Discography", "Explicit");
        modules.Add("RNG Crystal", "Tolerated,Sacrifice,Biased");
        modules.Add("Bomb Diffusal", "Tolerated");
        modules.Add("The Midnight Motorist", "Illiterate");
        modules.Add("The Rule", "Automaton");
        modules.Add("The cRule", "Automaton");
        modules.Add("Game of Ants", "Automaton");
        modules.Add("Jupiter", "Explicitly");
        modules.Add("SYNC-125 [3]", "Explicitly");
        modules.Add("Hereditary Base Notation", "Explicitly");
        modules.Add("Identification Crisis", "Surly");
        modules.Add("Mission Control", "Misinterpretation");
        modules.Add("Classical Sense", "Composer");
        modules.Add("The Weakest Link", "Composer");
        modules.Add("Cruel Qualities", "Composer");
        modules.Add("Cheat Checkout", "Exploitation");
        modules.Add("Two Bits", "Primitive");
        modules.Add("Combination Lock", "Periodically");
        modules.Add("Cube Synchronization", "Periodically,Acquire");
        modules.Add("Lombax Cubes", "Periodically");
        modules.Add("Backdoor Hacking", "Periodically");
        modules.Add("Breakfast Egg", "Periodically");
        modules.Add("The Azure Button", "Cyclic");
        modules.Add("Colour Shuffle", "Cyclic");
        modules.Add("The Aqua Button", "Cyclic");
        modules.Add("4D Maze", "Cyclic");
        modules.Add("The Glitched Button", "Cyclic");
        modules.Add("Out of Time", "Cyclic");
        modules.Add("3D Maze", "Cyclic");
        modules.Add("Reordered Keys", "Cyclic");
        modules.Add("Simon Signals", "Cyclic");
        modules.Add("Beanboozled Again", "Cyclic");
        modules.Add("Simon Subdivides", "Undivided");
        modules.Add("RPS Judging", "Whistle");
        modules.Add("Office Job", "Alphanumeric");
        modules.Add("Moddle", "Alphanumeric");
        modules.Add("Polymodule", "Alphanumeric");
        modules.Add("Passport Control", "Alphanumeric,Citation");
        modules.Add("Not Port Check", "Alphanumeric");
        modules.Add("F", "Alphanumeric");
        modules.Add("Dimension Disruption", "Alphanumeric");
        modules.Add("Gryphons", "Alphanumeric");
        modules.Add("Rhythms", "Alphanumeric");
        modules.Add("Mazeswapper", "Alphanumeric");
        modules.Add("Light Grid", "Alphanumeric");
        modules.Add("Alpha-Bits", "Alphanumeric");
        modules.Add("Repo Selector", "Alphanumeric");
        modules.Add("Copper-9", "Alphanumeric");
        modules.Add("Main Page", "Alphanumeric");
        modules.Add("Numerical Nightmare", "Alphanumeric");
        modules.Add("Bandboozled Again", "Alphanumeric,Mouthpiece");
        modules.Add("The Hyperlink", "Alphanumeric");
        modules.Add("Toon Enough", "Alphanumeric");
        modules.Add("27,644,437", "Alphanumeric");
        modules.Add("Orange Hexabuttons", "Alphanumeric");
        modules.Add("Simon Supports", "Opinion");
        modules.Add("Yellow Arrows", "Screwdriver");
        modules.Add("Splitting The Loot", "Divvied");
        modules.Add("The Necronomicon", "Disturbance");
        modules.Add("Ed Balls", "Portrait");
        modules.Add("Color Blindness", "Portrait");
        modules.Add("Street Fighter", "Portrait");
        modules.Add("Cruel Ed Balls", "Portrait");
        modules.Add("The Hangover", "Elixir");
        modules.Add("Guitar Chords", "Capo");
        modules.Add("Blackjack", "Bankrupt#2");
        modules.Add("Free Parking", "Bankrupt#1");
        modules.Add("Weezer", "Snippet");
        modules.Add("Death Note", "Snippet");
        modules.Add("Web Design", "Snippet");
        modules.Add("The Samsung", "Snippet");
        modules.Add("Deceptive Rainbow Arrows", "Snippet");
        modules.Add("Overclock", "Pretend#1");
        modules.Add("Twodoku", "Pretend#2,Geometric");
        modules.Add("Blue Huffman Cipher", "Pretend#2");
        modules.Add("Connected Monitors", "Pretend#2");
        modules.Add("Yellow Huffman Cipher", "Pretend#2");
        modules.Add("Mischmodul", "Flickering");
        modules.Add("Star Navigator", "Flickering");
        modules.Add("Faulty Buttons", "Flickering");
        modules.Add("Square Button", "Flickering");
        modules.Add("Purgatory", "Flickering");
        modules.Add("The Impostor", "Flickering,Giggle");
        modules.Add("The World's Largest Button", "Flickering");
        modules.Add("Module Sprint", "Flickering");
        modules.Add("The Hexabutton", "Flickering");
        modules.Add("Cyan Button't", "Deviation");
        modules.Add("Geometry", "Deviation");
        modules.Add("Eavesdropping", "Printer");
        modules.Add("The Fuse Box", "Printer");
        modules.Add("Simon Senses", "Traverse");
        modules.Add("Voronoi Maze", "Traverse");
        modules.Add("Amusement Parks", "Traverse");
        modules.Add("Not The Screw", "Traverse");
        modules.Add("Stoichiometry", "Traverse,Accidentally");
        modules.Add("Cyan Arrows", "Haywire,Annihilate#2");
        modules.Add("Neutrinos", "Annihilate#1");
        modules.Add("Increasing Indices", "Torture,Insanity");
        modules.Add("Labyrinth Madness", "Torture");
        modules.Add("Sorting", "Defies");
        modules.Add("Among the Colors", "Impostor");
        modules.Add("Phosphorescence", "Impostor,Sacrifice");
        modules.Add("Shortcuts", "Alignment");
        modules.Add("Dimension King", "Alignment");
        modules.Add("Albuquerque", "Anomaly");
        modules.Add("Netherite", "Debris");
        modules.Add("Logging", "Programmer");
        modules.Add("Markscript", "Programmer");
        modules.Add("Hold Ups", "Phantom");
        modules.Add("Gerrymandering", "Democracy");
        modules.Add("Matrices", "Diagonalize,Poorly");
        modules.Add("3D Tunnels", "Hovercraft");
        modules.Add("4D Tunnels", "Hovercraft");
        modules.Add("The Matrix", "Reboot");
        modules.Add("Intervals", "Recognize");
        modules.Add("SUSadmin", "Recognize");
        modules.Add("Epic Shapes", "Recognize");
        modules.Add("The Twin", "Recognize");
        modules.Add("Forget's Ultimate Showdown", "Recognize");
        modules.Add("Daniel Dice", "Gambling,Jackpot");
        modules.Add("Luigi Poker", "Gambling");
        modules.Add("ID Exchange", "Convict#1");
        modules.Add("Identity Parade", "Convict#2");
        modules.Add("Pointless Machines", "Calibration");
        modules.Add("ReGrettaBle Relay", "Reciprocal");
        modules.Add("Base On", "Reciprocal,Numerator");
        modules.Add("Huffman Coding", "Reciprocal");
        modules.Add("Inside", "Containment");
        modules.Add("Old AI", "Containment,Continuously");
        modules.Add("Base Off", "Humiliate");
        modules.Add("UFO Satellites", "Continuously");
        modules.Add("Forget Me Maybe", "Continuously");
        modules.Add("Two Knobs", "Continuously");
        modules.Add("Light Cycle", "Continuously");
        modules.Add("Forgetle", "Continuously");
        modules.Add("Robit Programming", "Continuously");
        modules.Add("Stroop's Test", "Continuously");
        modules.Add("Remember Me Now", "Rejected");
        modules.Add("Reverse Polish Notation", "Postfix");
        modules.Add("OmegaDestroyer", "Unambiguously,Likelihood,Unauthorized");
        modules.Add("Pathfinder", "Likelihood");
        modules.Add("The Orange Button", "Numerator");
        modules.Add("Decay", "Numerator");
        modules.Add("Egyptian Fractions", "Numerator");
        modules.Add("Frightened Ghost Movement", "Biased");
        modules.Add("Long Words", "Pity");
        modules.Add("The Yellow Button", "Grok");
        modules.Add("A Mistake", "Fragile,Circuitry,Accidentally");
        modules.Add("Echolocation", "Sonar");
        modules.Add("Tangrams", "Circuitry");
        modules.Add("Spelling Buzzed", "Circuitry,Vulgar");
        modules.Add("Obama Grocery Store", "Sustenance");
        modules.Add("Alcoholic Rampage", "Mercenary");
        modules.Add("Mortal Kombat", "Mercenary");
        modules.Add("Flamin' Finger", "Jackpot");
        modules.Add("Dr. Doctor", "Cyanide");
        modules.Add("UltraStores", "Resembling");
        modules.Add("The Octadecayotton", "Resembling");
        modules.Add("Painting", "Creativity");
        modules.Add("Hand Turkey", "Creativity");
        modules.Add("Battle of Wits", "Argument#1");
        modules.Add("The Legendre Symbol", "Argument#2");
        modules.Add("Orientation Hypercube", "Tesseract");
        modules.Add("Spiderman 2004", "Tesseract");
        modules.Add("Three Bits", "Trios,Poorly");
        modules.Add("Not Murder", "Manor");
        modules.Add("Gadgetron Vendor", "Firepower,Acquire");
        modules.Add("Forget Perspective", "Acquire");
        modules.Add("Again", "Acquire");
        modules.Add("2048", "Acquire");
        modules.Add("Cartiac Arrest", "Ambulance");
        modules.Add("Sword of Damocles", "Frail");
        modules.Add("Simon Swindles", "Obfuscating");
        modules.Add("Password Destroyer", "Unauthorized");
        modules.Add("Royal Piano Keys", "Accidentally");
        modules.Add("Buddy Bidding", "Accidentally");
        modules.Add("Gourmet Hamburger", "Accidentally");
        modules.Add("Megum", "Insanity");
        modules.Add("Elder Futhark", "Interweave");
        modules.Add("Judgement", "Surname");
        modules.Add("Tax Returns", "Surname");
        modules.Add("Odd One Out", "Surname");
        modules.Add("Watch the Clock", "Exempt");
        modules.Add("The Board Walk", "Exempt");
        modules.Add("Encrypted Hangman", "Exempt");
        modules.Add("Masher The Bottun", "Exempt");
        modules.Add("Entry Number One", "Corrupted");
        modules.Add("Entry Number Four", "Corrupted");
        modules.Add("Solve/Strike", "Corrupted");
        modules.Add("Mistranslated Venting Gas", "Corrupted");
        modules.Add("Password Mutilator EX", "Corrupted");
        modules.Add("Retirement", "Wife");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        //modules.Add("", "");
        #endregion

        //Debug.LogFormat("[Terminology #{0}] The wordlist is: {1}.",_moduleID, definitions.Select(x => x.Key.Contains("#") ? x.Key.Substring(0, x.Key.Length - 2) : x.Key).Distinct().OrderBy(x => x).Join(", "));
        //Debug.LogFormat("[Terminology #{0}] The wordlist is: {1}.", _moduleID, definitions.Select(x => x.Key).Distinct().Join("\n"));
        //Debug.LogFormat("[Terminology #{0}] It is of length {1}.", _moduleID, definitions.Count);

        //string s = definitions.Where(x => !"1234".Contains(x.Value[0])).Select(x => x.Key).OrderBy(x => x).Join(", ");

        // Detect if any words lack a part of speech (testing).
        //if (s != "") Debug.LogFormat("[Terminology #{0}] The following words lack a selected part of speech: {1}", _moduleID, s);

        StartCoroutine(InitializeModule());

        // Set all module textures to black.
        foreach (MeshRenderer m in moduleIcons)
        {
            m.material.color = new Color32(0, 0, 0, 255);
        }

        foreach (MeshRenderer LED in stageLEDs)
        {
            LED.material = stageMaterials[0];
        }

        borderH = 0f;
        borderS = 0f;
        borderA = 1f;
    }

    private IEnumerator InitializeModule()
    {
        moduleText.text = "";
        yield return new WaitForSeconds(1f);
        StartCoroutine(GenerateText(startupTexts.PickRandom(), 0));
        yield return new WaitUntil(() => generated);
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => moduleActivated && repoDataRetrievalCompleted && iconRetrievalCompleted);
        if(repoDataRetrievalFailed || iconRetrievalFailed) // If we fail to retrieve the icons...
        {
            queryFailed = true;
            // Replace the current list with one that consists of the built-in modules.
            modules = modules.Where(x => defaultNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //Debug.LogFormat("[Terminology #{0}] The default modules are: \"{1}\".", _moduleID, modules.Select(x => x.Key).Join(", "));
            //Debug.LogFormat("[Terminology #{0}] There are {1} provided mods.", _moduleID, modules.Count());

            // Remove all unnecessary words.
            string[] defaultWords = modules.Select(x => x.Value).Join(",").Split(',');
            definitions = definitions.Where(x => defaultWords.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            //Debug.LogFormat("[Terminology #{0}] The default definitions are: \"{1}\".", _moduleID, definitions.Select(x => x.Key).Join(", "));
        }

        StartCoroutine(GenerateStage());
    }

    private IEnumerator GenerateText(string generatedText, int part)
    {
        //Debug.LogFormat("[Terminology #{0}] Testing module.", _moduleID);
        for (byte i = 255; i > 0; i-=15)
        {
            if (borderS != 0f) borderS = i / 510f;
            borderA = i / 255f;
            yield return new WaitForSeconds(1f / 30);
        }
        moduleText.text = "";
        borderA = 1f;
        borderS = 0f;
        yield return new WaitForSeconds(0.2f);
        switch (part)
        {
            case 0: // Nothing
            case 1: // Noun
                borderH = 0.0f; // Red
                break;
            case 2: // Verb
                borderH = 1f/6; // Yellow
                break;
            case 3: // Adjective
                borderH = 2f / 6; // Green
                break;
            case 4: // Adverb
                borderH = 4f / 6; // Blue
                break;
        }

        for (int i = 0; i <= generatedText.Length; i++)
        {
            if (part != 0) borderS = ((float) i) / (generatedText.Length * 2);
            moduleText.text = generatedText.Substring(0, i);
            if (i % 2 == 0) Audio.PlaySoundAtTransform("Blue's Lines", transform);
            yield return new WaitForSeconds(1f/30);
        }
        generated = true;
        yield break;
    }
    private IEnumerator GenerateStage()
    {
        Debug.LogFormat("[Terminology #{0}] The default modules are: \"{1}\".", _moduleID, modules.Select(x => x.Key).Join(", "));

        // Step 1: Generate a word.
        KeyValuePair<string, string> word = definitions.PickRandom();
        stageText = word.Value.Substring(1);
        Debug.LogFormat("[Terminology #{0}] The module's text is: \"{1}\".", _moduleID, stageText);

        string baseWord = word.Key.Contains("#") ? word.Key.Substring(0, word.Key.Length - 2) : word.Key; // Remove a hashtag if present.
        Debug.LogFormat("[Terminology #{0}] The corresponding word is \"{1}\", which is a{3} {2}.", _moduleID, baseWord, partsOfSpeech["1234".IndexOf(word.Value[0])],
            "1234".IndexOf(word.Value[0]) > 1 ? "n" : "");
        
        generated = false;
        StartCoroutine(GenerateText(stageText, "01234".IndexOf(word.Value[0])));
        yield return new WaitUntil(() => generated);
        yield return new WaitForSeconds(0.2f);

        Dictionary<string, string> modulesCopy = new Dictionary<string, string>(modules); // Make a copy of the "modules" dictionary.

        // Step 2: Generate modules based on that word.
        // 2a: Generate one module that has the word.
        displayedModules[0] = modulesCopy.Where(x => x.Value.Split(',').Contains(word.Key)).PickRandom().Key;
        answer = displayedModules[0];

        Debug.LogFormat("[Terminology #{0}] The correct module is {1}.", _moduleID, displayedModules[0]);
        // 2b: Generate four modules that don't have the word.

        // 2b1: Make a Dictionary ONLY consisting of modules that don't contain the word.
        modulesCopy = modulesCopy.Where(x => !x.Value.Split(',').Select(y => y.Contains("#") ? y.Substring(0, y.Length - 2) : y).Contains(baseWord))
            .ToDictionary(x => x.Key, x => x.Value);

        // 2b2: Using this new Dictionary, we can generate modules.
        for (int i = 1; i < 5; i++)
        {
            displayedModules[i] = modulesCopy.PickRandom().Key;
            modulesCopy.Remove(displayedModules[i]);
        }

        // Step 3: Scramble the list of modules.
        displayedModules = displayedModules.Shuffle();

        yield return new WaitForSeconds(0.5f);

        Debug.LogFormat("[Terminology #{0}] The displayed modules are {1}.", _moduleID, displayedModules.Take(4).Join(", ") + ", and " + displayedModules[4]);
        // Step 4: Using the modules we currently have, convert them to textures for use in the module.
        GetIcon(displayedModules[0]); // Dummy call because otherwise the first image on the module can be bugged

        for (int i = 0; i < 5; i++)
        {
            moduleIcons[i].material.mainTexture = GetIcon(displayedModules[i]);
            moduleIcons[i].material.mainTexture.filterMode = FilterMode.Point;
            Audio.PlaySoundAtTransform("Accept", transform);
            for (int j = 0; j < 6; j++)
            {
                moduleIcons[i].material.color = new Color(j / 5f, j / 5f, j / 5f);
                yield return new WaitForSeconds(1f / 30);
            }
        }

        isInteractable = true; // We can now safely interact with the module.
        yield break;
    }

    private IEnumerator PressButton(KMSelectable button)
    {
        int tempPress = 0; // Button that is pressed
        for (int i = 0; i < 5; i++)
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

        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        button.AddInteractionPunch(0.5f);
        if (!isInteractable) yield break; // Do nothing if interacting

        if (displayedModules[tempPress] == answer)
        {
            if (stage != 3) // Advance stage normally
            {
                stage++;
                Debug.LogFormat("[Terminology #{0}] You pressed {1}, which is correct. Advancing to stage {2}...", _moduleID, displayedModules[tempPress], stage);
                stageLEDs[stage - 2].material = stageMaterials[1];
                isInteractable = false;
                for (int i = 0; i < 3; i++)
                {
                    moduleIcons[tempPress].material.color = new Color(0, 0, 0);
                    yield return new WaitForSeconds(1f / 15);
                    moduleIcons[tempPress].material.color = new Color(1, 1, 1);
                    if (i < 2) yield return new WaitForSeconds(1f / 15);
                }

                yield return new WaitForSeconds(0.2f);

                for (int i = 0; i < 5; i++)
                {
                    Audio.PlaySoundAtTransform("Accept", transform);
                    for (int j = 5; j >= 0; j--)
                    {
                        moduleIcons[i].material.color = new Color(j / 5f, j / 5f, j / 5f);
                        yield return new WaitForSeconds(1f / 30);
                    }
                }

                yield return new WaitForSeconds(0.2f);

                StartCoroutine(GenerateStage());
            }
            else // Solve the module
            {
                isInteractable = false;
                stageLEDs[2].material = stageMaterials[1];

                for (int i = 0; i < 3; i++)
                {
                    moduleIcons[tempPress].material.color = new Color(0, 0, 0);
                    yield return new WaitForSeconds(1f / 15);
                    moduleIcons[tempPress].material.color = new Color(1, 1, 1);
                    if (i < 2) yield return new WaitForSeconds(1f / 15);
                }

                yield return new WaitForSeconds(0.2f);

                for (int i = 0; i < 5; i++)
                {
                    Audio.PlaySoundAtTransform("Accept", transform);
                    for (int j = 5; j >= 0; j--)
                    {
                        moduleIcons[i].material.color = new Color(j / 5f, j / 5f, j / 5f);
                        yield return new WaitForSeconds(1f / 30);
                    }
                }

                yield return new WaitForSeconds(0.2f);

                generated = false;
                StartCoroutine(GenerateText(solveTexts.PickRandom(), 0));
                yield return new WaitUntil(() => generated);
                yield return new WaitForSeconds(0.2f);
                int[] displayedInts = new int[] {8, 22, 6, 13, 21}; // Icon indices
                for (int i = 0; i < 5; i++)
                {
                    moduleIcons[i].material.mainTexture = defaultIcons[displayedInts[i]];
                    Audio.PlaySoundAtTransform("Accept", transform);
                    for (int j = 0; j < 6; j++)
                    {
                        moduleIcons[i].material.color = new Color(j / 5f, j / 5f, j / 5f);
                        yield return new WaitForSeconds(1f / 30);
                    }
                }
                yield return new WaitForSeconds(0.2f);
                Module.HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            }
        }
        else // Strike on incorrect press
        {
            Debug.LogFormat("[Terminology #{0}] You pressed {1}, which is incorrect. Strike!", _moduleID, displayedModules[tempPress]);
            Module.HandleStrike();
            isInteractable = false; // Prevent interaction while the button is flashing
            for (int i = 0; i < 3; i++)
            {
                moduleIcons[tempPress].material.color = new Color(0, 0, 0);
                yield return new WaitForSeconds(1f / 15);
                moduleIcons[tempPress].material.color = new Color(1, 1, 1);
                if (i < 2) yield return new WaitForSeconds(1f / 15);
            }
            isInteractable = true;
        }
        yield break;
    }

    private IEnumerator RetrieveRepoData()
    {
        try
        {
            Debug.LogFormat("[Terminology #{0}] Attempting to retrieve repo data...", _moduleID);
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture("https://ktane.timwi.de/json/raw"))
            {
                yield return req.SendWebRequest();
                if (req.isHttpError || req.isNetworkError)
                {
                    iconRetrievalFailed = true;
                    Debug.LogFormat("[Terminology #{0}] ERROR: Failed to retrieve repository data. Returning to original icons.", _moduleID);
                    yield break;
                }

                // Deserialize JSON data
                _modules = JsonConvert.DeserializeObject<KTANEModuleResult>(req.downloadHandler.text).KTANEModules;
                Debug.LogFormat("[Terminology #{0}] Repository data successfully retrieved. Number of modules that exist: {1}.", _moduleID, _modules.Length);
            }
        }
        finally
        {
            repoDataRetrievalCompleted = true;
        }
    }
    private IEnumerator RetrieveIcons() // Yes, I did rely on Tricon for this thing
    {
        try
        {
            Debug.LogFormat("[Terminology #{0}] Attempting to retrieve icons...", _moduleID);
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture("https://ktane.timwi.de/iconsprite"))
            {
                yield return req.SendWebRequest();
                if (req.isHttpError || req.isNetworkError)
                {
                    iconRetrievalFailed = true;
                    Debug.LogFormat("[Terminology #{0}] ERROR: Failed to retrieve repository icons. Returning to original icons.", _moduleID);
                    yield break;
                }

                // Deserialize JSON data
                iconSprite = DownloadHandlerTexture.GetContent(req);
                Debug.LogFormat("[Terminology #{0}] Repository icons successfully retrieved. The size: {1}x{2}.", _moduleID, iconSprite.width, iconSprite.height);
            }
        }
        finally
        {
            iconRetrievalCompleted = true;
        }
    }

    private Texture2D GetIcon(string moduleName)
    {
        Texture2D result;
        if (!queryFailed) // If we successfully query the repository...
        {
            KTANEModule module = _modules.First(mod => mod.Name == moduleName);
            Color[] pixels = iconSprite.GetPixels(32 * module.IconX, iconSprite.height - 32 * (module.IconY + 1), 32, 32);
            result = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            result.SetPixels(pixels);
        }
        else // If we failed to query the repository...
        {
            result = defaultIcons.First(x => x.name == moduleName);
        }
        return result;
    }

    // Update is called once per frame
    void Update () {
        Color c = Color.HSVToRGB(borderH, 0 + borderS, 0.5f + borderS);
        definitionBorder.material.color = c;
        moduleText.color = new Color(c.r, c.g, c.b, borderA);
    }
} // End of main class

// Yes, I did rely on Tricon for this thing
[Serializable]
public class KTANEModuleResult
{
    [JsonProperty("KtaneModules")]
    public KTANEModule[] KTANEModules { get; private set; }
}
[Serializable]
public class KTANEModule
{
    [JsonProperty("ModuleID")]
    public string ModuleID { get; private set; }

    [JsonProperty("Name")]
    public string Name { get; private set; }

    [JsonProperty("X")]
    public int IconX { get; private set; }

    [JsonProperty("Y")]
    public int IconY { get; private set; }
}