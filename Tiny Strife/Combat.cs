using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : MonoBehaviour
{
    /// <summary>
    /// This class controls the logic and flow of a combat
    /// It grew a lot during the development
    /// Probably I should have splitted it in two or three scripts
    /// But at the end, it works nicely
    /// </summary>

    public Fighter[] teamA, teamB;

    [NonSerialized]
    public Combat3DController combat3Dcontroller;
    private Combat3DUI combat3DUI;

    private Fighter currentFighter;
    private Attack currentAttack;
    public bool skipTurn;
    private bool ended;
    private bool playerWon;

    //List used to sort all fighters by speed
    private List<Fighter> speedList = new List<Fighter>();

    //Events
    public int runningEvents = 0;
    public int damageTakingCalulations = 0;

    //Relic
    Relic attackRelic;

    //Climate
    [NonSerialized]
    public Climate climate;

    [Header("Audio")]
    [SerializeField]
    AudioClip victory;
    [SerializeField]
    AudioClip defeat;

    public void AddRunningEvent()
    {
        runningEvents++;
    }

    public void RemoveRunningEvent()
    {
        runningEvents--;
    }

    public void AddDamageCalculation()
    {
        damageTakingCalulations++;
    }

    public void RemoveDamageCalculation()
    {
        damageTakingCalulations--;
    }

    public bool RunningEvents()
    {
        return runningEvents > 0? true : false;
    }

    private void Start()
    {
        combat3Dcontroller = Combat3DController.instance;
        combat3DUI = Combat3DUI.instance;
    }

    public IEnumerator ResolveTurn(Attack attack, Fighter[] targets)
    {
        combat3DUI.ClearSkills();
        currentAttack = attack;

        AudioManager.instance.PlayAttack(attack.attackSound);

        currentFighter.Get3D().TriggerAttackAnimation();
        if (attack.attackPrefab != null)
        {
            EffectGenerator.instance.SpawnEffect(attack.attackPrefab,
                currentFighter.Get3D().transform);
        }

        if (!attack.specialBehaviour)
        {
            yield return StartCoroutine(currentFighter.ChangeMana(-attack._cost));
            yield return new WaitForSeconds(FastMode.instance.GetAttackDelay());

            //Damage calculation
            float totalDmg = 0;
            if (attack.damage > 0)
            {
                float relicMultiplier = 1.0f;
                if(RelicManager.instance != null && FighterBelongsToTeamA(currentFighter))
                {
                    yield return StartCoroutine(RelicManager.instance.PlayerAttack());
                    relicMultiplier = RelicManager.instance.multiplier;
                }

                for (int i = 0; i < targets.Length; i++)
                {
                    currentFighter.Get3D().TriggerDamageAnimation();
                    if (attack.attackTakePrefab != null)
                    {
                        EffectGenerator.instance.SpawnEffect(attack.attackTakePrefab,
                            targets[i].Get3D().transform);
                    }

                    //Critic chance
                    float dmg = attack.damage;
                    bool critic = false;

                    if (!attack.noCritic && !targets[i].criticInmune)
                    {
                        var random = UnityEngine.Random.Range(0f, 1f);
                        if (random <= currentFighter._critChance)
                        {
                            critic = true;
                            dmg += currentFighter._criticBonus * attack.damage;
                        }
                    }
                    

                    StartCoroutine(targets[i].TakeDamage(dmg * relicMultiplier, currentFighter.GetStr(), critic));
                    while (targets[i].calculatingTotalDmg)
                        yield return 0;
                    totalDmg += targets[i].damageToReceive;
                }

                while (damageTakingCalulations > 0)
                    yield return 0;

                // ------- Events ------ //
                EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
                eventParam.caster = currentFighter;
                eventParam.totalDmg = totalDmg;
                EventManager.TriggerEvent(EventManager.combatEvents.fullDamageCalculation, eventParam);
                while (runningEvents > 0)
                    yield return 0;
                // --------------------- //
            }
            //Healing calculation
            float totalHealing = 0f;
            if (attack.health != 0)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i].defeated)
                        continue;
                    //Critic chance
                    float hl = attack.health;
                    bool critic = false;
                    var random = UnityEngine.Random.Range(0f, 1f);
                    if (random <= currentFighter._critChance)
                    {
                        critic = true;
                        hl += currentFighter._criticBonus * attack.health;
                    }

                    // Wait for last coroutine to finish
                    StartCoroutine(targets[i].Heal(hl, critic));
                    totalHealing += targets[i].healingToReceive;
                }

                while (damageTakingCalulations > 0)
                    yield return 0;

                // ------- Events ------ //
                EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
                eventParam.caster = currentFighter;
                eventParam.totalHealing = totalHealing;
                EventManager.TriggerEvent(EventManager.combatEvents.fullHealingCalculation, eventParam);
                while (runningEvents > 0)
                    yield return 0;
                // --------------------- //
            }
        }
        else
        {
            yield return StartCoroutine(attack.ResolveAttack(currentFighter, targets));
        }

        for (int i = 0; i < attack.hotDot.Length; i++)
        {
            for (int j = 0; j < targets.Length; j++)
            {
                if (targets[j].defeated)
                    continue;

                var hotdot = new HotDot.HotDotInstance(attack.hotDot[i], attack.hotDot[i].uniqueApplied? 
                    attack.hotDot[i].hotdotID : 
                    attack.hotDot[i].hotdotID +attack.attackName + currentFighter.fighterName, currentFighter);


               yield return StartCoroutine(targets[j].GetDotManager().AddNewHotDot(hotdot));
            }
        }

        yield return StartCoroutine(EndTurn());
    }

    IEnumerator EndTurn()
    {
        CombatHoverManager.instance.OnFighterUnhovered(); //Unhover all
        CombatSelection.instance.DeselectAll();

        CheckDeath();
        yield return StartCoroutine(currentFighter.GetDotManager().DoTurn(HotDot.callType.turnEnd));
        CheckDeath();

        if (!ended)
        {
            currentAttack = null;
            // ------- Events ------ //
            EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
            eventParam.caster = currentFighter;
            EventManager.TriggerEvent(EventManager.combatEvents.turnEnd, eventParam);
            while (runningEvents > 0)
                yield return 0;
            // --------------------- //
            CheckDeath();
            if (ended)
                EndCombat();
            yield return StartCoroutine(currentFighter.ChangeMana(5)); //MANA REG
            StartCoroutine(NextTurn());
        }
        else
        {
            EndCombat();
        }
    }

    void EndCombat()
    {
        StartCoroutine(EndCoroutine());
    }

    IEnumerator EndCoroutine()
    {
        if (playerWon)
            AudioManager.instance.PlaySoundAndAtenuateMusic(victory);
        else
            AudioManager.instance.PlaySoundAndAtenuateMusic(defeat);

        yield return StartCoroutine(combat3DUI.ShowTitle(playerWon));

        foreach (var f in GetListOfAliveFighters())
        {
            yield return StartCoroutine(f.GetDotManager().DoTurn(HotDot.callType.combatEnd));
        }

        foreach (var f in teamA)
        {
            f.ClearDots();
        }

        foreach (var f in teamB)
        {
            f.ClearDots();
        }

        // ------- Events ------ //
        EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
        EventManager.TriggerEvent(EventManager.combatEvents.combatEnd, eventParam);
        while (runningEvents > 0)
            yield return 0;
        // --------------------- //


        if (playerWon)
        {
            yield return StartCoroutine(ExperienceManager.instance.GiveExperience());
        }


        if (climate != null)
            climate.End();

        foreach (var f in teamA)
        {
            f.CombatReset();
        }

        foreach (var f in teamB)
        {
            f.CombatReset();
        }

        GameManager.instance.EndOfCombat(playerWon);
    }

    public IEnumerator InitializeCombat(Fighter[] teamA, Fighter[] teamB, List<GlobalBuffs.GlobalHotDot> globalHotDots, Climate climate)
    {
        StopAllCoroutines();

        this.teamA = RelicManager.instance.CombatStart(teamA);

        this.teamB = teamB;
        ended = false;
        currentFighter = null;
        this.climate = climate;

        if(climate != null)
            climate.Setup();

        //combatUI.InitializeCombat(teamA, teamB);
        combat3Dcontroller.Setup(this.teamA, teamB);
        combat3DUI.InitializeUI(this.teamA, teamB);

        foreach (Fighter f in GetAllFighters())
            f.OnDeath.AddListener(CheckDeath);

        if(ExperienceManager.instance != null)
            ExperienceManager.instance.CalculatePotentialExperience(this);

        StoreAllFightersInSpeedList();
        SortSpeedList();
        CombatSpeedElementManager.instance.SetupPanel(speedList);


        // ------- Events ------ //
        EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
        EventManager.TriggerEvent(EventManager.combatEvents.combatStart,eventParam);

        // --------------------- //

        foreach (GlobalBuffs.GlobalHotDot globalHotDot in globalHotDots)
        {
            if(globalHotDot.combatsLeft > 0)
            {
                if (globalHotDot.affectsAllies)
                {
                    for (int i = 0; i < this.teamA.Length; i++)
                    {
                        var hotdot = new HotDot.HotDotInstance(globalHotDot.hotdot, globalHotDot.hotdot.hotdotID + this.teamA[i].fighterName, null);
                        yield return StartCoroutine(this.teamA[i].GetDotManager().AddNewHotDot(hotdot));
                    }
                }
                if (globalHotDot.affectsEnemies)
                {
                    for (int i = 0; i < teamB.Length; i++)
                    {
                        var hotdot = new HotDot.HotDotInstance(globalHotDot.hotdot, globalHotDot.hotdot.hotdotID + teamB[i].fighterName, null);
                        yield return StartCoroutine(teamB[i].GetDotManager().AddNewHotDot(hotdot));
                    }
                }
            }
        }

        if (RelicManager.instance != null)
            RelicManager.instance.InitializeAll();

        StartCoroutine(NextTurn());
    }

    public IEnumerator NextTurn()
    {
        if (ended)
            yield break;

        if (currentFighter != null)
        {
            speedList.Remove(currentFighter);
            CombatSpeedElementManager.instance.SetupPanel(speedList);
        }
        currentFighter = null;

        while(currentFighter == null){

            skipTurn = false;

            if (speedList.Count == 0)
            {
                // ------- Events ------ //
                EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
                EventManager.TriggerEvent(EventManager.combatEvents.allCharactersFinishedTurn, eventParam);
                // --------------------- //

                StoreAllFightersInSpeedList();
                if(speedList.Count == 0)
                {
                    CheckDeath();
                    if(ended)
                        EndCombat();
                }
            }

            SortSpeedList();
            CombatSpeedElementManager.instance.SetupPanel(speedList);

            if (speedList[0].defeated)
            {
                speedList.RemoveAt(0);
                CombatSpeedElementManager.instance.SetupPanel(speedList);
            }
            else
            {
                currentFighter = speedList[0];

                currentFighter.Get3D().OnTurn();
                combat3Dcontroller.SetTurn(currentFighter);
                SetCostOfAttacks();

                if (RelicManager.instance != null && FighterBelongsToTeamA(currentFighter))
                {
                    yield return StartCoroutine(RelicManager.instance.PlayerTurn());
                }

                CheckDeath();
                if (ended)
                {
                    EndCombat();
                    yield break;
                }

                // ------- Events ------ //
                EventManager.CombatEvent eventParam = new EventManager.CombatEvent();
                eventParam.caster = currentFighter;
                EventManager.TriggerEvent(EventManager.combatEvents.turnStart, eventParam);
                while (runningEvents > 0)
                    yield return 0;
                // --------------------- //

                yield return StartCoroutine(currentFighter.GetDotManager().DoTurn(HotDot.callType.turnStart));

                yield return 0;
                while (runningEvents > 0)
                    yield return 0;
                // --------------------- //

                CheckDeath();
                if (ended)
                {
                    EndCombat();
                    yield break;
                }

                if (currentFighter.defeated)
                    currentFighter = null;
            }
            yield return 0;
        }

        foreach (var item in GetListOfAliveFighters())
        {
            item.Get3DUI().GetComponentInChildren<HotDotManagerUI>().AdaptToTurn(currentFighter);
        }

        if (skipTurn)
        {
            yield return new WaitForSeconds(FastMode.instance.GetSkipDelay());
            yield return StartCoroutine(EndTurn());
            yield break;
        }


        combat3DUI.SetTurn(currentFighter);
        //IA case
        if (!FighterBelongsToTeamA(currentFighter))
        {
            var atck = IAAttackSelection.instance.GetRandomDoableAttack(currentFighter);
            CombatSelection.instance.ChooseAttack(atck, currentFighter);
            StartCoroutine(IAAttackSelection.instance.SelectRandomTargets(atck, currentFighter));
        }
    }

    void SetCostOfAttacks()
    {
        foreach (FighterAttack atc in currentFighter.attacks)
        {
            atc.GetAttack()._cost = atc.GetAttack().baseCost;
        }
    }

    public bool PlayerWon()
    {
        return playerWon;
    }

    //Utilities
    bool CombatHasFinished()
    {
        if (GetListOfAliveFightersInTeamX(true).Count == 0 ||
            GetListOfAliveFightersInTeamX(false).Count == 0)
            return true;

        return false;
    }
    void CheckDeath()
    {
        if (CombatHasFinished())
        {
            ended = true;
            if (GetListOfAliveFightersInTeamX(true).Count > 0)
                playerWon = true;
        }
    }
    public List<Fighter> GetAllFighters()
    {
        List<Fighter> allFighters = new List<Fighter>();
        foreach (var fa in teamA)
            allFighters.Add(fa);

        foreach (var fb in teamB)
            allFighters.Add(fb);

        return allFighters;
    }
    private void SortSpeedList()
    {
        speedList.Sort((p1, p2) => -((Fighter)p1).GetSpd().CompareTo(((Fighter)p2).GetSpd()));
    }
    public bool FighterBelongsToTeamA(Fighter fighter)
    {
        foreach (var fighterA in teamA)
        {
            if (fighter == fighterA)
                return true;
        }

        return false;
    }
    public bool FightersAreOnSameTeam(Fighter f1, Fighter f2)
    {
        if (FighterBelongsToTeamA(f1) && FighterBelongsToTeamA(f2) ||
            !FighterBelongsToTeamA(f1) && !FighterBelongsToTeamA(f2))
            return true;
        return false;
    }
    public List<Fighter> GetListOfAliveFightersInTeamX(bool isTeamA)
    {
        List<Fighter> team = new List<Fighter>();
        if (isTeamA)
        {
            foreach (var fighter in teamA)
            {
                if (!fighter.defeated)
                    team.Add(fighter);
            }
        }
        else
        {
            foreach (var fighter in teamB)
            {
                if (!fighter.defeated)
                    team.Add(fighter);
            }
        }
        return team;
    }

    public List<Fighter> GetListOfAliveFighters()
    {
        List<Fighter> team = new List<Fighter>();

        foreach (var fighter in teamA)
        {
            if (!fighter.defeated)
                team.Add(fighter);
        }

        foreach (var fighter in teamB)
        {
            if (!fighter.defeated)
                team.Add(fighter);
        }

        return team;
    }


    private void StoreAllFightersInSpeedList()
    {
        foreach (var fighterA in teamA)
        {
            if(fighterA._hp >0)
                speedList.Add(fighterA);
        }
        foreach (var fighterB in teamB)
        {
            if (fighterB._hp > 0)
                speedList.Add(fighterB);
        }
    }

    public int AliveMemebersOnTeamX(bool isTeamA)
    {
        var count = 0;
        if (isTeamA)
        {
            foreach (var fighter in teamA)
            {
                if (!fighter.defeated)
                    count++;
            }

            return count;
        }
        else
        {
            foreach (var fighter in teamB)
            {
                if (!fighter.defeated)
                    count++;
            }

            return count;
        }

    }
    public int AliveMembers()
    {
        return AliveMemebersOnTeamX(true) + AliveMemebersOnTeamX(false);
    }
    public Fighter GetCurrentFighter()
    {
        return currentFighter;
    }

    public Attack GetCurrentAttack()
    {
        return currentAttack;
    }

    #region singleton
    //Singleton
    public static Combat instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    #endregion
}
