﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelectionController : MonoBehaviour
{
    public GameObject gameController = null;
    public GameObject playerlist = null;
    public List<GameObject> playerContainer;
    private Dictionary<PlayerInput, GameObject> containerToPlayer = new Dictionary<PlayerInput, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        MatchRules rules = new MatchRules();
        this.gameController.GetComponent<GameController>().UpdateMatchRules(rules);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPlayerJoined(PlayerInput player)
    {
        if (!this.containerToPlayer.ContainsKey(player))
        {
            GameObject emptyContainer = GetEmptyPlayerContainer();
            if (emptyContainer != null)
            {
                this.containerToPlayer.Add(player, emptyContainer);
                player.GetComponentInParent<PlayerController>().Id = Convert.ToInt32(emptyContainer.name.Replace("PlayerContainer_", ""));
                emptyContainer.transform.Find("join_help").GetComponent<TextMeshProUGUI>().text = $"P{player.GetComponentInParent<PlayerController>().Id} Select character";

                this.gameController.GetComponent<GameController>().AddPlayer(player);
                Debug.Log("Player joined!");
            }
        }
    }

    public void UpdateSelectedCharacter(PlayerInput player, string character)
    {
        if (this.containerToPlayer.ContainsKey(player))
        {
            GameObject container = null;
            this.containerToPlayer.TryGetValue(player, out container);
            container.transform.Find("join_help").GetComponent<TextMeshProUGUI>().text = character;
        }
    }

    public GameObject GetEmptyPlayerContainer()
    {
        foreach(GameObject container in this.playerContainer)
        {
            if (!this.containerToPlayer.ContainsValue(container)) return container;
        }

        return null;
    }

    public void OnPlayerLeft(PlayerInput player)
    {
        if (this.containerToPlayer.ContainsKey(player))
        {
            GameObject container = null;
            this.containerToPlayer.TryGetValue(player, out container);
            container.transform.Find("join_help").GetComponent<TextMeshProUGUI>().text = "Press Start!";
            this.containerToPlayer.Remove(player);
            this.gameController.GetComponent<GameController>().RemovePlayer(player);
            Debug.Log("Player left!");
        }
    }
}
