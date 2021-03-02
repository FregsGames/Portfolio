using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    Combat combat;
    PauseManager pause;
    CombatUI combatUI;
    PlayerMovement pMovement;
    ConversationManager cManager;
    Interaction interaction;
    PlayerTeamManager pTeam;
    PlayerBag pBag;
    RandomCombatManager rCombat;
    PCManager pcManager;
    TeamInspect tInspect;
    EvolveManager evolveManager;
    AttackLearnManager attackLearnManager;

    Vector2 inputs = Vector2.zero;

    private void Start()
    {
        combat = Combat.instance;
        evolveManager = EvolveManager.instance;
        pBag = PlayerBag.instance;
        attackLearnManager = AttackLearnManager.instance;
        tInspect = TeamInspect.instance;
        rCombat = RandomCombatManager.instance;
        pcManager = PCManager.instance;
        pTeam = PlayerTeamManager.instance;
        pause = PauseManager.instance;
        combatUI = CombatUI.instance;
        cManager = ConversationManager.instance;
        pMovement = FindObjectOfType<PlayerMovement>();
        interaction = FindObjectOfType<Interaction>();
    }

    void Update()
    {

        if (cManager.OnConversation() && Input.GetMouseButtonDown(0))
        {
            cManager.ReceiveAcceptInput();
            return;
        }

        if (cManager.OnConversation())
            return;

        if (attackLearnManager.OnLearning())
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                attackLearnManager.AcceptInput();
                return;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace))
            {
                attackLearnManager.Dismiss();
                return;
            }

            inputs.x = Input.GetAxisRaw("Horizontal");
            inputs.y = Input.GetAxisRaw("Vertical");

            if (inputs.magnitude > 0)
                attackLearnManager.ReceiveInput(inputs);

            return;
        }

        if (evolveManager.OnEvolve())
        {
            if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Backspace))
            {
                evolveManager.AbortCancelation();
                return;

            }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace))
            {
                evolveManager.CancelEvolve();
                return;
            }
        }

        if (tInspect.OnTeamInspect())
        {
            if (Input.GetMouseButtonDown(1))
            {
                tInspect.Dismiss();
            }
            return;
        }

        if (pTeam.ShowingTeam())
        {
            inputs.x = Input.GetAxisRaw("Horizontal");
            inputs.y = Input.GetAxisRaw("Vertical");
            if (Input.GetMouseButtonDown(0))
            {
                pTeam.AcceptInput();
                return;
            }
            pTeam.ReceiveInput(inputs);
            return;
        }

        if (pBag.OnBag())
        {
            inputs.x = Input.GetAxisRaw("Horizontal");
            inputs.y = Input.GetAxisRaw("Vertical");

            if (Input.GetMouseButtonDown(0))
            {
                pBag.AcceptInput();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                pBag.Back();
                return;
            }

            if(inputs.magnitude > 0)
               pBag.ReceiveInput(inputs);
            return;
        }

        if (pcManager.OnPc())
        {
            inputs.x = Input.GetAxisRaw("Horizontal");
            inputs.y = Input.GetAxisRaw("Vertical");
            if (Input.GetKeyDown(KeyCode.Return))
            {
                pcManager.AcceptInput();
                return;
            }

            if (Input.GetMouseButtonDown(1) ||Input.GetKeyDown(KeyCode.Backspace))
            {
                pcManager.Dismiss();
                return;
            }


            if (inputs.magnitude > 0)
                pcManager.ReceiveInput(inputs);
            return;
        }


        if (PauseManager.instance.IsPaused())
        {
            if (inputs.y != 0)
            {
                pause.ReceiveInput(inputs);
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                pause.AcceptInput();
                return;
            }
        }
        if (combat.OnCombat())
        {
            if (Input.GetMouseButtonDown(0))
            {
                combatUI.AcceptInput();
                return;
            }
            Vector2 direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if(direction.magnitude > 0)
                combatUI.ReceiveInput(direction);
        }

        if (!combat.OnCombat() && !rCombat.OnPrevToCombat())
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                pause.TogglePause();
            }
        }

        if (!combat.OnCombat() && !pause.IsPaused() && !rCombat.OnPrevToCombat())
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                pMovement.ToggleAutoRun();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!pcManager.OnPc())
                    pcManager.ShowPC();
                else
                    pcManager.ExitPC();
            }

            if (Input.GetMouseButtonDown(0))
            {
                StartCoroutine(interaction.Interact());
            }

            pMovement.ResolveMovement(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"),
                Input.GetAxis("Fire3") > 0 ? true : false);
        }


    }

    #region Singleton
    public static InputManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    #endregion
}
