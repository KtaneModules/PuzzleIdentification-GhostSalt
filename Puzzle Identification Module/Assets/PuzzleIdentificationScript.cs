using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using PuzzleIdentification;
using Rnd = UnityEngine.Random;

public class PuzzleIdentificationScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable ModuleSelectable;
    public KMSelectable[] Keys;
    public TextMesh[] KeyTexts;
    public MeshRenderer[] LEDs;
    public TextMesh Display;
    public TextMesh InputText;

    private KMAudio.KMAudioRef Sound = null;

    private KeyCode[] TypableKeys =
    {
        KeyCode.Escape, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Colon, KeyCode.Backspace,
        KeyCode.Tab, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.Exclaim, KeyCode.Question, KeyCode.Delete,
        KeyCode.CapsLock, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote, KeyCode.Return,
        KeyCode.LeftShift, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Slash, KeyCode.RightShift,
        KeyCode.LeftControl, KeyCode.LeftWindows, KeyCode.LeftAlt, KeyCode.Space, KeyCode.RightAlt, KeyCode.RightWindows, KeyCode.Menu, KeyCode.RightControl
    };

    private int[] UselessKeys = { 0, 14, 53, 54, 55, 57, 58, 59, 60 };
    private int[] LetterKeys = { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 29, 30, 31, 32, 33, 34, 35, 36, 37, 42, 43, 44, 45, 46, 47, 48 };
    private int[] SymbolKeys = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 25, 26, 38, 39, 49, 50, 51 };
    private int[] ChosenPuzzles = { -1, -1, -1, -1 };
    private int ChosenGame;
    private int ChosenPuzzle;
    private int Shifting;
    private int Stage;
    private float DefaultGameMusicVolume;
    private string[] PuzzleMusicNames = { "layton curious village puzzle", "layton pandora's box puzzle", "layton lost future puzzle", "layton spectre's call puzzle", "layton miracle mask puzzle", "layton azran legacy puzzle" };
    private string[] GameNames = { "Professor Layton and the Curious Village", "Professor Layton and Pandora's Box", "Professor Layton and the Lost Future", "Professor Layton and the Spectre's Call", "Professor Layton and the Miracle Mask", "Professor Layton and the Azran Legacy" };
    private string KeyboardUpper = "QWERTYUIOPASDFGHJKLZXCVBNM";
    private string KeyboardLower = "qwertyuiopasdfghjklzxcvbnm";
    private string Symbols = "1234567890-=!?:',./";
    private bool Active;
    private bool CapsOn;
    private bool Solved;
    private bool Striking;
    private bool PlayingMusic;
    private bool Focused;
    private bool TwitchPlays;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        try
        {
            DefaultGameMusicVolume = GameMusicControl.GameMusicVolume;
        }
        catch (Exception) { }
        Bomb.OnBombExploded += delegate {
            if (PlayingMusic)
                Sound.StopSound();
            try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { }
        };
        Bomb.OnBombSolved += delegate {
            Sound.StopSound();
            try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { }
        };
        Display.text = "";
        Module.OnActivate += delegate { Audio.PlaySoundAtTransform("activate", transform); Display.text = "#???";  };
        ModuleSelectable.OnFocus += delegate { Focused = true; if (Active && PlayingMusic && !TwitchPlays) { Sound = Audio.PlaySoundAtTransformWithRef(PuzzleMusicNames[ChosenGame], transform); try { GameMusicControl.GameMusicVolume = 0.0f; } catch (Exception) { } } };
        ModuleSelectable.OnDefocus += delegate { Focused = false; if (Active && PlayingMusic && !TwitchPlays) { Sound.StopSound(); try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { } } };
        if (Application.isEditor)
            Focused = true;
        for (int i = 0; i < Keys.Length; i++)
        {
            int x = i;
            Keys[i].OnInteract += delegate { StartCoroutine(KeyPress(x)); return false; };
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        for (int i = 0; i < TypableKeys.Count(); i++)
        {
            if (Input.GetKeyDown(TypableKeys[i]) && Focused)
            {
                if (Input.GetKey(TypableKeys[1]) && (Input.GetKey(TypableKeys[41]) || Input.GetKey(TypableKeys[52])))
                    Keys[25].OnInteract();
                else if (Input.GetKey(TypableKeys[51]) && (Input.GetKey(TypableKeys[41]) || Input.GetKey(TypableKeys[52])))
                    Keys[26].OnInteract();
                else if (Input.GetKey(TypableKeys[38]) && (Input.GetKey(TypableKeys[41]) || Input.GetKey(TypableKeys[52])))
                    Keys[38].OnInteract();
                else if (!Input.GetKey(TypableKeys[38]))
                    Keys[i].OnInteract();
            }
        }
    }

    void EasterEgg()
    {
        Audio.PlaySoundAtTransform("easter egg", Display.transform);
        InputText.text = "";
    }

    private IEnumerator KeyPress(int pos)
    {
        Audio.PlaySoundAtTransform("press", Keys[pos].transform);
        for (int i = 0; i < 4; i++)
        {
            Keys[pos].transform.localPosition -= new Vector3(0, 0.001f, 0);
            yield return null;
        }
        Keys[pos].AddInteractionPunch(0.25f);
        if (!Solved && !Striking)
        {
            if (pos == 40)
            {
                if (!Active)
                {
                    PlayingMusic = true;
                    Active = true;
                    ChosenGame = Rnd.Range(0, Data.PuzzleNames.Length);
                    GeneratePuzzleNumber:
                        ChosenPuzzle = Rnd.Range(0, Data.PuzzleNames[ChosenGame].Length);
                    if (ChosenPuzzles[0] == ChosenPuzzle)
                        if (ChosenPuzzles[2] == ChosenGame)
                            goto GeneratePuzzleNumber;
                    else if (ChosenPuzzles[1] == ChosenPuzzle)
                        if (ChosenPuzzles[3] == ChosenGame)
                            goto GeneratePuzzleNumber;
                    Display.text = "#" + (ChosenPuzzle + 1).ToString("000");
                    Sound = Audio.PlaySoundAtTransformWithRef(PuzzleMusicNames[ChosenGame], transform);
                    try { GameMusicControl.GameMusicVolume = 0.0f; } catch (Exception) { }
                    Debug.LogFormat("[Puzzle Identification #{0}] The module is now on stage {1}. The game chosen is {2} and the puzzle chosen is \"{3}\" ({4}).", _moduleID, Stage + 1, GameNames[ChosenGame], Data.PuzzleNames[ChosenGame][ChosenPuzzle], "#" + (ChosenPuzzle + 1).ToString("000"));
                }
                else
                {
                    if (InputText.text == Data.PuzzleNames[ChosenGame][ChosenPuzzle])
                    {
                        if (Stage != 2)
                        {
                            Audio.PlaySoundAtTransform("stage", Display.transform);
                            InputText.text = "";
                            PlayingMusic = false;
                            Sound.StopSound();
                            try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { }
                            LEDs[Stage].material.color = new Color(0, 1, 0);
                            ChosenPuzzles[Stage] = ChosenPuzzle;
                            ChosenPuzzles[Stage + 2] = ChosenGame;
                            Stage++;
                            Active = false;
                            Display.text = "#???";
                        }
                        else
                            StartCoroutine(Solve());
                    }
                    else if (InputText.text.ToLower() == "uwu" || InputText.text.ToLower() == "owo")
                        EasterEgg();
                    else
                        StartCoroutine(Strike());
                }
            }
            else if (!UselessKeys.Contains(pos) && Active)
            {
                if (pos == 56 && InputText.text.Length < 29)
                {
                    InputText.text += " ";
                    if (Shifting == 1)
                        Shifting++;
                }
                else if (pos == 13 && InputText.text.Length != 0)
                {
                    InputText.text = InputText.text.Remove(InputText.text.Length - 1);
                    if (Shifting == 1)
                        Shifting++;
                }
                else if (pos == 27 && InputText.text.Length != 0)
                {
                    InputText.text = "";
                    Audio.PlaySoundAtTransform("erase", Keys[13].transform);
                    if (Shifting == 1)
                        Shifting++;
                }
                else if (pos == 28 && Active)
                {
                    if (Shifting == 1)
                        Shifting++;
                    CapsOn = !CapsOn;
                    if (CapsOn)
                        for (int i = 0; i < 26; i++)
                            KeyTexts[LetterKeys[i]].text = KeyboardUpper[i].ToString();
                    else
                        for (int i = 0; i < 26; i++)
                            KeyTexts[LetterKeys[i]].text = KeyboardLower[i].ToString();
                }
                else if ((pos == 41 || pos == 52) && Active)
                    Shifting++;
                else if (pos != 13 && pos != 27 && InputText.text.Length < 29)
                {
                    InputText.text += KeyTexts[pos].text;
                    if (Shifting == 1)
                        Shifting++;
                }
                if (Shifting == 2)
                {
                    if (!CapsOn)
                        for (int i = 0; i < 26; i++)
                            KeyTexts[LetterKeys[i]].text = KeyboardLower[i].ToString();
                    Shifting = 0;
                }
                else if (Shifting == 1 && !CapsOn)
                    for (int i = 0; i < 26; i++)
                        KeyTexts[LetterKeys[i]].text = KeyboardUpper[i].ToString();
                else if (!CapsOn)
                    for (int i = 0; i < 26; i++)
                        KeyTexts[LetterKeys[i]].text = KeyboardLower[i].ToString();
            }
        }
        for (int i = 0; i < 4; i++)
        {
            Keys[pos].transform.localPosition += new Vector3(0, 0.001f, 0);
            yield return null;
        }
    }

    private IEnumerator Solve()
    {
        Module.HandlePass();
        Sound.StopSound();
        try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { }
        PlayingMusic = false;
        Audio.PlaySoundAtTransform("solve", Display.transform);
        LEDs[Stage].material.color = new Color(0, 1, 0);
        Active = false;
        Solved = true;
        Display.text = "#???";
        InputText.text = "";
        yield return new WaitForSeconds(0.25f);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
                LEDs[j].material.color = new Color(0, 0, 0);
            yield return new WaitForSeconds(0.25f);
            for (int j = 0; j < 3; j++)
                LEDs[j].material.color = new Color(0, 1, 0);
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator Strike()
    {
        Debug.LogFormat("[Puzzle Identification #{0}] You submitted {1}, which was incorrect. Strike!", _moduleID, InputText.text);
        Module.HandleStrike();
        Sound.StopSound();
        try { GameMusicControl.GameMusicVolume = DefaultGameMusicVolume; } catch (Exception) { }
        PlayingMusic = false;
        Audio.PlaySoundAtTransform("strike", Display.transform);
        Active = false;
        Striking = true;
        Display.text = "#???";
        InputText.text = "";
        Stage = 0;
        ChosenPuzzles = new int[]{ -1, -1, -1, -1 };
        for (int i = 0; i < 3; i++)
            LEDs[i].material.color = new Color(1, 0, 0);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 3; i++)
            LEDs[i].material.color = new Color(0, 0, 0);
        Striking = false;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} Text HERE' to type 'Text HERE' into the module. Use '!{0} #' to clear your input and '!{0} *' to press the Enter key.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        TwitchPlays = true;
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            if (KeyboardUpper.Contains(command[i]))
            {
                if (!CapsOn)
                {
                    Keys[28].OnInteract();
                    yield return new WaitForSeconds(0.05f);
                }
                Keys[LetterKeys[KeyboardUpper.IndexOf(command[i])]].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else if (KeyboardLower.Contains(command[i]))
            {
                if (CapsOn)
                {
                    Keys[28].OnInteract();
                    yield return new WaitForSeconds(0.05f);
                }
                Keys[LetterKeys[KeyboardLower.IndexOf(command[i])]].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else if (command[i] == ' ')
            {
                Keys[56].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else if (command[i] == '*')
            {
                Keys[40].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else if (command[i] == '#')
            {
                Keys[27].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else if (Symbols.Contains(command[i]))
            {
                Keys[SymbolKeys[Symbols.IndexOf(command[i])]].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                yield return "sendtochaterror Invalid command: Cannot type the character " + command[i] + ".";
                yield break;
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!Solved)
        {
            yield return true;
            if (!Active)
            {
                Keys[40].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                Type:
                    for (int i = 0; i < Data.PuzzleNames[ChosenGame][ChosenPuzzle].Length; i++)
                    {
                        if (!Solved)
                        {
                            if (KeyboardUpper.Contains(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i]))
                            {
                                if (!CapsOn)
                                {
                                    Keys[28].OnInteract();
                                    yield return new WaitForSeconds(0.05f);
                                }
                                Keys[LetterKeys[KeyboardUpper.IndexOf(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i])]].OnInteract();
                                yield return new WaitForSeconds(0.05f);
                            }
                            else if (KeyboardLower.Contains(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i]))
                            {
                                if (CapsOn)
                                {
                                    Keys[28].OnInteract();
                                    yield return new WaitForSeconds(0.05f);
                                }
                                Keys[LetterKeys[KeyboardLower.IndexOf(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i])]].OnInteract();
                                yield return new WaitForSeconds(0.05f);
                            }
                            else if (Data.PuzzleNames[ChosenGame][ChosenPuzzle][i] == ' ')
                            {
                                Keys[56].OnInteract();
                                yield return new WaitForSeconds(0.05f);
                            }
                            else if (Symbols.Contains(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i]))
                            {
                                Keys[SymbolKeys[Symbols.IndexOf(Data.PuzzleNames[ChosenGame][ChosenPuzzle][i])]].OnInteract();
                                yield return new WaitForSeconds(0.05f);
                            }
                            else
                            {
                                yield return "sendtochaterror Something went wrong. Weird.";
                                yield break;
                            }
                        }
                    }
                if (!Solved)
                {
                    yield return new WaitForSeconds(0.05f);
                    if (InputText.text == Data.PuzzleNames[ChosenGame][ChosenPuzzle])
                        Keys[40].OnInteract();
                    else
                    {
                        Keys[27].OnInteract();
                        yield return new WaitForSeconds(0.05f);
                        goto Type;
                    }
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
    }
}
