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
    // Start is called before the first frame update
    void Start()
    {
        healthBar = GameObject.Find("Green").GetComponent<Image>();

    }

    // Update is called once per frame
    void Update()
    {
        /*if (healthAmount <= 0)
        {
            Application.LoadLevel(Application.loadedLevel);
        }*/
       
    }

    [RegisterCommand(Name="takeDamage",Help = "Take damage", MinArgCount = 1, MaxArgCount = 1)]
    public static void takeDamage(CommandArg[] args)
    {
        healthAmount -= args[0].Float;
        Debug.Log(args[0].Float);
        healthBar.GetComponent<Image>().fillAmount = healthAmount / 100f;

    }
    /*public void Heal(float healingAmount)
    {
        healingAmount += healingAmount;
        healthAmount = Mathf.Clamp(healthAmount, 0, 100);

        healthBar.fillAmount = healthAmount / 100f;
    }*/

}
