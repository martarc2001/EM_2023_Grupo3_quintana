using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TimerScript : NetworkBehaviour
{
    const float MAX_TIMEOUT = 5;
    public float TimeLeft = MAX_TIMEOUT;
    public bool TimerOn = false;
    public GameObject gameWillStartGameObject;
    public GameObject waitingForPlayersReady;

    public TextMeshProUGUI TimerTxt;
    // Start is called before the first frame update
    void Start()
    {
        TimeLeft = 5;
        TimerOn=true;
    }
    public void restart()
    {
        TimeLeft = 5;
        TimerOn = true;
    }
    [ClientRpc]
    public void restartClientRpc()
    {
        TimeLeft = 5;
        TimerOn = true;
    }

    void Start() { startTimer(); }

    public void startTimer() { TimerOn = true; }

    void Update()
    {
        if (ConnectedPlayers.Instance.readyPlayers.Value == 0)
        {
            checkConnectedPlayers();
        }
        if (TimerOn)
        {

            if (TimeLeft > 0)
            {
                TimeLeft -= Time.deltaTime;
                updateTimer(TimeLeft);
            }
            else
            {
                if (!IsClient) { ConnectedPlayers.Instance.StartGameServerRpc(); }

                TimeLeft = 0;
                TimerOn = false;
            }
        }
    }



    void updateTimer(float currentTime)
    {
        currentTime += 1;
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);
        TimerTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }



    public void checkConnectedPlayers()
    {
        TimerOn = false;
        TimeLeft = MAX_TIMEOUT;

        gameWillStartGameObject.SetActive(false);
        waitingForPlayersReady.SetActive(true);


    }



}
