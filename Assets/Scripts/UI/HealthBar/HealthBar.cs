using CommandTerminal;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    public static Image healthBar;
    public static float healthAmount = 100f;
    void Start()
    {
        healthBar = GameObject.Find("Green").GetComponent<Image>();

    }

    //[RegisterCommand(Name="takeDamage",Help = "Take damage", MinArgCount = 1, MaxArgCount = 1)]
    public static void takeDamage(CommandArg[] args)
    {
        healthAmount -= args[0].Float;
        Debug.Log(args[0].Float);
        healthBar.fillAmount = healthAmount / 100f;

    }

}
