﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MissionCheats: MonoBehaviour
{
    ////////////////////
    // TUTORIAL LEVEL
    ////////////////////
    [MenuItem(Constants.CHEATS_STARTMISSIONTUTORIAL, true)]
    public static bool ValidateStartMissionTutorial()
    {
        return Application.isPlaying && MissionManager.Instance && MissionManager.Instance.GetActiveMission<MissionTutorial>() == null;
    }

    [MenuItem(Constants.CHEATS_STARTMISSIONTUTORIAL)]
    public static void StartMissionTutorial()
    {
        MissionManager.Instance.StartMission(MissionsEnum.MissionTutorial);
    }

    [MenuItem(Constants.CHEATS_ENDMISSIONTUTORIAL, true)]
    public static bool ValidateStopMissionTutorial()
    {
        return Application.isPlaying && MissionManager.Instance && MissionManager.Instance.GetActiveMission<MissionTutorial>() != null;
    }

    [MenuItem(Constants.CHEATS_ENDMISSIONTUTORIAL)]
    public static void EndMissionTutorial()
    {
        MissionManager.Instance.EndMission(MissionsEnum.MissionTutorial);
    }

    ////////////////////
    // KITCHEN LEVEL
    ////////////////////
    [MenuItem(Constants.CHEATS_STARTMISSIONCAFETERIA1, true)]
    public static bool ValidateStartMissionCafeteria1()
    {
        return Application.isPlaying && MissionManager.Instance && MissionManager.Instance.GetActiveMission<MissionCafeteria1>() == null;
    }

    [MenuItem(Constants.CHEATS_STARTMISSIONCAFETERIA1)]
    public static void StartMissionCafeteria1()
    {
        MissionManager.Instance.StartMission(MissionsEnum.KitchenMission);
    }

    [MenuItem(Constants.CHEATS_ENDMISSIONCAFETERIA1, true)]
    public static bool ValidateStopMissionCafeteria1()
    {
        return Application.isPlaying && MissionManager.Instance && MissionManager.Instance.GetActiveMission<MissionCafeteria1>() != null;
    }

    [MenuItem(Constants.CHEATS_ENDMISSIONCAFETERIA1)]
    public static void EndMissionCafeteria1()
    {
        AMission mission = MissionManager.Instance.GetActiveMission<MissionCafeteria1>().mission;
        MissionManager.Instance.EndMission(MissionsEnum.KitchenMission);
    }
}
