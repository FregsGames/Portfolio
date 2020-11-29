using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    /// <summary>
    /// I use this class to serialize every value that I want to store in a save file
    /// Then, with my personal Saver class, I write it on a file
    /// I also use this script to read and load that stored data
    /// </summary>
    /// 
    public static int totalCharacters = 5; //KEEP THIS UPDATED
    public static int totalShopElements = 2; //KEEP THIS UPDATED

    //------General-----//
    int act;
    float gameTime;
    bool[] team = new bool[totalCharacters]; //bonny dreshi elektra murray
    int[,] globalHotDots = new int[totalShopElements, 3];
    int[] relics;
    int gold;
    int chapter;
    //float goldBonus;
    //relics
    //shop stuff (skull, potion)

    //-------World--------//
    Vector2 playerPos;
    int[,] worldTypes; // 0: null, 1: start, 2: combat, 3: shop, 4: blacksmith 5: boss, 6: elite
    int[,] worldWeatherTypes;

    //-----Characters----//
    int[] levels = new int[totalCharacters];
    int[] unusedPerks = new int[totalCharacters];
    float[] experience = new float[totalCharacters];
    int[,] stats = new int[totalCharacters, 5]; //hp, energy, str, def, spd
    int[,] _stats = new int[totalCharacters, 5]; //hp, energy, str, def, spd
    bool[,] talents = new bool[totalCharacters, 7]; //left to right 
    int[,] attacks = new int[totalCharacters, 3];
    int[,] attackLevels = new int[totalCharacters, 3];


    //References
    PathGenerator pGenerator;
    StatisticsEnd statistics;
    GameManager gManager;
    PathSelector pSelector;
    GlobalBuffs gBuffs;
    RelicStorage rStorage;
    GoldManager goldManager;
    AscensionManager aManager;

    public bool SaveGameExists()
    {
        /*if (PlayerPrefs.GetInt("s_gameExists", 0) == 1)
            return true;
        return false;*/

        if (Saver.instance.RetrieveInt("s_gameExists") == 1)
            return true;
        return false;
    }

    public IEnumerator LoadData()
    {
        AudioManager.instance.ChangeMusic(AudioManager.ThemeType.nexus1);

        yield return StartCoroutine(LoadScenes());
        Debug.Log("Calling retrieve save data");
        RetrieveSaveData();


        //TODO
        // ---- General ---- // 
        if (pGenerator == null)
            pGenerator = PathGenerator.instance;
        if (pGenerator == null)
            Debug.LogError("No PathGenerator found when trying to load data");

        pGenerator.floor = act;

        if (statistics == null)
            statistics = StatisticsEnd.instance;
        if (statistics == null)
            Debug.LogError("No StatisticsEnd found when trying to load data");

        statistics.SetElapsedTime(gameTime);

        if (gManager == null)
            gManager = GameManager.instance;
        if (gManager == null)
            Debug.LogError("No GameManager found when trying to load data");

        if (gBuffs == null)
            gBuffs = GlobalBuffs.instance;
        if (gBuffs == null)
            Debug.LogError("No GlobalBuffs found when trying to store data");

        gBuffs.SetupFromSave(globalHotDots);

        if (goldManager == null)
            goldManager = GoldManager.instance;
        if (goldManager == null)
            Debug.LogError("No GoldManager found when trying to store data");

        goldManager.LoadFromSave(gold);

        if (aManager == null)
            aManager = AscensionManager.instance;
        if (aManager == null)
            Debug.LogError("No AscensionManager found when trying to store data");
        aManager.currentAscension = chapter;


        //Team loading
        if (PlayerCharactersUnlockeables.instance == null)
            Debug.LogError("No PlayerCharactersUnlockeables found when trying to load data");

        PlayerCharactersUnlockeables.instance.RetrieveUnlocks();
        int i = 0;
        List<Fighter> fighters = new List<Fighter>();

        foreach (KeyValuePair<GameObject, bool> characters in PlayerCharactersUnlockeables.instance.GetUnlockeables())
        {
            if (team[i])
            {
                fighters.Add(characters.Key.GetComponent<Fighter>());
            }
            i++;
        }
        gManager.teamA = fighters.ToArray();
        gManager.SetupFromSave();

        //Relics
        if (rStorage == null)
            rStorage = RelicStorage.instance;
        if (rStorage == null)
            Debug.LogError("No RelicStorage found when trying to store data");

        rStorage.EnableRelicsFromSave(relics);

        // World ------//
        pGenerator.GenerateNewWorlds(worldTypes, worldWeatherTypes);

        if (pSelector == null)
            pSelector = PathSelector.instance;
        if (pSelector == null)
            Debug.LogError("No PathSelector found when trying to load data");

        pSelector.SetCurrentWorld(playerPos);

        //Characters ---- //
        i = 0;
        foreach (Fighter f in gManager.teamA)
        {
            f.GetComponent<FighterExperience>().SetLevel(levels[f.fUnlockIndex], i + 1);
            f.GetComponent<FighterExperience>().SetCurrentExperience(experience[f.fUnlockIndex]);
            f.GetComponent<FighterExperience>().SetPerkPoints(unusedPerks[f.fUnlockIndex]);

            f.hp = stats[f.fUnlockIndex, 0];
            f.str = stats[f.fUnlockIndex, 2];
            f.def = stats[f.fUnlockIndex, 3];
            f.spd = stats[f.fUnlockIndex, 4];
            f.energy = stats[f.fUnlockIndex, 1];

            f._hp = _stats[f.fUnlockIndex, 0];
            f._str = _stats[f.fUnlockIndex, 2];
            f._def = _stats[f.fUnlockIndex, 3];
            f._spd = _stats[f.fUnlockIndex, 4];
            f._energy = _stats[f.fUnlockIndex, 1];

            foreach (KeyValuePair<int, PasiveSkill> skills in f.GetComponentInChildren<SkillTree>().characterSkillTree)
            {
                if(talents[f.fUnlockIndex, skills.Key])
                    skills.Value.LoadFromSave();
            }

            int attackLength = 0;
            for (int k = 0; k < attacks.GetLength(1); k++)
            {
                if (attacks[f.fUnlockIndex, k] != -1)
                {
                    attackLength++;
                }
            }

            f.attacks = new FighterAttack[attackLength];
            for (int k = 0; k < f.attacks.Length; k++)
            {
                if (attacks[f.fUnlockIndex, k] != -1)
                {
                    f.attacks[k] = new FighterAttack(AttackStorage.instance.GetAllSkills()
                        [attacks[f.fUnlockIndex, k]].GetComponent<UpgradedAttack>()
                        , attackLevels[f.fUnlockIndex, k]);
                        //AttackStorage.instance.GetSkills()[attacks[f.fUnlockIndex, k]].GetComponent<Attack>();
                }
            }

            f.Get3DUI().UpdateManaAndHealth();

            i++;
        }

        SceneManager.UnloadSceneAsync("Loader");
        SceneManager.UnloadSceneAsync("Loading");
        SceneManager.UnloadSceneAsync("Menu");
    }

    IEnumerator LoadScenes()
    {
        AsyncOperation ao;
        ao = SceneManager.LoadSceneAsync("Loader", LoadSceneMode.Additive);
        while (!ao.isDone)
        {
            yield return null;
        }

        SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Additive);

        ao = SceneManager.LoadSceneAsync("Nexus", LoadSceneMode.Additive);
        while (!ao.isDone)
        {
            yield return null;
        }

    }

    public void RetrieveSaveData()
    {
        Debug.Log("Retrieving save data");

        //------General-----//
        act = Saver.instance.RetrieveInt("s_act", 1);                   //Act
        gameTime = Saver.instance.RetrieveFloat("s_gameTime");       //Game Time
        for (int i = 0; i < team.Length; i++)                   //Team (active characters)
        {
            team[i] = Saver.instance.RetrieveInt("s_player" + i, 0) == 1 ? true : false;
        }

        relics = new int[Saver.instance.RetrieveInt("s_relicCount", 0)];
        for (int i = 0; i < relics.Length; i++)
        {
            relics[i] = Saver.instance.RetrieveInt("s_relic" + i, 0);
        }

        gold = Saver.instance.RetrieveInt("s_gold", 0);

        chapter = Saver.instance.RetrieveInt("s_chapter", 0);

        for (int i = 0; i < totalShopElements; i++)             //Global hotdots
        {
            for (int j = 0; j < 3; j++)
            {
                globalHotDots[i, 0] = Saver.instance.RetrieveInt("s_globalBuffs_0" + i, 0);
                globalHotDots[i, 1] = Saver.instance.RetrieveInt("s_globalBuffs_1" + i, 0);
                globalHotDots[i, 2] = Saver.instance.RetrieveInt("s_globalBuffs_2" + i, 0);
            }
        }

        //-------World--------//
        playerPos = new Vector2(                                //Player pos
            Saver.instance.RetrieveInt("s_playerPosX", 0),
            Saver.instance.RetrieveInt("s_playerPosY", 0));

        //Types
        worldTypes = new int[Saver.instance.RetrieveInt("s_worldMatrixSizeX", 0), Saver.instance.RetrieveInt("s_worldMatrixSizeY", 0)];
        worldWeatherTypes = new int[Saver.instance.RetrieveInt("s_worldMatrixSizeX", 0), Saver.instance.RetrieveInt("s_worldMatrixSizeY", 0)];
        for (int i = 0; i < worldTypes.GetLength(0); i++)
        {
            for (int j = 0; j < worldTypes.GetLength(1); j++)
            {
                worldTypes[i, j] = Saver.instance.RetrieveInt("s_worldMatrixType" + i + j, 0);
            }
        }

        for (int i = 0; i < worldWeatherTypes.GetLength(0); i++)
        {
            for (int j = 0; j < worldWeatherTypes.GetLength(1); j++)
            {
                worldWeatherTypes[i, j] = Saver.instance.RetrieveInt("s_worldMatrixWeather" + i + j, 0);
            }
        }

        //Weathers

        //-----Characters----//
        levels = new int[totalCharacters];
        for (int i = 0; i < team.Length; i++)                   //Team levels
        {
            levels[i] = Saver.instance.RetrieveInt("s_playerLvL" + i, 1);
        }
        
        unusedPerks = new int[totalCharacters];
        for (int i = 0; i < team.Length; i++)                   //Team levels
        {
            unusedPerks[i] = Saver.instance.RetrieveInt("s_playerUnusedPerk" + i, 1);
        }

        stats = new int[totalCharacters, 5];
        for (int i = 0; i < totalCharacters; i++)                   //Team stats
        {
            for (int j = 0; j < 5; j++)                   
            {
                stats[i, j] = Saver.instance.RetrieveInt("s_playerStat" + i + j, 0);

            }
        }

        _stats = new int[totalCharacters, 5];
        for (int i = 0; i < totalCharacters; i++)                   //Team _stats
        {
            for (int j = 0; j < 5; j++)                   
            {
                _stats[i, j] = Saver.instance.RetrieveInt("s_player_Stat" + i + j, 0);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _talents
        {
            for (int j = 0; j < 7; j++)
            {
                talents[i, j] = Saver.instance.RetrieveInt("s_playerTalents" + i + j, 0) == 0? false : true;
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team attacks
        {
            for (int j = 0; j < 3; j++)
            {
                attacks[i, j] = Saver.instance.RetrieveInt("s_playerAttacks" + i + j, 0);
                attackLevels[i, j] = Saver.instance.RetrieveInt("s_playerAttacksLevel" + i + j, 0);
            }
        }

    }

    public void SaveGame()
    {
        Saver.instance.Save("s_gameExists", 1);

        //------General-----//
        Saver.instance.Save("s_act", act);                   //Act
        Saver.instance.Save("s_gameTime", gameTime);       //Game Time
        for (int i = 0; i < team.Length; i++)               //Team (active characters)
        {
            Saver.instance.Save("s_player" + i, team[i] == true ? 1 : 0) ;
        }

        Saver.instance.Save("s_gold", gold);
        //PlayerPrefs.SetFloat("s_goldBonus", goldBonus);

        Saver.instance.Save("s_relicCount", rStorage.GetAllRelics().Count);
        for (int i = 0; i < rStorage.GetAllRelics().Count; i++)
        {
            Saver.instance.Save("s_relic" + i, rStorage.GetAllRelics()[i].IsSelected()? 1: 0);
        }

        Saver.instance.Save("s_chapter", chapter);

        for (int i = 0; i < globalHotDots.GetLength(0); i++)             //Global hotdots
        {
            for (int j = 0; j < 3; j++)
            {
                Saver.instance.Save("s_globalBuffs_0" + i, globalHotDots[i, 0]);
                Saver.instance.Save("s_globalBuffs_1" + i, globalHotDots[i, 1]);
                Saver.instance.Save("s_globalBuffs_2" + i, globalHotDots[i, 2]);
            }
        }

        //-------World--------//
        Saver.instance.Save("s_playerPosX", (int)playerPos[0]); // Player pos
        Saver.instance.Save("s_playerPosY", (int)playerPos[1]);


        //Types

        Saver.instance.Save("s_worldMatrixSizeX", worldTypes.GetLength(0));
        Saver.instance.Save("s_worldMatrixSizeY", worldTypes.GetLength(1));

        for (int i = 0; i < worldTypes.GetLength(0); i++)
        {
            for (int j = 0; j < worldTypes.GetLength(1); j++)
            {
                Saver.instance.Save("s_worldMatrixType" + i + j, worldTypes[i, j]);
            }

            for (int j = 0; j < worldWeatherTypes.GetLength(1); j++)
            {
                Saver.instance.Save("s_worldMatrixWeather" + i + j, worldWeatherTypes[i, j]);
            }
        }

        //Weathers

        //-----Characters----//
        for (int i = 0; i < team.Length; i++)                   //Team levels
        {
            Saver.instance.Save("s_playerLvL" + i, levels[i]);
        }

        for (int i = 0; i < team.Length; i++)                   //Team unused perks
        {
            Saver.instance.Save("s_playerUnusedPerk" + i, unusedPerks[i]);
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team stats
        {
            for (int j = 0; j < 5; j++)
            {
                Saver.instance.Save("s_playerStat" + i + j, stats[i, j]);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _stats
        {
            for (int j = 0; j < 5; j++)
            {
                Saver.instance.Save("s_player_Stat" + i + j, _stats[i, j]);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _talents
        {
            for (int j = 0; j < 7; j++)
            {
                Saver.instance.Save("s_playerTalents" + i + j, talents[i, j] == true? 1 : 0);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team attacks
        {
            for (int j = 0; j < 3; j++)
            {
                Saver.instance.Save("s_playerAttacks" + i + j, attacks[i, j]);
                Saver.instance.Save("s_playerAttacksLevel" + i + j, attackLevels[i, j]);
            }
        }

    }
    public void StoreData()
    {
        #region storeGeneral
        // ---- General ---- // 
        if (pGenerator == null)
            pGenerator = PathGenerator.instance;
        if (pGenerator == null)
            Debug.LogError("No PathGenerator found when trying to store data");

        act = pGenerator.floor;

        if (statistics == null)
            statistics = StatisticsEnd.instance;
        if (statistics == null)
            Debug.LogError("No StatisticsEnd found when trying to store data");

        gameTime = statistics.GetElapsedTime();

        if (goldManager == null)
            goldManager = GoldManager.instance;
        if (goldManager == null)
            Debug.LogError("No GoldManager found when trying to store data");

        gold = goldManager.GetGold();
        //goldBonus = goldManager.GetGoldBonus();

        if (gManager == null)
            gManager = GameManager.instance;
        if (gManager == null)
            Debug.LogError("No GameManager found when trying to store data");

        team = new bool[totalCharacters]; //Set all to false
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            team[gManager.teamA[i].fUnlockIndex] = true;
        }

        if (aManager == null)
            aManager = AscensionManager.instance;
        if (aManager == null)
            Debug.LogError("No AscensionManager found when trying to store data");
        chapter = aManager.currentAscension;

        if (gBuffs == null)
            gBuffs = GlobalBuffs.instance;
        if(gBuffs == null)
            Debug.LogError("No GlobalBuffs found when trying to store data");

        globalHotDots = gBuffs.GetMatrix();

        if (rStorage == null)
            rStorage = RelicStorage.instance;
        if (rStorage == null)
            Debug.LogError("No RelicStorage found when trying to store data");

        relics = new int[rStorage.GetAllRelics().Count];

        for (int i = 0; i < rStorage.GetAllRelics().Count; i++)
        {
            relics[i] = rStorage.GetAllRelics()[i].IsSelected()? 1: 0;
        }

        #endregion

        #region storeWorld

        if (pSelector == null)
            pSelector = PathSelector.instance;
        if (pSelector == null)
            Debug.LogError("No PathSelector found when trying to store data");

        playerPos = pSelector.currentWorld.matrixIndex;

        if (pSelector == null)
            pSelector = PathSelector.instance;
        if (pSelector == null)
            Debug.LogError("No PathSelector found when trying to store data");

        worldTypes = new int[pGenerator.worldMatrix.GetLength(0), pGenerator.worldMatrix.GetLength(1)];
        worldWeatherTypes = new int[pGenerator.worldMatrix.GetLength(0), pGenerator.worldMatrix.GetLength(1)];

        for (int i = 0; i < pGenerator.worldMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < pGenerator.worldMatrix.GetLength(1); j++)
            {
                if (pGenerator.worldMatrix[i, j] == null)
                {
                    worldTypes[i, j] = 0;
                    worldWeatherTypes[i, j] = -1;
                    continue;
                }

                worldWeatherTypes[i, j] = pGenerator.worldMatrix[i, j].globalIndex;

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.start)
                {
                    worldTypes[i, j] = 1;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.normalCombat)
                {
                    worldTypes[i, j] = 2;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.shop)
                {
                    worldTypes[i, j] = 3;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.blacksmith)
                {
                    worldTypes[i, j] = 4;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.boss)
                {
                    worldTypes[i, j] = 5;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.elite)
                {
                    worldTypes[i, j] = 6;
                    continue;
                }
            }
        }

        //WEATHER TYPES

        #endregion

        #region storeCharacters

        levels = new int[totalCharacters]; //Set all to 0
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            levels[gManager.teamA[i].fUnlockIndex] = gManager.teamA[i].GetComponent<FighterExperience>().GetLevel();
        }

        unusedPerks = new int[totalCharacters]; //Set all to 0
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            unusedPerks[gManager.teamA[i].fUnlockIndex] = gManager.teamA[i].GetComponent<FighterExperience>().GetPerkPoints();
        }

        stats = new int[totalCharacters, 5]; //Set all to 0 //hp, energy, str, def, spd
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            stats[gManager.teamA[i].fUnlockIndex, 0] = gManager.teamA[i].hp;
            stats[gManager.teamA[i].fUnlockIndex, 1] = gManager.teamA[i].energy;
            stats[gManager.teamA[i].fUnlockIndex, 2] = gManager.teamA[i].str;
            stats[gManager.teamA[i].fUnlockIndex, 3] = gManager.teamA[i].def;
            stats[gManager.teamA[i].fUnlockIndex, 4] = gManager.teamA[i].spd;
        }

        _stats = new int[totalCharacters, 5]; //Set all to 0 //hp, energy, str, def, spd
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            _stats[gManager.teamA[i].fUnlockIndex, 0] = gManager.teamA[i]._hp;
            _stats[gManager.teamA[i].fUnlockIndex, 1] = gManager.teamA[i]._energy;
            _stats[gManager.teamA[i].fUnlockIndex, 2] = gManager.teamA[i]._str;
            _stats[gManager.teamA[i].fUnlockIndex, 3] = gManager.teamA[i]._def;
            _stats[gManager.teamA[i].fUnlockIndex, 4] = gManager.teamA[i]._spd;
        }

        experience = new float[totalCharacters]; //Set all to 0
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            experience[gManager.teamA[i].fUnlockIndex] = gManager.teamA[i].GetComponent<FighterExperience>().GetCurrentExp();
        }

        talents = new bool[totalCharacters, 7]; //Set all to false
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            foreach (KeyValuePair<int, PasiveSkill> skills in gManager.teamA[i].GetComponentInChildren<SkillTree>().characterSkillTree)
            {
                talents[gManager.teamA[i].fUnlockIndex, skills.Key] = skills.Value.IsSelected();
            }
        }

        attacks = new int[totalCharacters, 3];

        for (int i = 0; i < attacks.GetLength(0); i++)
        {
            for (int j = 0; j < attacks.GetLength(1); j++)
            {
                attacks[i, j] = -1;
            }
        }

        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            for (int j = 0; j < gManager.teamA[i].attacks.Length; j++)
            {
                attacks[gManager.teamA[i].fUnlockIndex, j] =
                    gManager.teamA[i].attacks[j] == null ? -1 : gManager.teamA[i].attacks[j].GetAttack().generalIndex;

                attackLevels[gManager.teamA[i].fUnlockIndex, j] = 
                    gManager.teamA[i].attacks[j] == null ? -1 : gManager.teamA[i].attacks[j].level;
            }
        }

        #endregion
    }

    public void DeleteSave()
    {
        Saver.instance.Save("s_gameExists", 0);
    }


    #region singleton
    //Singleton
    public static SaveManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
    #endregion
}
