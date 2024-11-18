﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, TargetSelection, RunningTurn, Busy, Bag, PartyScreen, AboutToUse, MoveToForget, BattleOver }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnitSingle;
    [SerializeField] BattleUnit enemyUnitSingle;
    [SerializeField] List<BattleUnit> playerUnitsMulti;
    [SerializeField] List<BattleUnit> playerUnitsMulti2;
    [SerializeField] List<BattleUnit> enemyUnitsMulti;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    //[SerializeField] GameObject shardSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] GameObject singleBattleElements;
    [SerializeField] GameObject multiBattleElements;

    List<BattleUnit> playerUnits;
    List<BattleUnit> playerUnits2;
    List<BattleUnit> enemyUnits;

    List<BattleAction> actions;

    int unitCount = 1;
    int actionIndex = 0;
    BattleUnit currentUnit;

    public event Action<bool> OnBattleOver;

    BattleState state;

    int currentAction;
    int currentMove;
    int currentTarget;
    bool aboutToUseChoice = true;

    SpiritParty playerParty;
    SpiritParty playerParty2;
    SpiritParty handlerParty;
    Spirit wildSpirit;

    bool isHandlerBattle = false;
    PlayerController player;
    PlayerController2 player2;
    HandlerController handler;

    int escapeAttempts;
    MoveBase moveToLearn;
    BattleUnit unitTryingToLearn;

    BattleUnit unitToSwitch;

    public void StartBattle(SpiritParty playerParty, Spirit wildSpirit)
    {
        this.playerParty = playerParty;
        this.wildSpirit = wildSpirit;
        player = playerParty.GetComponent<PlayerController>();
        isHandlerBattle = false;
        unitCount = 1;

        StartCoroutine(SetupBattle());
    }

    public void StartHandlerBattle(SpiritParty playerParty, SpiritParty playerParty2, SpiritParty handlerParty, int unitCount = 2)
    {
        this.playerParty = playerParty;
        this.playerParty2 = playerParty2;
        this.handlerParty = handlerParty;

        isHandlerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        player2 = playerParty2.GetComponent<PlayerController2>();
        handler = handlerParty.GetComponent<HandlerController>();

        this.unitCount = unitCount;

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        // Setup Battle Elements
        multiBattleElements.SetActive(unitCount > 1);

        
       
            playerUnits = playerUnitsMulti;
            enemyUnits = enemyUnitsMulti;
        

        for (int i = 0; i < playerUnits.Count; i++)
        {
            playerUnits[i].Clear();
            playerUnits2[i].Clear();
            enemyUnits[i].Clear();
        }

        if (!isHandlerBattle)
        {
            yield return dialogBox.TypeDialog($"A wild {enemyUnits[0].Spirit.Base.Name} appeared.");
        }
        else
        {

            yield return dialogBox.TypeDialog($"{handler.Name} wants to battle");


            var enemySpirits = handlerParty.GetHealthySpirit(unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                enemyUnits[i].gameObject.SetActive(true);
                enemyUnits[i].Setup(enemySpirits[i]);
            }

            string names = String.Join(" and ", enemySpirits.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"{handler.Name} send out {names}");

            // Send out first Spirit of the player
            playerImage.gameObject.SetActive(false);
            var playerSpirits = playerParty.GetHealthySpirit(unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                playerUnits[i].gameObject.SetActive(true);
                playerUnits[i].Setup(playerSpirits[i]);
            }

            names = String.Join(" and ", playerSpirits.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"Go {names}!");
        }

        escapeAttempts = 0;
        partyScreen.Init();

        actions = new List<BattleAction>();
        ActionSelection(0);
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Spirits.ForEach(p => p.OnBattleOver());

        playerUnits.ForEach(u => u.Hud.ClearData());
        enemyUnits.ForEach(u => u.Hud.ClearData());

        OnBattleOver(won);
    }

    void ActionSelection(int actionIndex)
    {
        state = BattleState.ActionSelection;

        this.actionIndex = actionIndex;
        currentUnit = playerUnits[actionIndex];

        dialogBox.SetMoveNames(currentUnit.Spirit.Moves);

        dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}");
        dialogBox.EnableActionSelector(true);
    }

    void OpenBag()
    {
        state = BattleState.Bag;
        inventoryUI.gameObject.SetActive(true);
    }

    void OpenPartyScreen()
    {
        partyScreen.CalledFrom = state;
        state = BattleState.PartyScreen;
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    void TargetSelection()
    {
        state = BattleState.TargetSelection;
        currentTarget = 0;
    }

    IEnumerator ChooseMoveToForget(Spirit Spirit, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(Spirit.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }

    void AddBattleAction(BattleAction action)
    {
        actions.Add(action);

        // Check if all player actions are selected
        if (actions.Count == unitCount)
        {
            // Choose enemy actions
            foreach (var enemyUnit in enemyUnits)
            {
                var randAction = new BattleAction()
                {
                    Type = ActionType.Move,
                    User = enemyUnit,
                    Target = playerUnits[UnityEngine.Random.Range(0, playerUnits.Count)],
                    Move = enemyUnit.Spirit.GetRandomMove()
                };
                actions.Add(randAction);
            }

            // Sort Actions
            actions = actions.OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.User.Spirit.Speed).ToList();

            StartCoroutine(RunTurns());
        }
        else
        {
            ActionSelection(actionIndex + 1);
        }
    }

    IEnumerator RunTurns()
    {
        state = BattleState.RunningTurn;

        foreach (var action in actions)
        {
            if (action.IsInvalid)
                continue;

            if (action.Type == ActionType.Move)
            {
                yield return RunMove(action.User, action.Target, action.Move);
                yield return RunAfterTurn(action.User);
                if (state == BattleState.BattleOver) yield break;
            }
            else if (action.Type == ActionType.SwitchSpirit)
            {
                state = BattleState.Busy;
                yield return SwitchSpirit(action.User, action.SelectedSpirit);
            }
            else if (action.Type == ActionType.UseItem)
            {
                // This is handled from item screen, so do nothing and skip to enemy move
                dialogBox.EnableActionSelector(false);
            }
            else if (action.Type == ActionType.Run)
            {
                yield return TryToEscape();
            }

            if (state == BattleState.BattleOver) break;
        }

        if (state != BattleState.BattleOver)
        {
            actions.Clear();
            ActionSelection(0);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Spirit.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Spirit);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Spirit);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Spirit.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Spirit, targetUnit.Spirit))
        {

            

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Spirit, targetUnit.Spirit, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Spirit.TakeDamage(move, sourceUnit.Spirit);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Spirit.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Spirit, targetUnit.Spirit, secondary.Target);
                }
            }

            if (targetUnit.Spirit.HP <= 0)
            {
                yield return HandleSpiritFainted(targetUnit);
            }

        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Spirit.Base.Name}'s attack missed");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Spirit source, Spirit target, MoveTarget moveTarget)
    {
        // Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        // Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        // Volatile Status Condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // Statuses like burn or psn will hurt the Spirit after the turn
        sourceUnit.Spirit.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Spirit);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Spirit.HP <= 0)
        {
            yield return HandleSpiritFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Spirit source, Spirit target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Spirit Spirit)
    {
        while (Spirit.StatusChanges.Count > 0)
        {
            var message = Spirit.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandleSpiritFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Spirit.Base.Name} Fainted");
        yield return new WaitForSeconds(2f);

        yield return HandleExpGain(faintedUnit);

        NextStepsAfterFainting(faintedUnit);
    }

    IEnumerator HandleExpGain(BattleUnit faintedUnit)
    {
        if (!faintedUnit.IsPlayerUnit)
        {
            // Exp Gain
            int expYield = faintedUnit.Spirit.Base.ExpYield;
            int enemyLevel = faintedUnit.Spirit.Level;
            

            int expGain = Mathf.FloorToInt((expYield * enemyLevel) / (7 * unitCount));

            for (int i = 0; i < unitCount; i++)
            {
                var playerUnit = playerUnits[i];

                playerUnit.Spirit.Exp += expGain;
                yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} gained {expGain} exp");
                yield return playerUnit.Hud.SetExpSmooth();

                // Check Level Up
                while (playerUnit.Spirit.CheckForLevelUp())
                {
                    playerUnit.Hud.SetLevel();
                    yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} grew to level {playerUnit.Spirit.Level}");

                    // Try to learn a new Move
                    var newMove = playerUnit.Spirit.GetLearnableMoveAtCurrLevel();
                    if (newMove != null)
                    {
                        if (playerUnit.Spirit.Moves.Count < SpiritBase.MaxNumOfMoves)
                        {
                            playerUnit.Spirit.LearnMove(newMove.Base);
                            yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} learned {newMove.Base.Name}");
                            dialogBox.SetMoveNames(playerUnit.Spirit.Moves);
                        }
                        else
                        {
                            unitTryingToLearn = playerUnit;
                            yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} trying to learn {newMove.Base.Name}");
                            yield return dialogBox.TypeDialog($"But it cannot learn more than {SpiritBase.MaxNumOfMoves} moves");
                            yield return ChooseMoveToForget(playerUnit.Spirit, newMove.Base);
                            yield return new WaitUntil(() => state != BattleState.MoveToForget);
                            yield return new WaitForSeconds(2f);
                        }
                    }

                    yield return playerUnit.Hud.SetExpSmooth(true);
                }
            }


            yield return new WaitForSeconds(1f);
        }
    }

    void NextStepsAfterFainting(BattleUnit faintedUnit)
    {
        // Remove the action of fainted Spirit
        var actionToRemove = actions.FirstOrDefault(a => a.User == faintedUnit);
        if (actionToRemove != null)
            actionToRemove.IsInvalid = true;

        if (faintedUnit.IsPlayerUnit)
        {
            var activeSpirits = playerUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = playerParty.GetHealthySpirits(activeSpirits);

            if (activeSpirits.Count == 0 && nextSpirit == null)
            {
                BattleOver(false);
            }
            else if (nextSpirit != null)
            {
                // Send out next Spirit
                unitToSwitch = faintedUnit;
                OpenPartyScreen();
            }
            else if (nextSpirit == null && activeSpirits.Count > 0)
            {
                // No Spirit left to send out but we can stil continue the battle
                playerUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                // Attacks targeted at the removed unit should be changed
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = playerUnits.First());
            }
        }
        else
        {
            if (!isHandlerBattle)
            {
                BattleOver(true);
                return;
            }

            var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);

            if (activeSpirits.Count == 0 && nextSpirit == null)
            {
                BattleOver(true);
            }
            else if (nextSpirit != null)
            {
                // Send out next Spirit
                unitToSwitch = faintedUnit;
                StartCoroutine(SendNextHandlerSpirit());
            }
            else if (nextSpirit == null && activeSpirits.Count > 0)
            {
                // No Spirit left to send out but we can stil continue the battle
                enemyUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                // Attacks targeted at the removed unit should be changed
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = enemyUnits.First());
            }
        }
    }

     IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if (damageDetails.TypeEffectiveness < 1f)
            yield return dialogBox.TypeDialog("It's not very effective!");
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.TargetSelection)
        {
            HandleTargetSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.Bag)
        {
            /*Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = BattleState.ActionSelection;
            };

            Action<ItemBase> onItemUsed = (ItemBase usedItem) =>
            {
                StartCoroutine(OnItemUsed(usedItem));
            };
            
            inventoryUI.HandleUpdate(onBack, onItemUsed); */
        }
        else if (state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if (state == BattleState.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == SpiritBase.MaxNumOfMoves)
                {
                    // Don't learn the new move
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Spirit.Base.Name} did not learn {moveToLearn.Name}"));
                }
                else
                {
                    // Forget the selected move and learn new move
                    var selectedMove = unitTryingToLearn.Spirit.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Spirit.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    unitTryingToLearn.Spirit.Moves[moveIndex] = new Move(moveToLearn);
                }

                moveToLearn = null;
                unitTryingToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                // Fight
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                // Bag
                OpenBag();
            }
            else if (currentAction == 2)
            {
                // Spirit
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                // Run
                var action = new BattleAction()
                {
                    Type = ActionType.Run,
                    User = currentUnit
                };
                AddBattleAction(action);
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, currentUnit.Spirit.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, currentUnit.Spirit.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = currentUnit.Spirit.Moves[currentMove];
            if (move.PP == 0) return;

            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);

            if (enemyUnits.Count > 1)
            {
                TargetSelection();
            }
            else
            {
                var action = new BattleAction()
                {
                    Type = ActionType.Move,
                    User = currentUnit,
                    Target = enemyUnits[0],
                    Move = move
                };
                AddBattleAction(action);
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection(actionIndex);
        }
    }

    void HandleTargetSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentTarget;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentTarget;

        currentTarget = Mathf.Clamp(currentTarget, 0, enemyUnits.Count - 1);

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            enemyUnits[i].SetSelected(i == currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            enemyUnits[currentTarget].SetSelected(false);

            var action = new BattleAction()
            {
                Type = ActionType.Move,
                User = currentUnit,
                Target = enemyUnits[currentTarget],
                Move = currentUnit.Spirit.Moves[currentMove]
            };
            AddBattleAction(action);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            enemyUnits[currentTarget].SetSelected(false);
            MoveSelection();
        }
    }

    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.SelectedMember;
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted Spirit");
                return;
            }
            if (playerUnits.Any(p => p.Spirit == selectedMember))
            {
                partyScreen.SetMessageText("You can't switch with an active Spirit");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.ActionSelection)
            {
                var action = new BattleAction()
                {
                    Type = ActionType.SwitchSpirit,
                    User = currentUnit,
                    SelectedSpirit = selectedMember
                };
                AddBattleAction(action);
            }
            else
            {
                state = BattleState.Busy;
                bool isTrainerAboutToUse = partyScreen.CalledFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchSpirit(unitToSwitch, selectedMember, isTrainerAboutToUse));
                unitToSwitch = null;
            }

            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            if (playerUnits.Any(u => u.Spirit.HP <= 0))
            {
                partyScreen.SetMessageText("You have to choose a Spirit to continue");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.AboutToUse)
            {
                StartCoroutine(SendNextHandlerSpirit());
            }
            else
                ActionSelection(actionIndex);

            partyScreen.CalledFrom = null;
        };

        partyScreen.HandleUpdate(onSelected, onBack);
    }

    void HandleAboutToUse()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice;

        dialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true)
            {
                // Yes Option
                OpenPartyScreen();
            }
            else
            {
                // No Option
                StartCoroutine(SendNextHandlerSpirit());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextHandlerSpirit());
        }
    }

    IEnumerator SwitchSpirit(BattleUnit unitToSwitch, Spirit newSpirit, bool isHandlerAboutToUse = false)
    {
        if (unitToSwitch.Spirit.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Spirit.Base.Name}");
            //unitToSwitch.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        unitToSwitch.Setup(newSpirit);
        dialogBox.SetMoveNames(newSpirit.Moves);
        yield return dialogBox.TypeDialog($"Go {newSpirit.Base.Name}!");

        if (isHandlerAboutToUse)
            StartCoroutine(SendNextHandlerSpirit());
        else
            state = BattleState.RunningTurn;
    }

    IEnumerator SendNextHandlerSpirit()
    {
        state = BattleState.Busy;

        var faintedUnit = enemyUnits.First(u => u.Spirit.HP == 0);

        var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);
        faintedUnit.Setup(nextSpirit);
        yield return dialogBox.TypeDialog($"{handler.Name} send out {nextSpirit.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    /*IEnumerator OnItemUsed(ItemBase usedItem)
    {
        state = BattleState.Busy;
        inventoryUI.gameObject.SetActive(false);

        if (usedItem is ShardItem)
        {
            yield return;
        }

        var action = new BattleAction()
        {
            Type = ActionType.UseItem,
            User = currentUnit
        };
        AddBattleAction(action);
    }*/


    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if (isHandlerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        int playerSpeed = playerUnits[0].Spirit.Speed;
        int enemySpeed = enemyUnits[0].Spirit.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Can't escape!");
                state = BattleState.RunningTurn;
            }
        }
    }
}