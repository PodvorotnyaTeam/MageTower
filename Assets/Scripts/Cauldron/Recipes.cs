using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipes", menuName = "Alchemy/Recipes")]
public class Recipes : ScriptableObject
{
    public string id;
    public string nameRecipe;

    public int tocisity;
    public int acidity;
    public int volatility;
    public int viscosity;
    public int thermoactivity;
    public int vitality;
    public int conductivity;
    public int psychoactivity;
    public int entropy;

    public List<int> attributes;

    public bool isCommon;
    public bool isEpic;
    public bool isIdeal;

    public int maxDeviation;

    public GameObject result;
    public void Awake()
    {
        attributes = new List<int> { tocisity, acidity, volatility, viscosity, thermoactivity, vitality, conductivity, psychoactivity, entropy };
    }
}
