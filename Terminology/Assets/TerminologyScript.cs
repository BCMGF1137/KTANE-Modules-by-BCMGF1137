using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;
using UnityEngine;
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
    public TextMesh moduleText;
    public Renderer moduleTextRenderer;

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
    private bool moduleActivated = false;

    private string[] startupTexts = new string[] // What you see before the module generates
    {
        // Steam preview image
        "This is the sixth module I've uploaded so far, good luck! -BCMGF1137/19#5398",

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
    };

    private bool repoDataRetrievalFailed = false; // Did we fail to retrieve repo data?
    private bool repoDataRetrievalCompleted = false; // Did we finish the repo data retrieval process?
    private bool iconRetrievalFailed = false; // Did we fail to retrieve icons?
    private bool iconRetrievalCompleted = false; // Did we finish the icon retrieval process?

    private KTANEModule[] _modules;
    private Texture2D iconSprite;

    private void Awake()
    {
        _moduleID = _moduleID++;
        StartCoroutine(RetrieveRepoData());
        StartCoroutine(RetrieveIcons());

        Module.OnActivate += delegate
        {
            moduleActivated = true;
        };
    }

    // Use this for initialization
    void Start () {
        // Initialize the definitions.

        // Initial message: This is the sixth module I've uploaded so far, good luck! -BCMGF1137/19#5398
        #region definitions
        definitions.Add("Accretion", "1The process of growth or enlargement by a gradual buildup: such as an increase by external addition or accumulation (as by adhesion of external parts or particles).");
        definitions.Add("Increasingly", "4To an increasing degree.");
        definitions.Add("Repetitive", "3Containing repetition.");
        definitions.Add("Homeless", "3Having no home or permanent place of residence.");
        definitions.Add("Coincidentally", "4In a coincidental manner; by coincidence");
        definitions.Add("Pilfering", "3Stealing stealthily in small amounts and often again and again");
        definitions.Add("Armrest", "1A support for the arm");
        definitions.Add("Incompetence", "1The state or fact of being incompetent");
        definitions.Add("Disregard", "2To pay no attention to; treat as unworthy of regard or notice");
        definitions.Add("Bored", "1Filled with or characterized by boredom");
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
        definitions.Add("Distract", "2To draw or direct (something, such as someone's attention) to a different object or in different directions at the same time");
        definitions.Add("Factorial", "1The product of all the positive integers from 1 to n");
        definitions.Add("Concentric", "3Having a common center");
        definitions.Add("Explicit", "3Fully revealed or expressed without vagueness, implication, or ambiguity; leaving no question as to meaning or intent.");
        definitions.Add("Tolerated", "3To allow to be or to be done without prohibition, hindrance, or contradiction.");
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
        definitions.Add("Haywire", "3being out of order or having gone wrong");
        definitions.Add("Annihilate#1", "2To cause (something, such as a particle and its antiparticle) to vanish or cease to exist by coming together and changing into other forms of energy (such as photons)");
        definitions.Add("Annihilate#2", "2To cause to cease to exist; to do away with entirely so that nothing remains");
        definitions.Add("Torture", "2To cause intense suffering to; torment");
        definitions.Add("Defies", "2To confront with assured power of resistance : disregard");
        definitions.Add("Ordeal", "2A severe trial or experience");
        definitions.Add("Impostor", "1One that assumes false identity or title for the purpose of deception");
        definitions.Add("Sacrifice", "2To suffer loss of, give up, renounce, injure, or destroy especially for an ideal, belief, or end");
        definitions.Add("Alignment", "1The act of aligning or state of being aligned, especially the proper positioning or state of adjustment of parts (as of a mechanical or electronic device) in relation to each other");
        definitions.Add("Anomaly", "1Something different, abnormal, peculiar, or not easily classified");
        definitions.Add("Constitution", "1The basic principles and laws of a nation, state, or social group that determine the powers and duties of the government and guarantee certain rights to the people in it");
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
        definitions.Add("Calibration", "1The act or process of calibrating : the state of being calibrated");
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
        //definitions.Add("?", "??");
        //definitions.Add("?", "??");
        #endregion
        // The commented-out lines above are templates for adding more words.

        // List out the modules used in this module.
        #region modules
        modules.Add("Supermassive Black Hole", "Accretion");
        modules.Add("S", "Increasingly,Remnants,Deviation");
        modules.Add("12trap", "Increasingly,Countermeasures");
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
        modules.Add("Busted Password", "Electrician");
        modules.Add("X-Radar", "Midpoint");
        modules.Add("The Navy Button", "Midpoint,Stencil,Cyclic,Alignment");
        modules.Add("Doofenshmirtz Evil Inc.", "Midpoint");
        modules.Add("Off-White Cipher", "Midpoint,Toroidal");
        modules.Add("Spongebob Patrick Squidward Sandy", "Midpoint,Traverse");
        modules.Add("OmegaForget", "Remnants");
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
        modules.Add("Color-Symbolic Interpretation Module", "Distract");
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
        modules.Add("Twodoku", "Pretend#2");
        modules.Add("Blue Huffman Cipher", "Pretend#2");
        modules.Add("Connected Monitors", "Pretend#2");
        modules.Add("Yellow Huffman Cipher", "Pretend#2");
        modules.Add("Mischmodul", "Flickering");
        modules.Add("Star Navigator", "Flickering");
        modules.Add("Faulty Buttons", "Flickering");
        modules.Add("Square Button", "Flickering");
        modules.Add("Purgatory", "Flickering");
        modules.Add("The Impostor", "Flickering");
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
        modules.Add("Stoichiometry", "Traverse");
        modules.Add("Cyan Arrows", "Haywire,Annihilate#2");
        modules.Add("Neutrinos", "Annihilate#1");
        modules.Add("Increasing Indices", "Torture");
        modules.Add("Labyrinth Madness", "Torture");
        modules.Add("Sorting", "Defies");
        modules.Add("Among the Colors", "Impostor");
        modules.Add("Phosphorescence", "Impostor,Sacrifice");
        modules.Add("Shortcuts", "Alignment");
        modules.Add("Dimension King", "Alignment");
        modules.Add("Albuquerque", "Anomaly");
        modules.Add("Saul Goodman Button", "Constitution");
        modules.Add("Netherite", "Debris");
        modules.Add("Logging", "Programmer");
        modules.Add("Markscript", "Programmer");
        modules.Add("Hold Ups", "Phantom");
        modules.Add("Gerrymandering", "Democracy");
        modules.Add("Matrices", "Diagonalize");
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
        modules.Add("OmegaDestroyer", "Unambiguously,Likelihood");
        modules.Add("Pathfinder", "Likelihood");
        modules.Add("The Orange Button", "Numerator");
        modules.Add("Decay", "Numerator");
        modules.Add("Egyptian Fractions", "Numerator");
        modules.Add("Frightened Ghost Movement", "Biased");
        modules.Add("Long Words", "Pity");
        modules.Add("The Yellow Button", "Grok");
        modules.Add("A Mistake", "Fragile,Circuitry");
        modules.Add("Echolocation", "Sonar");
        modules.Add("Tangrams", "Circuitry");
        modules.Add("Spelling Buzzed", "Circuitry");
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
        modules.Add("Three Bits", "Trios");
        modules.Add("Not Murder", "Manor");
        modules.Add("Gadgetron Vendor", "Firepower,Acquire");
        modules.Add("Forget Perspective", "Acquire");
        modules.Add("Again", "Acquire");
        modules.Add("2048", "Acquire");
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
    }

    private IEnumerator InitializeModule()
    {
        moduleText.text = "";
        yield return new WaitForSeconds(1f);
        StartCoroutine(GenerateText(startupTexts.PickRandom()));
        yield return new WaitUntil(() => moduleActivated && repoDataRetrievalCompleted && iconRetrievalCompleted);
    }

    private IEnumerator GenerateText(string generatedText)
    {
        Debug.LogFormat("[Terminology #{0}] Testing module.", _moduleID);
        SetWordWrappedText(ref generatedText);
        Color32 t = moduleText.color;
        for (byte i = 255; i > 0; i-=15)
        {
            moduleText.color = new Color32(t.r, t.g, t.b, i);
            yield return new WaitForSeconds(1f / 30);
        }
        moduleText.text = "";
        moduleText.color = new Color32(t.r, t.g, t.b, 255);
        for (int i = 0; i <= generatedText.Length; i++)
        {
            moduleText.text = generatedText.Substring(0, i);
            if (i % 2 == 0) Audio.PlaySoundAtTransform("Blue's Lines", transform);
            yield return new WaitForSeconds(1f/30);
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
        KTANEModule module = _modules.First(mod => mod.Name == moduleName);
        Color[] pixels = iconSprite.GetPixels(32 * module.IconX, iconSprite.height - 32 * (module.IconY + 1), 32, 32);
        Texture2D result = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        return result;
    }

    private void SetWordWrappedText(ref string text)
    {
        
    }

    // Update is called once per frame
    void Update () {
		
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