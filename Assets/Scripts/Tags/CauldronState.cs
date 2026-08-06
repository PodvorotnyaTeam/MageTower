using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.WSA;
using System.Linq;

public class CauldronState : MonoBehaviour
{
    public List<Recipes> recipes;
    public Ingridient overcookedIngridient;
    public Recipes failedRecipe;
    private List<Recipes> tempRecipes = new List<Recipes>();
    private List<Recipes> sortRecipes = new List<Recipes>();
    [SerializeField]
    private List<GameObject> ingridientsInCauldron = new List<GameObject>();
    [SerializeField]
    private GameObject resultPoint;
    private int k = 0;

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
                ingridient.GetComponent<IngridientStats>().cookingTIMER += Time.deltaTime;
                if (ingridient.GetComponent<IngridientStats>().cookingTIMER >= ingridient.GetComponent<IngridientStats>().ingridient.cookingTime) //&ingridient.GetComponent<IngridientStats>().cookingTIMER < ingridient.GetComponent<IngridientStats>().ingridient.overcookingTime
                {
                    RecipeMatcher(ingridient.GetComponent<IngridientStats>().ingridient);
                    ingridientsInCauldron.Remove(ingridient);
                    Destroy(ingridient);
                }
                //else if (ingridient.cookingTIMER >= ingridient.ingridient.overcookingTime)
                //{
                //    RecipeMatcher(overcookedIngridient);
                //}
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

    public void RecipeMatcher(Ingridient ingridient)
    {
        sortRecipes = new List<Recipes>(tempRecipes);
        foreach (var recipe in sortRecipes)
        {
            if (recipe.ingridients[k].nameIngridient != ingridient.nameIngridient)
            {
                tempRecipes.Remove(recipe);
            }
        }
        k++;
    }

    public void Brew()
    {
        if (tempRecipes.Count > 0)
        {
            foreach (var recipe in tempRecipes)
            {
                if (k == tempRecipes.First().ingridients.Count)
                {
                    Instantiate(tempRecipes.First().result, resultPoint.transform);
                }
            }
        }
        else
        {
            Instantiate(failedRecipe.result, resultPoint.transform);
        }
        tempRecipes = new List<Recipes>(recipes);
        ingridientsInCauldron.Clear();
        k = 0;
    }

}
