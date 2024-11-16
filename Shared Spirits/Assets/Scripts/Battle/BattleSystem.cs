using System;
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
    [SerializeField] List<BattleUnit> enemyUnitsMulti;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] GameObject singleBattleElements;
    [SerializeField] GameObject multiBattleElements;

    List<BattleUnit> playerUnits;
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
    SpiritParty trainerParty;
    Spirit wildSpirit;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;
    MoveBase moveToLearn;
    BattleUnit unitTryingToLearn;

    BattleUnit unitToSwitch;

    public void StartBattle(SpiritParty playerParty, Spirit wildSpirit)
    {
        this.playerParty = playerParty;
        this.wildSpirit = wildSpirit;
        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;
        unitCount = 1;

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty,
        int unitCount = 2)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        this.unitCount = unitCount;

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        // Setup Battle Elements
        singleBattleElements.SetActive(unitCount == 1);
        multiBattleElements.SetActive(unitCount > 1);

        if (unitCount == 1)
        {
            playerUnits = new List<BattleUnit>() { playerUnitSingle };
            enemyUnits = new List<BattleUnit>() { enemyUnitSingle };
        }
        else
        {
            playerUnits = playerUnitsMulti;
            enemyUnits = enemyUnitsMulti;
        }

        for (int i = 0; i < playerUnits.Count; i++)
        {
            playerUnits[i].Clear();
            enemyUnits[i].Clear();
        }

        if (!isTrainerBattle)
        {
            // Wild Pokemon Battle
            playerUnits[0].Setup(playerParty.GetHealthyPokemon());
            enemyUnits[0].Setup(wildPokemon);

            dialogBox.SetMoveNames(playerUnits[0].Pokemon.Moves);
            yield return dialogBox.TypeDialog($"A wild {enemyUnits[0].Pokemon.Base.Name} appeared.");
        }
        else
        {
            // Trianer Battle

            // Show trainer and player sprites
            for (int i = 0; i < playerUnits.Count; i++)
            {
                playerUnits[i].gameObject.SetActive(false);
                enemyUnits[i].gameObject.SetActive(false);
            }

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle");

            // Send out first pokemon of the trainer
            trainerImage.gameObject.SetActive(false);
            var enemyPokemons = trainerParty.GetHealthyPokemons(unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                enemyUnits[i].gameObject.SetActive(true);
                enemyUnits[i].Setup(enemyPokemons[i]);
            }

            string names = String.Join(" and ", enemyPokemons.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {names}");

            // Send out first pokemon of the player
            playerImage.gameObject.SetActive(false);
            var playerPokemons = playerParty.GetHealthyPokemons(unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                playerUnits[i].gameObject.SetActive(true);
                playerUnits[i].Setup(playerPokemons[i]);
            }

            names = String.Join(" and ", playerPokemons.Select(p => p.Base.Name));
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
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());

        playerUnits.ForEach(u => u.Hud.ClearData());
        enemyUnits.ForEach(u => u.Hud.ClearData());

        OnBattleOver(won);
    }

    void ActionSelection(int actionIndex)
    {
        state = BattleState.ActionSelection;

        this.actionIndex = actionIndex;
        currentUnit = playerUnits[actionIndex];

        dialogBox.SetMoveNames(currentUnit.Pokemon.Moves);

        dialogBox.SetDialog($"Choose an action for {currentUnit.Pokemon.Base.Name}");
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

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to change pokemon?");

        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
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
                    Move = enemyUnit.Pokemon.GetRandomMove()
                };
                actions.Add(randAction);
            }

            // Sort Actions
            actions = actions.OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.User.Pokemon.Speed).ToList();

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
            else if (action.Type == ActionType.SwitchPokemon)
            {
                state = BattleState.Busy;
                yield return SwitchPokemon(action.User, action.SelectedPokemon);
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
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {

            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                }
            }

            if (targetUnit.Pokemon.HP <= 0)
            {
                yield return HandlePokemonFainted(targetUnit);
            }

        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
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

        // Statuses like burn or psn will hurt the pokemon after the turn
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return HandlePokemonFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
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

    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.Base.Name} Fainted");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        yield return HandleExpGain(faintedUnit);

        NextStepsAfterFainting(faintedUnit);
    }

    IEnumerator HandleExpGain(BattleUnit faintedUnit)
    {
        if (!faintedUnit.IsPlayerUnit)
        {
            // Exp Gain
            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / (7 * unitCount));

            for (int i = 0; i < unitCount; i++)
            {
                var playerUnit = playerUnits[i];

                playerUnit.Pokemon.Exp += expGain;
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp");
                yield return playerUnit.Hud.SetExpSmooth();

                // Check Level Up
                while (playerUnit.Pokemon.CheckForLevelUp())
                {
                    playerUnit.Hud.SetLevel();
                    yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} grew to level {playerUnit.Pokemon.Level}");

                    // Try to learn a new Move
                    var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                    if (newMove != null)
                    {
                        if (playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
                        {
                            playerUnit.Pokemon.LearnMove(newMove.Base);
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}");
                            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
                        }
                        else
                        {
                            unitTryingToLearn = playerUnit;
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Base.Name}");
                            yield return dialogBox.TypeDialog($"But it cannot learn more than {PokemonBase.MaxNumOfMoves} moves");
                            yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
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
        // Remove the action of fainted pokemon
        var actionToRemove = actions.FirstOrDefault(a => a.User == faintedUnit);
        if (actionToRemove != null)
            actionToRemove.IsInvalid = true;

        if (faintedUnit.IsPlayerUnit)
        {
            var activePokemons = playerUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();
            var nextPokemon = playerParty.GetHealthyPokemon(activePokemons);

            if (activePokemons.Count == 0 && nextPokemon == null)
            {
                BattleOver(false);
            }
            else if (nextPokemon != null)
            {
                // Send out next pokemon
                unitToSwitch = faintedUnit;
                OpenPartyScreen();
            }
            else if (nextPokemon == null && activePokemons.Count > 0)
            {
                // No pokemon left to send out but we can stil continue the battle
                playerUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                // Attacks targeted at the removed unit should be changed
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = playerUnits.First());
            }
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
                return;
            }

            var activePokemons = enemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();
            var nextPokemon = trainerParty.GetHealthyPokemon(activePokemons);

            if (activePokemons.Count == 0 && nextPokemon == null)
            {
                BattleOver(true);
            }
            else if (nextPokemon != null)
            {
                // Send out next pokemon
                unitToSwitch = faintedUnit;

                if (unitCount == 1)
                    StartCoroutine(AboutToUse(nextPokemon));
                else
                    StartCoroutine(SendNextTrainerPokemon());
            }
            else if (nextPokemon == null && activePokemons.Count > 0)
            {
                // No pokemon left to send out but we can stil continue the battle
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
            Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = BattleState.ActionSelection;
            };

            Action<ItemBase> onItemUsed = (ItemBase usedItem) =>
            {
                StartCoroutine(OnItemUsed(usedItem));
            };

            inventoryUI.HandleUpdate(onBack, onItemUsed);
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
                if (moveIndex == PokemonBase.MaxNumOfMoves)
                {
                    // Don't learn the new move
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Pokemon.Base.Name} did not learn {moveToLearn.Name}"));
                }
                else
                {
                    // Forget the selected move and learn new move
                    var selectedMove = unitTryingToLearn.Pokemon.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    unitTryingToLearn.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
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
                // Pokemon
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

        currentMove = Mathf.Clamp(currentMove, 0, currentUnit.Pokemon.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, currentUnit.Pokemon.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = currentUnit.Pokemon.Moves[currentMove];
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
                Move = currentUnit.Pokemon.Moves[currentMove]
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
                partyScreen.SetMessageText("You can't send out a fainted pokemon");
                return;
            }
            if (playerUnits.Any(p => p.Pokemon == selectedMember))
            {
                partyScreen.SetMessageText("You can't switch with an active pokemon");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.ActionSelection)
            {
                var action = new BattleAction()
                {
                    Type = ActionType.SwitchPokemon,
                    User = currentUnit,
                    SelectedPokemon = selectedMember
                };
                AddBattleAction(action);
            }
            else
            {
                state = BattleState.Busy;
                bool isTrainerAboutToUse = partyScreen.CalledFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchPokemon(unitToSwitch, selectedMember, isTrainerAboutToUse));
                unitToSwitch = null;
            }

            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            if (playerUnits.Any(u => u.Pokemon.HP <= 0))
            {
                partyScreen.SetMessageText("You have to choose a pokemon to continue");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.AboutToUse)
            {
                StartCoroutine(SendNextTrainerPokemon());
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
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SwitchPokemon(BattleUnit unitToSwitch, Pokemon newPokemon, bool isTrainerAboutToUse = false)
    {
        if (unitToSwitch.Pokemon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Pokemon.Base.Name}");
            unitToSwitch.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        unitToSwitch.Setup(newPokemon);
        dialogBox.SetMoveNames(newPokemon.Moves);
        yield return dialogBox.TypeDialog($"Go {newPokemon.Base.Name}!");

        if (isTrainerAboutToUse)
            StartCoroutine(SendNextTrainerPokemon());
        else
            state = BattleState.RunningTurn;
    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;

        var faintedUnit = enemyUnits.First(u => u.Pokemon.HP == 0);

        var activePokemons = enemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();
        var nextPokemon = trainerParty.GetHealthyPokemon(activePokemons);
        faintedUnit.Setup(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    IEnumerator OnItemUsed(ItemBase usedItem)
    {
        state = BattleState.Busy;
        inventoryUI.gameObject.SetActive(false);

        if (usedItem is PokeballItem)
        {
            yield return ThrowPokeball((PokeballItem)usedItem);
        }

        var action = new BattleAction()
        {
            Type = ActionType.UseItem,
            User = currentUnit
        };
        AddBattleAction(action);
    }

    IEnumerator ThrowPokeball(PokeballItem pokeballItem)
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal the trainers pokemon!");
            state = BattleState.RunningTurn;
            yield break;
        }

        var playerUnit = playerUnits[0];
        var enemyUnit = enemyUnits[0];

        yield return dialogBox.TypeDialog($"{player.Name} used {pokeballItem.Name.ToUpper()}!");

        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();
        pokeball.sprite = pokeballItem.Icon;

        // Animations
        yield return pokeball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon, pokeballItem);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); ++i)
        {
            yield return new WaitForSeconds(0.5f);
            yield return pokeball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            // Pokemon is caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was caught");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added to your party");

            Destroy(pokeball);
            BattleOver(true);
        }
        else
        {
            // Pokemon broke out
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free");
            else
                yield return dialogBox.TypeDialog($"Almost caught it");

            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }

    int TryToCatchPokemon(Pokemon pokemon, PokeballItem pokeballItem)
    {
        float a = (3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * pokeballItem.CatchRateModifier * ConditionsDB.GetStatusBonus(pokemon.Status) / (3 * pokemon.MaxHp);

        if (a >= 255)
            return 4;

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            ++shakeCount;
        }

        return shakeCount;
    }

    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        int playerSpeed = playerUnits[0].Pokemon.Speed;
        int enemySpeed = enemyUnits[0].Pokemon.Speed;

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