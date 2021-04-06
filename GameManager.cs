using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState { standard, monitor, win, lose };

    [Header("Game State")]
    public GameState currentState;

    [Header("Monitor Switching")]
    [SerializeField] List<SpriteRenderer> standardSprites = new List<SpriteRenderer>(); // put any art that needs to be disabled during monitor phase
    [SerializeField] List<GameObject> lightsToDisable = new List<GameObject>();
    [SerializeField] List<GameObject> monitors = new List<GameObject>(); // monitor 0, monitor 1, and monitor 2
    [SerializeField] List<PlayerController> playerControllers = new List<PlayerController>();
    [SerializeField] List<GameObject> playerObjects = new List<GameObject>();
    [SerializeField] List<GamePlayer> gamePlayers = new List<GamePlayer>();
    [SerializeField] GameObject heartCanvas;

    [Header("Player Settings")]
    [SerializeField] int currentChar;
    [SerializeField] List<GameObject> roomLights = new List<GameObject>();

    public bool finishedLevel1 = false;
    public bool finishedLevel2 = false;
    public bool finishedLevel3 = false;

    [Header("Three Player Settings")]
    public bool threeCharactersInitialized = false;
    [SerializeField] Transform threeCharacterCameraPoint;
    [SerializeField] List<GameObject> doorTriggers = new List<GameObject>();
    bool initializedWin = false;

    [Header("Camera/Win Objects")]
    [SerializeField] Transform winPoint;
    [SerializeField] List<GameObject> newPlayers = new List<GameObject>();
    [SerializeField] AudioSource elevatorSound;
    [SerializeField] float elevatorSoundDuration;
    [SerializeField] ParticleSystem confetti;
    [SerializeField] GameObject winCanvas;

    [Header("Lose Objects")]
    [SerializeField] GameObject mainCanvas;
    [SerializeField] GameObject gameOverCanvas;
    [SerializeField] List<Transform> computerPositions = new List<Transform>();
    [SerializeField] float gameOverDelay; // how long to wait before activating game over canvas etc.
    [SerializeField] List<GameObject> computerScreens = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    private void Start()
    {
        currentState = GameState.standard;
        //ActivateMonitor(0);

        ActivateCharacter(currentChar);
    }

    private void Update()
    {
        if (currentState != GameState.lose)
        {
            if (GameOver())
            {
                if (!initializedWin)
                {
                    WonTheGame();
                    initializedWin = true;
                }
            }
        }
    }

    public void ActivateMonitor(int i)
    {
        currentState = GameState.monitor;
        MusicManager.instance.SwitchClip(MusicManager.instance.monitorSongs[i], .1f);
        CameraController.instance.target = null;
        SoundManager.instance.PlayKeyboardSound();
        foreach (SpriteRenderer sr in standardSprites) sr.enabled = false; // turn off standard art
        foreach (GameObject light in lightsToDisable) light.SetActive(false); // turn off standard lights
        foreach (PlayerController player in playerControllers) player.canMove = false; // disable player movement/input
        foreach (GamePlayer player in gamePlayers) player.currentHealth = 3;
        heartCanvas.SetActive(true);

        monitors[i].SetActive(true);
    }

    public void DeactivateMonitor(int i)
    {
        currentState = GameState.standard;
        foreach (SpriteRenderer sr in standardSprites) sr.enabled = true; // turn on standard art
        foreach (GameObject light in lightsToDisable) light.SetActive(true); // turn on standard lights
        foreach (PlayerController player in playerControllers) player.canMove = true; // enable player movement/input
        heartCanvas.SetActive(false);

        monitors[i].SetActive(false); // turn off monitor
    }

    public void ActivateCharacter(int i) // * use i == 3 special call to control all of the characters
    {
        if (i != 3)
        {
            CameraController.instance.target = playerObjects[i].transform;
            playerControllers[i].enabled = true;
            MusicManager.instance.SwitchClip(MusicManager.instance.standardSong, .1f);
        }

        if (i == 0)
        {
            playerControllers[1].enabled = false;
            playerControllers[2].enabled = false;
        }
        else if (i == 1)
        {
            playerControllers[0].enabled = false;
            playerControllers[2].enabled = false;
            finishedLevel1 = true;
            roomLights[0].SetActive(true);
        }
        else if (i == 2)
        {
            playerControllers[0].enabled = false;
            playerControllers[1].enabled = false;
            finishedLevel2 = true;
            roomLights[1].SetActive(true);
        }
        else // i == 3 special call to controller all of the characters
        {
            if (!threeCharactersInitialized)
            {
                playerControllers[0].enabled = true;
                playerControllers[1].enabled = true;
                playerControllers[2].enabled = true;
                CameraController.instance.target = null;
                CameraController.instance.transform.position = threeCharacterCameraPoint.position;
                CameraController.instance.zoomOut = true;
                lightsToDisable[1].SetActive(false);
                foreach (GameObject trigger in doorTriggers) trigger.SetActive(true);
                finishedLevel3 = true;
                SoundManager.instance.PlaySound(SoundManager.instance.elevatorDoorsOpen, .2f, 1f);
                MusicManager.instance.StopMusic();
                roomLights[2].SetActive(true);

                threeCharactersInitialized = true;
            }
        }
    }

    bool GameOver()
    {
        if (!playerControllers[0].enabled && !playerControllers[1].enabled && !playerControllers[2].enabled && finishedLevel3) // all 3 players entered the doors
            return true;
        else return false;
    }

    public void WonTheGame()
    {
        currentState = GameState.win;
        elevatorSound.Play();
        Invoke(nameof(StopElevatorSound), elevatorSoundDuration);
        winPoint.gameObject.SetActive(true);
        CameraController.instance.followSpeed /= 2f;
        CameraController.instance.target = winPoint;
        foreach (GameObject player in newPlayers) player.SetActive(true);
        foreach (PlayerController player in playerControllers) player.animator.SetBool("Fade", true); // fade animation
        Invoke(nameof(PlayCheerNoise), 3.5f);
        Invoke(nameof(PlayWinMusic), 3.5f);
        winCanvas.SetActive(true);
        MusicManager.instance.aSource.loop = false;
        MusicManager.instance.StopMusic();

        Debug.Log("Player just won the game. Initializing win tasks");

        // animate players shrinking into elevator or something similar
        // have camera traverse upward to above ground, then players come out from ground and are free 
        // ** vision: camera goes to center pod, slowly zooms out to a view of all 3 cells and slowly moves upwards
        // once it reaches all of the players at the top, game is over. show whole map with decent lighting
    }

    void PlayWinMusic()
    {
        MusicManager.instance.SwitchClip(MusicManager.instance.winSong, .2f);
    }
    void PlayCheerNoise()
    {
        SoundManager.instance.PlaySound(SoundManager.instance.cheerNoise, .2f, 1.2f);
        confetti.Play();
    }

    void StopElevatorSound()
    {
        elevatorSound.Stop();
    }

    public void LostTheGame(int i) // pass the player that lost the game
    {
        CameraController.instance.target = computerPositions[i];
        Camera.main.orthographicSize = 2f;
        computerScreens[i].SetActive(true);
        SoundManager.instance.PlaySound(SoundManager.instance.screenOff, .2f, 1f);
        MusicManager.instance.StopMusic();

        Invoke(nameof(LoseMusic), gameOverDelay); // delay for dramatic effect
        Invoke(nameof(LoseTasks), gameOverDelay);
    }

    void LoseMusic()
    {
        MusicManager.instance.SwitchClip(MusicManager.instance.loseSong, .2f);
    }

    void LoseTasks()
    {
        currentState = GameState.lose;
        mainCanvas.SetActive(false);
        gameOverCanvas.SetActive(true);
        SoundManager.instance.PlaySound(SoundManager.instance.screenOff, .2f, .8f);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
