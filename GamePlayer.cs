using System.Collections.Generic;
using UnityEngine;

public class GamePlayer : MonoBehaviour
{
    // movement
    // the player will move in a pixellated fashion, square by square.
    [SerializeField] float moveAmount; // distance player should move each move frame
    [SerializeField] float timeBetweenMove;
    float timeSinceMoved;

    // multi-character
    [SerializeField] List<GamePlayer> otherPlayers = new List<GamePlayer>();

    // stats
    int maxHealth = 3;
    public int currentHealth;
    [SerializeField] List<GameObject> hearts = new List<GameObject>();
    [SerializeField] List<PlayerController> players = new List<PlayerController>();
    float restartDelay = 1f; // don't let player move for this duration after losing a heart and going back to start of level
    bool canPlay = false;

    // input
    float x, y;

    public bool storedStartPositionAlready = false; // prevents start position from updating to become a false position upon retrying the minigames
    [SerializeField] Transform startPos;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHeartUI();

        transform.position = startPos.position;
        Invoke(nameof(ResetPlayer), restartDelay);
    }

    private void Update()
    {
        if (canPlay)
        {
            PlayerInput();
            Movement();
        }
        else // if one can't play, make sure all can't play
        {
            foreach (GamePlayer player in otherPlayers)
            {
                player.canPlay = false;
                Invoke(nameof(player.ResetPlayer), restartDelay);
            }
        }

        if (Input.GetKey(KeyCode.L))
        {
            GameManager.instance.ActivateCharacter(CurrentMonitor() + 1);
            GameManager.instance.DeactivateMonitor(CurrentMonitor());
        }
    }

    void PlayerInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
    }

    void Movement()
    {
        if (Mathf.Abs(x) > 0f || Mathf.Abs(y) > 0f) // player trying to move
        {
            if (timeSinceMoved <= 0f)
            {
                MoveSpecificDistance();
                timeSinceMoved = timeBetweenMove;
            }
        }

        timeSinceMoved -= Time.deltaTime;
    }

    void MoveSpecificDistance()
    {
        if (Mathf.Abs(x) > 0) transform.Translate(moveAmount * x * Vector2.right);
        else if (Mathf.Abs(y) > 0) transform.Translate(moveAmount * y * Vector2.up);
    } 

    void UpdateHeartUI()
    {
        if (currentHealth == 3) foreach (GameObject heart in hearts) heart.SetActive(true);
        else if (currentHealth == 2)
        {
            hearts[0].SetActive(true);
            hearts[1].SetActive(true);
            hearts[2].SetActive(false);
        }
        else if (currentHealth == 1)
        {
            hearts[0].SetActive(true);
            hearts[1].SetActive(false);
            hearts[2].SetActive(false);
        }
        else foreach (GameObject heart in hearts) heart.SetActive(false);
    }

    int CurrentMonitor() // returns index of current player that corresponds to this pod
    {
        if (this.CompareTag("Player1")) return 0;
        else if (this.CompareTag("Player2")) return 1;
        else return 2;
    }

    void HitWall()
    {
        currentHealth -= 1;
        UpdateHeartUI();

        if (currentHealth <= 0) KickPlayerOffComputer();

        transform.position = startPos.position;
        if (otherPlayers != null) foreach (GamePlayer player in otherPlayers) player.BackToStart(); // move all players back to start position upon collision

        canPlay = false;
        Invoke(nameof(ResetPlayer), restartDelay);
    }

    public void BackToStart()
    {
        this.transform.position = this.startPos.position;
    }

    public void ResetPlayer()
    {
        this.canPlay = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
        {
            if (collision.collider.CompareTag("Hazard") && canPlay)
            {
                Debug.Log("Bumping into wall. Player resets to start position and loses a heart");
                HitWall();
                SoundManager.instance.PlaySound(SoundManager.instance.hitWall, .2f, 1f);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision) // prevents bug where player can stick to wall right on start and not be penalized
    {
        if (collision != null)
        {
            if (collision.collider.CompareTag("Hazard") && canPlay)
            {
                Debug.Log("Bumping into wall. Player resets to start position and loses a heart");

                SoundManager.instance.PlaySound(SoundManager.instance.hitWall, .2f, 1f);
                currentHealth -= 1;
                UpdateHeartUI();

                if (currentHealth <= 0) KickPlayerOffComputer();

                transform.position = startPos.position;
                canPlay = false;

                Invoke(nameof(ResetPlayer), restartDelay);
            }
        }
    }

    void KickPlayerOffComputer()
    {
        currentHealth = maxHealth;
        UpdateHeartUI();
        canPlay = true; // make sure play can play once they get back on
        players[CurrentMonitor()].switchedToMonitor = false; // allows player to interact with this computer again
        if (this.name == "GamePlayer") players[CurrentMonitor()].LoseLife();
        transform.position = startPos.position;
        MusicManager.instance.SwitchClip(MusicManager.instance.standardSong, .1f);
        SoundManager.instance.PlaySound(SoundManager.instance.hitWall, .2f, 1f);
        GameManager.instance.DeactivateMonitor(CurrentMonitor());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null)
        {
            if (collision.CompareTag("Win1"))
            {
                Debug.Log("Beat level one");
                GameManager.instance.ActivateCharacter(1); // switch to character 2 now
                GameManager.instance.DeactivateMonitor(0);
                SoundManager.instance.PlaySound(SoundManager.instance.beatLevel, .1f, 1f);
            }

            if (collision.CompareTag("Win2"))
            {
                Debug.Log("Beat level two");
                GameManager.instance.ActivateCharacter(2); // switch to character 2 now
                GameManager.instance.DeactivateMonitor(1);
                SoundManager.instance.PlaySound(SoundManager.instance.beatLevel, .1f, 1f);
            }

            if (collision.CompareTag("Win3"))
            {
                Debug.Log("Beat level three");
                GameManager.instance.ActivateCharacter(3); // switch to ALL characters now
                GameManager.instance.DeactivateMonitor(2);
                MusicManager.instance.StopMusic();
                SoundManager.instance.PlaySound(SoundManager.instance.beatLevel, .1f, 1f);
            }
        }
    }
}
