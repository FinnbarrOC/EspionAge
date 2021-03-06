﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
using NaughtyAttributes;
using NPCs;

public class MissionTutorial : AMission
{
    public Vector3 playerStartPosition;
    public Vector3 playerStartRotation;
    public Vector3 playerRespawnPosition;
    public Vector3 playerRespawnRotation;

    [BoxGroup("Nurse Room Sequence")] public MissionObject tutorialNurse;
    [BoxGroup("Nurse Room Sequence")] public string tutorialNurseSpeakerId;
    [FMODUnity.EventRef]
    [BoxGroup("Nurse Room Sequence")] public string tutorialNurseVoicePath;
    [ReorderableList]
    [BoxGroup("Nurse Room Sequence")]  public List<Conversation> nurseConversations;
    [ReorderableList]
    [BoxGroup("Nurse Room Sequence")] public List<Conversation> birdieCaughtConversations;
    [BoxGroup("Nurse Room Sequence")] public Conversation cantEscapeYetConversation;
    [ReorderableList]
    [BoxGroup("Nurse Room Sequence")] public List<NPCs.Components.PatrolWaypoint> lostBirdieWaypoints;
    [ReorderableList]
    [BoxGroup("Nurse Room Sequence")] public List<Conversation> lostBirdieConversations;
    [BoxGroup("Nurse Room Sequence")] public MissionObject otherBedNPC;
    [BoxGroup("Nurse Room Sequence")] public string otherBedNPCSpeakerId;
    [FMODUnity.EventRef]
    [BoxGroup("Nurse Room Sequence")] public string otherBedNPCVoicePath;
    [BoxGroup("Nurse Room Sequence")] public List<Conversation> otherBedNPCConversations;

    // private nurse room variables
    private bool nurseIsFollowingPlayer = false;
    private bool canEscape = false;
    private bool isInCantEscapeConversation = false;
    private TutorialNurse tutorialNurseAI;
    private FieldOfVision tutorialNurseFOV;
    private int currentCaughtConversationIndex = 0;
    private Conversation currentNurseConversation;
    private Conversation currentLostBirdieConversation;
    private Conversation currentOtherNPCConversation;

    [Header("Cutscenes")]
    public List<string> startCutsceneTexts;
    public GameObject awakenessPointerUIAnimation;
    public GameObject vaseFocusCameraPrefab;
    public GameObject vaseDropCutsceneText;
    public GameObject enemyFocusCameraPrefab;
    public GameObject enemyCutsceneText;
    public GameObject birdieRunawayCutsceneText;
    public GameObject specialAbilityPointerUIAnimation;

    [Header("Vase - General")]
    public GameObject vasePrefab;
    public GameObject vaseStandPrefab;

    [Header("First Vase")]
    public Vector3 firstVasePosition;
    private SpawnedVase firstVase;

    [Header("Row of Vases")]
    [ReorderableList]
    public List<Vector3> vasePositions;
    private List<SpawnedVase> spawnedVases;
    private List<GameObject> spawnedBrokenVases;
    // stand position must then be, (x - 0.5, 1.5, z - 1) (empirically)

    [Header("Chaser Enemies")]
    public List<GameObject> chaserPrefabs;
    [MinMaxSlider(0.5f, 2f)] public Vector2 randomWidthMultiplierRange;
    [MinMaxSlider(0.5f, 2f)] public Vector2 randomHeightMultiplierRange;
    public float enemyCutsceneAnimationSpeed = 0.2f;
    public List<TutorialChaserGroup> chaserGroups;

    [Header("Misc. Objects")]
    public List<MissionObject> extraObjects;

    [Header("Post-Tutorial Cleaning")] [ReorderableList]
    public List<Conversation> cleaningConversations;

    [Header("FMOD Audio")]
    private FMODUnity.StudioEventEmitter musicEv;

    private bool startCutscenePlayed = false;
    private bool respawning = false;
    private bool missionCompleting = false;

    private List<SpawnedEnemy> spawnedEnemies;

    [System.Serializable]
    public class TutorialChaserGroup
    {
        public float startChaseRadius;
        public float chaseSpeed;
        public List<Vector3> enemyStartPositions;
    }

    public class SpawnedVase
    {
        public GameObject vaseObject;
        public GameObject vaseStand;
        public LoudObject loudObject;
        public BreakableObject breakableObject;

        public SpawnedVase(GameObject o, GameObject s)
        {
            vaseObject = o;
            vaseStand = s;

            loudObject = Utils.GetRequiredComponentInChildren<LoudObject>(vaseObject);
            breakableObject = Utils.GetRequiredComponentInChildren<BreakableObject>(vaseObject);
        }
    }

    public class SpawnedEnemy
    {
        public GameObject gameObject;
        public PureChaser pureChaser;
        public TutorialChaserGroup chaserGroup;
        public NPCBark npcBark;
        public NPCInteractable npcInteractable;

        public SpawnedEnemy(GameObject gameObject, PureChaser pureChaser, TutorialChaserGroup chaserGroup, NPCBark npcBark, NPCInteractable npcInteractable)
        {
            this.gameObject = gameObject;
            this.pureChaser = pureChaser;
            this.chaserGroup = chaserGroup;
            this.npcBark = npcBark;
            this.npcInteractable = npcInteractable;
        }
    }
    
    private void Awake()
    {
        spawnedVases = new List<SpawnedVase>();
        spawnedBrokenVases = new List<GameObject>();
        spawnedEnemies = new List<SpawnedEnemy>();
        musicEv = GetComponent<FMODUnity.StudioEventEmitter>();
    }

    protected override void Initialize()
    {
        Debug.Log("MissionTutorial Initialize()!");

        // Force fade out
        UIManager.Instance.InstantFadeOut();

        // Init some general vars
        UIManager.Instance.CanPause = false;

        // Move player to a set position
        GameManager.Instance.GetPlayerController().EnablePlayerInput = false;
        GameManager.Instance.GetPlayerRigidbody().isKinematic = true;  // to stop collisions with the bed for the wake up animation
        GameManager.Instance.GetPlayerTransform().position = playerStartPosition;
        GameManager.Instance.GetPlayerTransform().rotation = Quaternion.Euler(playerStartRotation);

        // Toggle the event in the EventManager
        GameEventManager.Instance.SetEventStatus(GameEventManager.GameEvent.TutorialActive, true);

        // spawn the specific first vase we will drop, and the remaining ones
        firstVase = SpawnVase(firstVasePosition, trackBrokenVase: false);
        firstVase.loudObject.dropRadius = 0f;

        // Spawn all the other vases
        SpawnRegularVases();
        SpawnExtraObjects();

        // Listen for the player to pass through the final door to finish the mission
        RegionManager.Instance.finalHallwayDoor.OnPlayerPassThrough += CompleteMission;

        // Audio parameters setting
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("ChaseEnd", 0f);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("ChaseStarts", 0f);

        StartCoroutine(StartMissionLogic());
    }

    private void HandleCouldNotFindBirdie()
    {
        tutorialNurseAI.OnCouldNotFindBirdie -= HandleCouldNotFindBirdie;

        UnlockDoorAndDisableNurseComponents();

        tutorialNurseAI.SetLostBirdieWaypoints(lostBirdieWaypoints);
        PlaySequentialConversationsSequential();
    }

    private void PlaySequentialConversationsSequential()
    {
        if (lostBirdieConversations.Count > 0)
        {
            currentLostBirdieConversation = lostBirdieConversations[0];
            lostBirdieConversations.RemoveAt(0);

            DialogueManager.Instance.StartConversation(currentLostBirdieConversation);
            DialogueManager.Instance.OnFinishConversation += OnFinishLostBirdieConversation;
        }
        else
        {
            currentLostBirdieConversation = null;
        }
    }

    private void OnFinishLostBirdieConversation(Conversation conversation)
    {
        if (conversation != currentLostBirdieConversation) return;
        DialogueManager.Instance.OnFinishConversation -= OnFinishLostBirdieConversation;
        PlaySequentialConversationsSequential();
    }

    private void HandleTutorialNurseSpottingPlayer()
    {
        // If they are returning, we want them to fully return before starting a conversation again
        if (nurseIsFollowingPlayer || tutorialNurseAI.currentState == TutorialNurseStates.Returning) return;

        if (DialogueManager.Instance.IsConversationActive(currentOtherNPCConversation))
        {
            tutorialNurseAI.SetFoundBirdie();  // so he doesnt freak out
            tutorialNurseAI.ReturnToOrigin();  // go back to origin
            return;
        }

        if (RegionManager.Instance.PlayerIsInRegion(RegionManager.Instance.nurseRoomBirdiesBedArea))
        {
            if (nurseConversations.Count == 0) return;

            currentNurseConversation = nurseConversations.First();
            DialogueManager.Instance.StartConversation(currentNurseConversation);
            tutorialNurseAI.SetFoundBirdie();

            nurseConversations.RemoveAt(0);
            if (nurseConversations.Count == 0)
            {
                UnlockDoorAndDisableNurseComponents();
                return;
            }

            DialogueManager.Instance.OnFinishConversation += StopFollowingAfterConversationEnds;
            tutorialNurseAI.StopMovement();
        }
        else if (RegionManager.Instance.PlayerIsInRegion(RegionManager.Instance.nurseRoomDoorArea) || 
            RegionManager.Instance.PlayerIsInRegion(RegionManager.Instance.nurseRoomOtherBedArea))
        {
            if (birdieCaughtConversations.Count == 0)
            {
                Debug.LogError("Need at least one element in birdieCaughtConversations!");
                return;
            }
            currentNurseConversation = birdieCaughtConversations[currentCaughtConversationIndex++ % birdieCaughtConversations.Count];
            DialogueManager.Instance.StartConversation(currentNurseConversation);
            nurseIsFollowingPlayer = true;

            tutorialNurseAI.StartFollowingPlayer();
            tutorialNurseAI.SetFoundBirdie();
            WorldObjectivePointer.Instance.PointTo(playerStartPosition, RegionManager.Instance.nurseRoomBirdiesBedArea);

            RegionManager.Instance.OnPlayerEnterRegion += WaitForBirdieToGoBackToBed;
        }
        else
        {
            Debug.LogError("Player is not in any of the expected nurse room regions when tutorial nurse spotted her!");
        }
    }

    private void StopFollowingAfterConversationEnds(Conversation conversation)
    {
        if (conversation != currentNurseConversation) return;
        DialogueManager.Instance.OnFinishConversation -= StopFollowingAfterConversationEnds;

        tutorialNurseAI.ReturnToOrigin();
        nurseIsFollowingPlayer = false;
    }

    private void WaitForBirdieToGoBackToBed(RegionTrigger region)
    {
        if (region != RegionManager.Instance.nurseRoomBirdiesBedArea) return;
        RegionManager.Instance.OnPlayerEnterRegion -= WaitForBirdieToGoBackToBed;

        tutorialNurseAI.ReturnToOrigin();
        nurseIsFollowingPlayer = false;
    }

    private void UnlockDoorAndDisableNurseComponents()
    {
        canEscape = true;

        // Disable events were we previously listening for
        tutorialNurseAI.OnCouldNotFindBirdie -= HandleCouldNotFindBirdie;
        tutorialNurseFOV.OnTargetSpotted -= HandleTutorialNurseSpottingPlayer;
        RegionManager.Instance.nurseRoomDoor.OnPlayerCollideWithDoor -= OnPlayerCollideWithNurseRoomDoor;

        // Hide the FOV now (if we disable the component it'll go wonky and freeze the fov mesh weirdly)
        tutorialNurseFOV.viewRadius = 0f;

        // Make the nurse go back to his chair + make him stay there (it's his originWaypoint)
        tutorialNurseAI.ReturnThenIdle();

        // Tell the user they found the right way out!
        ObjectiveList.Instance.CrossOutObjectiveText();

        // Unlock the door (conversations will be clear to the player that they can leave now)
        RegionManager.Instance.nurseRoomDoor.SetLocked(false);

        // Others...
        // TODO: door unlock sound
    }

    private void CompleteMission()
    {
        if (missionCompleting || respawning) return;

        RegionManager.Instance.finalHallwayDoor.OnPlayerPassThrough -= CompleteMission;
        missionCompleting = true;

        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("ChaseEnd", 1f);

        spawnedEnemies.ForEach(e =>
        {
            e.pureChaser.enemy.OnCollideWithPlayer -= RestartAfterCutscene;
            e.pureChaser.enemy.enabled = false;
            e.pureChaser.StartCleaning();
            e.pureChaser.enabled = false;
            e.pureChaser.agent.enabled = false;

            e.gameObject.GetComponent<CinemachineCollisionImpulseSource>().enabled = false;
            e.gameObject.GetComponent<Collider>().isTrigger = false;
            e.gameObject.GetComponent<Rigidbody>().isKinematic = false;

            e.npcBark.StopCurrentBark();

            e.gameObject.GetComponent<SpeakerUI>().SetIsClamp(true);
        });

        // Trigger the final bark set for only the first enemy
        if (spawnedEnemies.Count > 0)
        {
            spawnedEnemies[0].npcBark.TriggerBark(NPCBarkTriggerType.TutorialChaseOver);
        }

        // finally stop all barks completely after the final one being triggered, and start setting up cleaning conversations
        spawnedEnemies.ForEach(e =>
        {
            e.npcBark.enabled = false;
            e.npcInteractable.enabled = true;
        });

        // Assign all conversations
        int currentEnemyIndex = 0;
        foreach (Conversation conversation in cleaningConversations)
        {
            Conversation conversationCopy = Conversation.CreateCopy(conversation);

            SpawnedEnemy currentEnemy = spawnedEnemies[currentEnemyIndex % spawnedEnemies.Count];
            for (int i = 0; i < conversationCopy.lines.Length; i++)
            {
                if (string.IsNullOrEmpty(conversationCopy.lines[i].id.Trim()))
                {
                    conversationCopy.lines[i].id = DialogueManager.Instance.GetSpeakerId(currentEnemy.gameObject);
                }
            }
            currentEnemy.npcInteractable.defaultConvos.Add(conversationCopy);
            currentEnemyIndex++;
        }

        AlertMissionComplete();
        MissionManager.Instance.EndMission(MissionsEnum.MissionTutorial);
    }

    private IEnumerator StartMissionLogic()
    {
        // The starting rolling text... maybe one day we'll remove this
        foreach (string text in startCutsceneTexts)
        {
            yield return UIManager.Instance.textOverlay.SetText(text);
        }
        UIManager.Instance.zoneText.ClearText(); // instant clear it to type it out when we fade in later

        // Tutorial Nurse Section Init: add as speaker, init with ai components to respond to certain events
        tutorialNurse.spawnedInstance = MissionManager.Instance.SpawnMissionObject(tutorialNurse);
        DialogueManager.Instance.AddSpeaker(
            new SpeakerContainer(
                tutorialNurseSpeakerId,
                tutorialNurse.spawnedInstance,
                tutorialNurseVoicePath));
        tutorialNurseAI = Utils.GetRequiredComponent<TutorialNurse>(tutorialNurse.spawnedInstance);
        tutorialNurseAI.OnCouldNotFindBirdie += HandleCouldNotFindBirdie;  // make the ai freak out when they cant find birdie
        tutorialNurseFOV = Utils.GetRequiredComponent<FieldOfVision>(tutorialNurse.spawnedInstance);
        tutorialNurseFOV.OnTargetSpotted += HandleTutorialNurseSpottingPlayer;  // we can handle convos that occur when nurse spots birdie

        // Spawn the other bed NPC by the window
        otherBedNPC.spawnedInstance = MissionManager.Instance.SpawnMissionObject(otherBedNPC);
        DialogueManager.Instance.AddSpeaker(
            new SpeakerContainer(
                otherBedNPCSpeakerId,
                otherBedNPC.spawnedInstance,
                otherBedNPCVoicePath));
        if (otherBedNPCConversations.Count == 0)
        {
            Debug.LogError("Need at least one conversation set for otherBedNPCConversations!");
        }
        else
        {
            currentOtherNPCConversation = otherBedNPCConversations.First();
            otherBedNPC.spawnedInstance.GetComponent<NPCInteractable>().defaultConvos = new List<Conversation>() { currentOtherNPCConversation };
            otherBedNPCConversations.RemoveAt(0);
            DialogueManager.Instance.OnFinishConversation += LoadNextOtherNPCConversation;
        }

        // Fade in and allow to pause, and lock the door to start
        UIManager.Instance.FadeIn();
        UIManager.Instance.CanPause = true;
        RegionManager.Instance.nurseRoomDoor.SetLocked(true);
        RegionManager.Instance.nurseRoomDoor.OnPlayerCollideWithDoor += OnPlayerCollideWithNurseRoomDoor;

        // Wake up cutscene + enabling movement afterwards
        if (!GameManager.Instance.skipSettings.allRealtimeCutscenes)
        {
            GameManager.Instance.GetPlayerAnimator().SetTrigger(Constants.ANIMATION_BIRDIE_WAKEUP);
            float animationLength = GameManager.Instance.GetPlayerAnimator().GetCurrentAnimatorClipInfo(0)[0].clip.length; // should always be there
            yield return new WaitForSeconds(animationLength * 0.8f);  // fine-tuned for best visuals
        }
        GameManager.Instance.GetPlayerRigidbody().isKinematic = false;
        GameManager.Instance.GetPlayerController().EnablePlayerInput = true;

        // we type it here because the user finally gains control, so it looks cool to then type the location they're in
        UIManager.Instance.zoneText.DisplayText(RegionManager.Instance.GetPlayerCurrentZone().regionName, RegionManager.Instance.GetPlayerCurrentZone().isRestricted);

        // Cutscene for once they enter the hallway
        RegionManager.Instance.nurseRoomDoor.OnDoorClose += OnNurseRoomDoorClose;
    }

    private void LoadNextOtherNPCConversation(Conversation conversation)
    {
        if (conversation == currentOtherNPCConversation)
        {
            if (otherBedNPCConversations.Count == 0)
            {
                DialogueManager.Instance.OnFinishConversation -= LoadNextOtherNPCConversation;
            }
            else
            {
                currentOtherNPCConversation = otherBedNPCConversations.First();
                otherBedNPC.spawnedInstance.GetComponent<NPCInteractable>().defaultConvos = new List<Conversation>() { currentOtherNPCConversation };
                otherBedNPCConversations.RemoveAt(0);
            }
        }
    }

    private void OnPlayerCollideWithNurseRoomDoor()
    {
        if (canEscape || isInCantEscapeConversation) return;

        isInCantEscapeConversation = true;
        DialogueManager.Instance.StartConversation(cantEscapeYetConversation);
        DialogueManager.Instance.OnFinishConversation += WaitForCantFinishConversationToFinish;
    }

    private void WaitForCantFinishConversationToFinish(Conversation conversation)
    {
        if (conversation == cantEscapeYetConversation)
        {
            DialogueManager.Instance.OnFinishConversation -= WaitForCantFinishConversationToFinish;
            isInCantEscapeConversation = false;
        }
    }

    private void OnNurseRoomDoorClose()
    {
        // we only care if the door closes and the player is completely out of the nurse room
        if (!RegionManager.Instance.PlayerIsInZone(RegionManager.Instance.nursesRoom))
        {
            RegionManager.Instance.nurseRoomDoor.OnDoorClose -= OnNurseRoomDoorClose;

            if (currentLostBirdieConversation != null)
            {
                DialogueManager.Instance.OnFinishConversation -= OnFinishLostBirdieConversation;
                DialogueManager.Instance.ResolveConversation(currentLostBirdieConversation);
            }
            musicEv.Play();

            startCutscenePlayed = true;
            firstVase.loudObject.Drop();
            firstVase.breakableObject.OnBreak += StartVaseFocus;
        }
    }

    private void StartVaseFocus(GameObject brokenInstance)
    {
        StartCoroutine(VaseCutsceneCoroutine(brokenInstance));
    }

    private IEnumerator VaseCutsceneCoroutine(GameObject focusObject)
    {
        RegionManager.Instance.hallwayToHedgeMazeDoor1.SetLocked(true);
        RegionManager.Instance.hallwayToHedgeMazeDoor2.SetLocked(true);

        GameManager.Instance.GetPlayerController().EnablePlayerInput = false;
        UIManager.Instance.CanPause = false;
        UIManager.Instance.staminaBar.overrideValue = true;
        UIManager.Instance.staminaBar.overrideTo = 0f;
        CinemachineVirtualCamera currentCamera = CameraManager.Instance.GetActiveVirtualCamera();

        // Cutscene Order: Vase Focus -> Outline Awakeness Meter -> Enemies Come -> Birdie Runs Away
        Time.timeScale = 0f;
        yield return StartCoroutine(MissionManager.Instance.PlayCutsceneText(awakenessPointerUIAnimation));
        Time.timeScale = 1f;
        yield return StartCoroutine(MissionManager.Instance.PlayCutscenePart(currentCamera, vaseFocusCameraPrefab, vaseDropCutsceneText, focusObject.transform));
        SpawnEnemies();
        SetEnemyAnimationSpeed(enemyCutsceneAnimationSpeed);  // make all slower than usual for now
        yield return StartCoroutine(MissionManager.Instance.PlayCutscenePart(currentCamera, enemyFocusCameraPrefab, enemyCutsceneText, spawnedEnemies[0].gameObject.transform, doHardBlend: true));
        ResetEnemySpeed();  // reset to assigned speeds
        GameManager.Instance.GetPlayerController().EnablePlayerInput = true;
        UIManager.Instance.CanPause = true;
        UIManager.Instance.staminaBar.OnLightningEnabled += StartSpecialAbilityTutorial;
        UIManager.Instance.staminaBar.overrideValue = false;

        yield return StartCoroutine(MissionManager.Instance.PlayCutsceneText(birdieRunawayCutsceneText));
    }

    private void StartSpecialAbilityTutorial(bool enabled)
    {
        if (!enabled) return;

        // this is the moment we've all been waiting for
        UIManager.Instance.staminaBar.OnLightningEnabled -= StartSpecialAbilityTutorial;

        StartCoroutine(DisplaySpecialAbilityTutorial());
    }

    private IEnumerator DisplaySpecialAbilityTutorial()
    {
        if (GameManager.Instance.skipSettings.allRealtimeCutscenes) yield break;

        Time.timeScale = 0f;
        UIManager.Instance.CanPause = false;
        GameManager.Instance.GetPlayerController().EnablePlayerInput = false;
        yield return StartCoroutine(MissionManager.Instance.PlayCutsceneText(specialAbilityPointerUIAnimation));
        GameManager.Instance.GetPlayerController().EnablePlayerInput = true;
        UIManager.Instance.CanPause = true;
        Time.timeScale = 1f;
    }

    private void SpawnEnemies()
    {
        chaserGroups.ForEach(group =>
        {
            group.enemyStartPositions.ForEach(position =>
            {
                GameObject enemyInstance = Instantiate(chaserPrefabs[Random.Range(0, chaserPrefabs.Count)], position, Quaternion.identity);
                Vector3 originalScale = enemyInstance.transform.localScale;
                enemyInstance.transform.localScale = new Vector3(
                    originalScale.x * Random.Range(randomWidthMultiplierRange.x, randomWidthMultiplierRange.y),
                    originalScale.y * Random.Range(randomHeightMultiplierRange.x, randomHeightMultiplierRange.y),
                    originalScale.z * Random.Range(randomWidthMultiplierRange.x, randomWidthMultiplierRange.y));

                PureChaser chaser = Utils.GetRequiredComponent<PureChaser>(enemyInstance);
                chaser.targetTransform = GameManager.Instance.GetPlayerTransform();
                chaser.SetSpeed(group.chaseSpeed);
                chaser.startChaseRadius = group.startChaseRadius;
                chaser.enemy.OnCollideWithPlayer += RestartAfterCutscene;

                NPCBark npcBark = Utils.GetRequiredComponent<NPCBark>(enemyInstance);
                NPCInteractable npcInteractable = Utils.GetRequiredComponent<NPCInteractable>(enemyInstance);
                npcInteractable.enabled = false;

                spawnedEnemies.Add(new SpawnedEnemy(enemyInstance, chaser, group, npcBark, npcInteractable));
            });
        });
    }

    private void SetEnemyAnimationSpeed(float speed)
    {
        spawnedEnemies.ForEach(enemy =>
        {
            PureChaser chaser = Utils.GetRequiredComponent<PureChaser>(enemy.gameObject);
            chaser.SetAnimationSpeed(speed);
        });
    }

    private void ResetEnemySpeed()
    {
        spawnedEnemies.ForEach(enemy =>
        {
            PureChaser chaser = Utils.GetRequiredComponent<PureChaser>(enemy.gameObject);
            chaser.SetAnimationSpeed(1f);
            chaser.SetSpeed(enemy.chaserGroup.chaseSpeed);
        });
    }

    private void RestartAfterCutscene()
    {
        if (!respawning && !missionCompleting)
        {
            respawning = true;
            StartCoroutine(RestartAfterCutsceneCoroutine());
        }
    }

    private IEnumerator RestartAfterCutsceneCoroutine()
    {
        UIManager.Instance.FadeOut();
        yield return new WaitForSeconds(UIManager.Instance.fadeSpeed);
        GameManager.Instance.GetPlayerTransform().position = playerRespawnPosition;
        GameManager.Instance.GetPlayerTransform().rotation = Quaternion.Euler(playerRespawnRotation);
        GameManager.Instance.GetMovementController().ResetVelocity();
        UIManager.Instance.staminaBar.ResetAwakeness();

        spawnedEnemies.ForEach(e =>
        {
            e.npcBark.StopCurrentBark();
        });

        // would be weird if it disappeared, but the first vase should still be destroyed at this point
        DestroyAllObjects(exceptFirstVase: true);

        SpawnRegularVases();
        SpawnEnemies();
        SpawnExtraObjects();

        if (missionCompleting)  // if we die while mission completing, then re-wait for this
        {
            RegionManager.Instance.finalHallwayDoor.OnPlayerPassThrough += CompleteMission;
        }
        missionCompleting = false;

        UIManager.Instance.FadeIn();
        yield return new WaitForSeconds(UIManager.Instance.fadeSpeed);

        respawning = false;
    }

    private SpawnedVase SpawnVase(Vector3 position, bool trackBrokenVase = true)
    {
        GameObject vaseInstance = Instantiate(vasePrefab, position, Quaternion.identity);
        GameObject vaseStandInstance = Instantiate(vaseStandPrefab, new Vector3(position.x - 0.8f, 0f, position.z - 1f), Quaternion.identity);

        if (trackBrokenVase)
        {
            BreakableObject breakableVase = Utils.GetRequiredComponentInChildren<BreakableObject>(vaseInstance);
            breakableVase.OnBreak += OnVaseBreak;
        }

        return new SpawnedVase(vaseInstance, vaseStandInstance);
    }

    private void OnVaseBreak(GameObject brokenInstance)
    {
        spawnedBrokenVases.Add(brokenInstance);
    }

    protected override void Cleanup()
    {
        // Here we have checks for all the instances specifically because this can be called on App shutdown
        //  this means its possible for some Singletons to have already been garbage collected by the time we get here
        Debug.Log("MissionTutorial Cleanup()!");

        if (GameManager.Instance)
        {
            // Resolves bug where this is called mid conversation with NPC whcihc makes player move again
            if(!DialogueManager.Instance.CheckIsAdvancing())
            {
                GameManager.Instance.GetPlayerController().EnablePlayerInput = true;
            }
            GameManager.Instance.GetPlayerRigidbody().isKinematic = false;
        }

        // Update GameEventManager
        if (GameEventManager.Instance)
        {
            GameEventManager.Instance.SetEventStatus(GameEventManager.GameEvent.TutorialActive, false);
        }

        if (DialogueManager.Instance)
        {
            DialogueManager.Instance.RemoveSpeaker(otherBedNPCSpeakerId);
        }

        if (MissionManager.Instance)
        {
            MissionManager.Instance.DestroyMissionObject(tutorialNurse);
            MissionManager.Instance.DestroyMissionObject(otherBedNPC);
        }

        if (RegionManager.Instance)
        {
            RegionManager.Instance.nurseRoomDoor.SetLocked(false);
            RegionManager.Instance.hallwayToHedgeMazeDoor1.SetLocked(false);
            RegionManager.Instance.hallwayToHedgeMazeDoor2.SetLocked(false);
        }

        // Handle the cutscene event handlers
        if (!startCutscenePlayed && RegionManager.Instance)
        {
            RegionManager.Instance.nurseRoomDoor.OnDoorClose -= OnNurseRoomDoorClose;
        }
        startCutscenePlayed = false;
        missionCompleting = false;

        if (UIManager.Instance)
        {
            UIManager.Instance.textOverlay.SetText(string.Empty);
            UIManager.Instance.InstantFadeIn();
        }

        // Destroy all spawned objects
        DestroyAllObjects(exceptPersistentObjects: true);
    }

    private void SpawnRegularVases()
    {
        vasePositions.ForEach(p =>
        {
            spawnedVases.Add(SpawnVase(p));
        });
    }

    private void SpawnExtraObjects()
    {
        extraObjects.ForEach(i =>
        {
            i.spawnedInstance = MissionManager.Instance.SpawnMissionObject(i);
        });
    }

    private void DestroyAllObjects(bool exceptFirstVase = false, bool exceptPersistentObjects = false)
    {
        if (!exceptFirstVase && firstVase != null)
        {
            if (firstVase.vaseObject)
            {
                Destroy(firstVase.vaseObject);
            }
            if (firstVase.vaseStand)
            {
                Destroy(firstVase.vaseStand);
            }
        }
        if (!exceptPersistentObjects)
        {
            DestroyFromList(spawnedBrokenVases); // we want to keep these on the floor!
            DestroyFromList(spawnedVases.Select(v => v.vaseStand).ToList()); // keep these toppled over!
            DestroyFromList(spawnedEnemies.Select(e => e.gameObject).ToList());
        }
        DestroyFromList(spawnedVases.Select(v => v.vaseObject).ToList());
        spawnedVases.Clear();
        spawnedEnemies.Clear();
        if (MissionManager.Instance)
        {
            MissionManager.Instance.DestroyMissionObjects(extraObjects);
        }
    }

    private void DestroyFromList(List<GameObject> gameObjects)
    {
        if (gameObjects != null && gameObjects.Count > 0)
        {
            gameObjects.ForEach(o =>
            {
                if (o)
                {
                    Destroy(o);
                }
            });
        }
    }
}
