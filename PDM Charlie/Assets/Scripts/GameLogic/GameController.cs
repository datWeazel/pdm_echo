﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject UI = null;
    public List<PlayerInput> players;
    public List<GameObject> characterPrefabs;
    public PlayerInputManager playerInputManager;
    public string gameState = "";

    public MatchRules rules = null;
    public string stageName = "";

    private Vector3 mainMenuCamPosition = new Vector3(347.23f, 0.1f, 583.15f);
    private Quaternion mainMenuCamRotation = Quaternion.Euler(10.3f, -90.0f, 0.0f);

    private Vector3 mainMenuCharSelectCamPosition = new Vector3(347.2f, 0.8f, 589.2f);
    private Quaternion mainMenuCharSelectCamRotation = Quaternion.Euler(26.8f, -90.0f, 0.0f);

    // Start is called before the first frame update
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(this);
        gameState = "start_screen"; 
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddPlayer(PlayerInput player)
    {
        Debug.Log("Joined!");
        if (gameState == "start_screen")
        {
            SetGameState("menu_main");
        }

        if (players.Count <= 0)
        {
            UI.GetComponent<MainMenuController>().HideStartGameScreen();
        }

        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    public void RemovePlayer(int playerId)
    {
        PlayerInput p = players.FirstOrDefault(pInput => pInput.playerIndex == playerId);
        if (p != null) players.Remove(p);

        if (this.gameState == "match_active")
        {
            CameraLogic camLogic = Camera.main.GetComponent<CameraLogic>();
            if (camLogic != null) camLogic.RemovePlayerFromCam(p?.GetComponent<PlayerController>().characterController.transform);

            if (this.rules.teamSize == 1)
            {
                if (players.Count == 1)
                {
                    EndMatch(players);
                }
            }
        }

        if(players.Count <= 0)
        {
            UI.GetComponent<MainMenuController>().ShowStartGameScreen();
        }
    }

    public void EndMatch(List<PlayerInput> winners)
    {
        string endScreenText = "";
        foreach(PlayerInput p in winners)
        {
            endScreenText += $"P{p.GetComponent<PlayerController>().Id} ";
        }

        endScreenText += "WINS!";
        MatchHUDController matchHUDController = GameObject.Find("MATCH_HUD").GetComponent<MatchHUDController>();
        matchHUDController.UpdateEndScreenText(endScreenText);
        matchHUDController.SetEndScreenVisible(true);
        this.gameState = "menu_match_end";
    }

    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void UpdateSelectedStage(string stage)
    {
        this.stageName = stage;
        GameObject.Find("SelectedStage").GetComponent<TextMeshProUGUI>().text = $"{this.stageName}";
    }

    public void RaiseStockCount()
    {
        this.rules.stocks++;
        if (this.rules.stocks > 999) this.rules.stocks = 999;
        GameObject.Find("StocksCount").GetComponent<TextMeshProUGUI>().text = $"{this.rules.stocks}";
    }

    public void LowerStockCount()
    {
        this.rules.stocks--;
        if (this.rules.stocks < 1) this.rules.stocks = 1;
        GameObject.Find("StocksCount").GetComponent<TextMeshProUGUI>().text = $"{this.rules.stocks}";
    }

    public void UpdateMatchRules(MatchRules rules)
    {
        this.rules = rules;
    }

    public void PrepareMatch()
    {
        if (this.stageName == "") return;

        gameState = "match_prepare";
        SceneManager.LoadScene(this.stageName);

        
        StartCoroutine(StartMatch(3));
    }

    IEnumerator StartMatch(int waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        gameState = "match_active";

        foreach (PlayerInput playerInput in players)
        {
            PlayerController player = playerInput.GetComponent<PlayerController>();
            GameObject character = characterPrefabs.FirstOrDefault(p => p.name == player.character);
            if (character != null)
            {
                GameObject c = player.CreateCharacter(character, GetPlayerStageSpawn(player.Id));
                player.stocks = this.rules.stocks;
                player.matchHUD = GetPlayerMatchInfoController(player.Id);
                player.matchHUD.ActivateParent();
                player.matchHUD.UpdatePlayerName($"P{player.Id}");
                player.matchHUD.UpdatePlayerStockCount(player.stocks);
                Camera.main.GetComponent<CameraLogic>()?.AddPlayerToCam(c.transform);
            }
        }

        Debug.Log("Match started!");
    }

    public void SetGameState(string state)
    {
        gameState = state;

        if (gameState == "menu_main")
        {
            UI.GetComponent<MainMenuController>().MoveMainMenuCameraToPosition(mainMenuCamPosition, mainMenuCamRotation);
        }
        else if (gameState == "menu_character_select")
        {
            UI.GetComponent<MainMenuController>().MoveMainMenuCameraToPosition(mainMenuCharSelectCamPosition, mainMenuCharSelectCamRotation);
        }
        else if (gameState == "menu_stage_select")
        {
            UI.GetComponent<MainMenuController>().MoveMainMenuCameraToPosition(mainMenuCamPosition, mainMenuCamRotation);
        }
    }

    public string GetGameState()
    {
        return this.gameState;
    }

    public PlayerMatchInfoController GetPlayerMatchInfoController(int id)
    {
        GameObject[] matchHUDs = GameObject.FindGameObjectsWithTag("PlayerMatchHUD");
        Debug.Log($"matchHUDs Count: {matchHUDs.Length}");
        foreach(GameObject matchHUD in matchHUDs)
        {
            if (matchHUD.name == $"PlayerMatchHUD_{id}")
            {
                //return matchHUD.GetComponent<PlayerMatchInfoController>();
                return matchHUD.GetComponentInChildren<PlayerMatchInfoController>(true);
            }
        }

        return null;
    }

    public Vector3 GetPlayerStageSpawn(int id)
    {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("StageSpawn");
        foreach (GameObject spawn in spawns)
        {
            if (spawn.name == $"Spawn_{id}")
            {
                //return matchHUD.GetComponent<PlayerMatchInfoController>();
                return spawn.transform.position;
            }
        }

        return new Vector3();
    }

    public bool DoesEveryPlayerHaveCharacter()
    {
        foreach (PlayerInput player in players)
        {
            if (player.GetComponent<PlayerController>().character == "") return false;
        }

        return true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (this.players.Count <= 0)
            {
                UI.GetComponent<MainMenuController>().ShowStartGameScreen();
                this.gameState = "start_screen";
            }
            else
            {
                this.gameState = "menu_main";
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
