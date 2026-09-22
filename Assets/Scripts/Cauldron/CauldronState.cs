using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.WSA;

public class CauldronState : MonoBehaviour
{
    public List<Recipes> recipes;
    public Ingridient overcookedIngridient;

    public List<GameObject> failedRecipes;

    public List<Recipes> tempRecipes = new List<Recipes>();
    private List<Recipes> sortRecipes = new List<Recipes>();

    [SerializeField]
    private List<GameObject> ingridientsInCauldron = new List<GameObject>();
    public List<int> attributesInCauldron = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    [SerializeField]
    private GameObject resultPoint;
    [SerializeField]
    private CampfireMehanic campfire;
    [SerializeField]
    private Bucket_behavior bucket_Behavior;
    public bool cauldronIsFull = false;
    public bool fireIsLit = false;
    private int k = 0;
    private int f = 0;

    public void Awake()
    {
        foreach (var recipe in recipes)
        {
            tempRecipes.Add(recipe);
        }
    }

    public IEnumerator Coroutine()
    {
        yield return new WaitForSeconds(6);
    }

    public void Update()
    {
        if (ingridientsInCauldron.Count >= 1)
        {
            List<GameObject> tempGameObjects = new List<GameObject>(ingridientsInCauldron);
            foreach (GameObject ingridient in tempGameObjects)
            {
                if (campfire.state.isCampfireLit && cauldronIsFull)
                {
                    ingridient.GetComponent<IngridientStats>().cookingTIMER += Time.deltaTime;
                    if (ingridient.GetComponent<IngridientStats>().cookingTIMER >= ingridient.GetComponent<IngridientStats>().ingridient.cookingTime)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            attributesInCauldron[i] += ingridient.GetComponent<IngridientStats>().ingridient.attributes[i];
                            attributesInCauldron[i] = Math.Clamp(attributesInCauldron[i], -10, 10);
                        }
                        ingridientsInCauldron.Remove(ingridient);
                        Destroy(ingridient);
                    }
                }
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        ingridientsInCauldron.Add(other.gameObject);
        Debug.Log("В котёл помещён" +  other.name);
    }

    public void OnTriggerExit(Collider other)
    {
        ingridientsInCauldron.Remove(other.gameObject);
        Debug.Log("Из котла был удалён" + other.name);
    }

    private void RIUpdater()
    {

    }

    public void RecipeMatcher()
    {
        sortRecipes = new List<Recipes>(tempRecipes);
        foreach (var recipe in sortRecipes)
        {
            for (int i = 0; i < 9; i++)
            {
                int diff = Mathf.Abs(attributesInCauldron[i] - recipe.attributes[i]);
                if (diff > recipe.maxDeviation) { tempRecipes.Remove(recipe); break; }
            }
        }
    }

    public void RarityChecker(double score, GameObject i)
    {
        if (score < 1)
        {
            i.transform.Find("Rarity/Epic").gameObject.SetActive(true);
        }
        else if (score < 2)
        {
            i.transform.Find("Rarity/Rare").gameObject.SetActive(true);
        }
    }

    public void Brew()
    {
        RecipeMatcher();
        if (tempRecipes.Count > 0)
        {
            if (tempRecipes.Count == 1)
            {
                double deviation = 0;
                for (int i = 0; i < 9; i++)
                {
                    int diff = Mathf.Abs(attributesInCauldron[i] - tempRecipes[0].attributes[i]);
                    deviation += diff / tempRecipes[0].maxDeviation;
                }
                GameObject j = Instantiate(tempRecipes[0].result, resultPoint.transform);
                RarityChecker(deviation, j);
            }
            else
            {
                List<double> deviations = new List<double>();
                foreach (var item in tempRecipes)
                {
                    double deviation = 0;
                    for (int i = 0; i < 9; i++)
                    {
                        int diff = Mathf.Abs(attributesInCauldron[i] - item.attributes[i]);
                        deviation += diff / item.maxDeviation;
                    }
                    deviations.Add(deviation);
                }
                int index = deviations.IndexOf(deviations.Min());
                GameObject j = Instantiate(tempRecipes[index].result, resultPoint.transform);
                RarityChecker(deviations[index], j);
            }
        }
        else
        {
            int index = attributesInCauldron.IndexOf(attributesInCauldron.Max());
            Instantiate(failedRecipes[index], resultPoint.transform);
        }
        bucket_Behavior.ResetWaterLLevels();

        f += 1;
        if (f > 2)
        {
            campfire.HardReset();
        }
        else campfire.SoftReset();
        tempRecipes = new List<Recipes>(recipes);
        attributesInCauldron = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        ingridientsInCauldron.Clear();
        k = 0;
    }

}
