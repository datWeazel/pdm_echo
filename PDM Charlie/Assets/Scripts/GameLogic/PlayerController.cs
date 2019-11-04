﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public int Id = -1;
    public int stocks = 1;
    public float percentage = 0;
    public Vector3 spawnpoint = new Vector3();

    public GameObject selector = null;
    public float selectorSpeed = 1.0f;
    private GameObject gameController = null;
    private GameController gameControllerScript = null;

    public string character = "";
    public CharacterControllerBase characterController = null;

    public PlayerMatchInfoController matchHUD = null;

    private Vector2 moveVector = new Vector2();

    private bool lightAttackHold = false;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);

        this.gameController = GameObject.Find("GameController");
        this.gameControllerScript = gameController.GetComponent<GameController>();

        this.selector.GetComponentInChildren<TextMeshProUGUI>().text = $"P {this.Id}";
    }

    // Update is called once per frame
    void Update()
    {
        string gameState = this.gameControllerScript.GetGameState();
        //Debug.Log($"State: {gameState} || Id: {this.Id}");

        if(gameState.Contains("menu_"))
        {
            if (gameState == "menu_main" & this.Id != 1)
            {
                SetSelectorState(false);
                return;
            }

            SetSelectorState(true);

            if (this.moveVector != new Vector2())
            {
                float cursorSpeed = this.selectorSpeed;
                if (lightAttackHold) cursorSpeed *= 2;
                this.selector.transform.position += new Vector3((this.moveVector.x * cursorSpeed), (this.moveVector.y * cursorSpeed), 0);
            }
        }
        else
        {
            SetSelectorState(false);

            if (this.matchHUD != null)
            {
                this.matchHUD.UpdatePlayerPercentage(this.percentage);
            }


            if (gameState == "match_active")
            {
                if (this.moveVector != new Vector2())
                {
                    this.characterController?.Move(new Vector2(this.moveVector.x, this.moveVector.y));
                }
            }
        }
    }

    public void HitDeathZone()
    {
        this.stocks--;
        this.matchHUD?.UpdatePlayerStockCount(this.stocks);

        this.percentage = 0.0f;

        if(this.stocks == 0)
        {
            this.gameControllerScript.RemovePlayer(this.GetComponent<PlayerInput>());
            GameObject.Destroy(this.characterController.gameObject);
            return;
        }

        this.characterController.SetPosition(this.spawnpoint);
        this.characterController.OnCharacterDying();
    }

    public GameObject CreateCharacter(GameObject characterPrefab, Vector3 position)
    {
        GameObject character = Instantiate(characterPrefab, this.transform);
        character.transform.position = position;

        this.characterController = character.GetComponent<CharacterControllerBase>();
        this.spawnpoint = position;

        return character;
    }

    public void OnMove(InputValue value)
    {
        this.moveVector = value.Get<Vector2>();
    }

    public void OnPoint(InputValue value)
    {
        this.selector.transform.position = value.Get<Vector2>();
    }

    public void OnClick()
    {
        if(this.gameControllerScript.GetGameState().Contains("menu_"))
        {
            Select();
        }
    }

    public void OnJump(InputValue value)
    {

        if (this.gameControllerScript.GetGameState().Contains("menu_"))
        {
            Select();
        }
        else if (this.gameControllerScript.GetGameState() == "match_active")
        {
            this.characterController?.JumpButtonPressed();
        }
    }

    public void OnJumpRelease(InputValue value)
    {
        this.characterController?.JumpButtonReleased();
    }

    public void Select()
    {
        PointerEventData cursor = new PointerEventData(EventSystem.current);
        cursor.position = this.selector.transform.position;

        List<RaycastResult> objectsHit = new List<RaycastResult>();
        EventSystem.current.RaycastAll(cursor, objectsHit);

        foreach (RaycastResult rr in objectsHit)
        {
            Transform CharacterNameTag = rr.gameObject.transform.Find("CharacterNameTag");
            if (CharacterNameTag != null)
            {
                this.character = CharacterNameTag.GetComponent<TextMeshProUGUI>().text;
                GameObject.Find("CharacterSelect").GetComponent<CharacterSelectionController>().UpdateSelectedCharacter(this.transform.GetComponent<PlayerInput>(), this.character);
                Debug.Log($"Player selected character {CharacterNameTag.GetComponent<TextMeshProUGUI>().text} ");

                return;
            }

            Transform StageNameTag = rr.gameObject.transform.Find("StageNameTag");
            if(StageNameTag != null)
            {
                this.gameControllerScript.stageName = StageNameTag.GetComponent<TextMeshProUGUI>().text;
                GameObject.Find("SelectedStage").GetComponent<TextMeshProUGUI>().text = $"{this.gameControllerScript.stageName}";
                return;
            }

            Button btn = rr.gameObject.GetComponent<Button>();
            if(btn != null)
            {
                if (rr.gameObject.name == "btn_menu_stage_select" && (this.gameControllerScript.players.Count < 2 || !this.gameControllerScript.DoesEveryPlayerHaveCharacter())) return;
                if (rr.gameObject.name == "btn_match_start" && this.gameControllerScript.stageName == "") return;
                btn.onClick.Invoke();
                return;
            }
        }
    }

    public void OnLightAttack(InputValue value)
    {
        lightAttackHold = true;
        if (this.gameControllerScript.GetGameState() == "match_active")
        {
            this.characterController?.LightAttackButtonPressed();
            Debug.Log("light atk");
        }
    }

    public void OnLightAttackRelease(InputValue value)
    {
        lightAttackHold = false;
        if (this.gameControllerScript.GetGameState() == "match_active")
        {
            this.characterController?.LightAttackButtonReleased();
            Debug.Log("light atk release");
        }
    }

    public void OnHeavyAttack(InputValue value)
    {
        if (this.gameControllerScript.GetGameState() == "match_active")
        {
            this.characterController?.HeavyAttackButtonPressed();
        }
    }

    public void OnHeavyAttackRelease(InputValue value)
    {
        if (this.gameControllerScript.GetGameState() == "match_active")
        {
            this.characterController?.HeavyAttackButtonReleased();
        }
    }

    public void OnStart(InputValue value)
    {
        if(this.gameControllerScript.gameState == "menu_match_end")
        {
            this.gameControllerScript.SetGameState("menu_main");
            this.gameControllerScript.LoadScene("MainMenu");
        }
    }

    public void OnSelect()
    {
        string gameState = this.gameControllerScript.gameState;
        if(gameState == "menu_main" || gameState == "menu_character_select")
        {
            GameObject.Destroy(this.gameObject);
        }
    }

    public void SetSelectorState(bool isActive)
    {
        if (this.selector.activeSelf != isActive) this.selector.SetActive(isActive);
    }
}
