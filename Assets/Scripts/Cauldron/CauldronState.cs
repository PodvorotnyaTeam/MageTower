using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [Header("Анимация перемешивания")]
    [Tooltip("Компонент Animator ложки (если не назначен в инспекторе, ищется автоматически на объекте Spoon)")]
    [SerializeField]
    private Animator spoonAnimator;
    [Tooltip("Имя анимации / состояния перемешивания")]
    [SerializeField]
    private string stirringAnimationName = "Stirring";

    public bool cauldronIsFull = false;
    public bool fireIsLit = false;
    bool isThereIngredient = false;
    private bool isBrewing = false;
    private int k = 0;
    private int f = 0;

    private void OnDisable()
    {
        isBrewing = false;
    }

    public void Awake()
    {
        foreach (var recipe in recipes)
        {
            tempRecipes.Add(recipe);
        }

        InitSpoonAnimator();
    }

    public void InitSpoonAnimator()
    {
        if (spoonAnimator != null) return;

        // 1. Поиск среди дочерних объектов общего родителя (котла)
        if (transform.parent != null)
        {
            Transform spoonTransform = transform.parent.Find("Spoon");
            if (spoonTransform != null)
            {
                spoonAnimator = spoonTransform.GetComponent<Animator>();
            }
        }

        // 2. Если не найден в родителе, ищем ложку со скриптом Animator по всей сцене (включая неактивные объекты)
        if (spoonAnimator == null)
        {
            GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var animators = root.GetComponentsInChildren<Animator>(true);
                foreach (var anim in animators)
                {
                    if (anim.gameObject.name.Equals("Spoon", StringComparison.OrdinalIgnoreCase))
                    {
                        spoonAnimator = anim;
                        break;
                    }
                }
                if (spoonAnimator != null) break;
            }
        }

        // 3. Если Spoon найден в сцене, но Animator на нем отсутствует, добавляем компонент автоматически
        if (spoonAnimator == null)
        {
            Transform spoonTransform = transform.parent != null ? transform.parent.Find("Spoon") : null;
            if (spoonTransform == null)
            {
                GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t.gameObject.name.Equals("Spoon", StringComparison.OrdinalIgnoreCase))
                        {
                            spoonTransform = t;
                            break;
                        }
                    }
                    if (spoonTransform != null) break;
                }
            }

            if (spoonTransform != null)
            {
                spoonAnimator = spoonTransform.GetComponent<Animator>();
                if (spoonAnimator == null)
                {
                    spoonAnimator = spoonTransform.gameObject.AddComponent<Animator>();
                }
            }
        }
    }

    public void PlayStirringAnimation()
    {
        if (spoonAnimator == null)
        {
            InitSpoonAnimator();
        }

        if (spoonAnimator != null)
        {
            // Если объект Spoon выключен на сцене, активируем его
            if (!spoonAnimator.gameObject.activeSelf)
            {
                spoonAnimator.gameObject.SetActive(true);
            }

            // Убеждаемся, что Animator включен
            if (!spoonAnimator.enabled)
            {
                spoonAnimator.enabled = true;
            }

            // Если у ложки есть Rigidbody, делаем kinematic на время анимации, чтобы физика не сбивала движение
            Rigidbody rb = spoonAnimator.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Сбрасываем триггеры в контроллере, чтобы не было повторного перехода из Idle
            if (spoonAnimator.runtimeAnimatorController != null)
            {
                foreach (var param in spoonAnimator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        spoonAnimator.ResetTrigger(param.name);
                    }
                }
            }

            // Запускаем состояние анимации напрямую с начала ровно 1 раз
            spoonAnimator.Play(stirringAnimationName, 0, 0f);
        }
        else
        {
            Debug.LogWarning("[CauldronState] Не удалось найти Animator для объекта Spoon!");
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
                        isThereIngredient = true;
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

    private void SpawnPotionResult(Recipes recipe, double deviation)
    {
        if (recipe == null || recipe.result == null)
        {
            Debug.LogError("CauldronState: Recipe result is not assigned.");
            return;
        }

        GameObject potion = Instantiate(recipe.result, resultPoint.transform);
        RarityChecker(deviation, potion);

        // Recipes are named after their stable asset IDs (for example,
        // Heal_Potion). Quest objectives use the same ID.
        string potionID = string.IsNullOrWhiteSpace(recipe.name)
            ? recipe.nameRecipe
            : recipe.name;

        QuestItem questItem = potion.GetComponent<QuestItem>();
        if (questItem == null)
            questItem = potion.AddComponent<QuestItem>();
        questItem.itemID = potionID;

        GameEvents.OnPotionCrafted?.Invoke(potionID, 1);
    }

    public void Brew()
    {
        // Не запускаем повторно, если варка уже идёт
        if (isBrewing) return;

        if (isThereIngredient == false)
        {
            StartCoroutine(BrewRoutine());
        }
    }

    private IEnumerator BrewRoutine()
    {
        isBrewing = true;
        PlayStirringAnimation();

        if (spoonAnimator != null)
        {
            // Ждем 1 кадр, чтобы Animator обновил состояние
            yield return null;

            float duration = 0f;
            if (spoonAnimator.runtimeAnimatorController != null)
            {
                foreach (var clip in spoonAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == stirringAnimationName)
                    {
                        duration = clip.length;
                        break;
                    }
                }
            }

            if (duration > 0f)
            {
                float speed = Mathf.Max(0.0001f, spoonAnimator.speed);
                yield return new WaitForSeconds(duration / speed);
            }
            else
            {
                // Запасное ожидание по normalizedTime, если длительность клипа не найдена
                while (spoonAnimator != null &&
                       spoonAnimator.GetCurrentAnimatorStateInfo(0).IsName(stirringAnimationName) &&
                       spoonAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                {
                    yield return null;
                }
            }
        }

        ExecuteBrewResults();
        isBrewing = false;
    }

    private void ExecuteBrewResults()
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
                SpawnPotionResult(tempRecipes[0], deviation);
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
                SpawnPotionResult(tempRecipes[index], deviations[index]);
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
        isThereIngredient = false;
    }
}
