using System;
using GoveKits.Runtime.Storage.Save;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [Serializable]
    private sealed class SavePayload
    {
        public int Counter;
        public string LastSaveTime;
    }

    private sealed class SavePayloadData : ISaveData<SavePayload>
    {
        public string RelativePath => "tests/save_test";

        public SavePayload State;

        public SavePayload Save() => State;

        public void Load(SavePayload state) => State = state;
    }

    private bool hasRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (FindAnyObjectByType<SaveTest>() != null)
        {
            return;
        }

        var go = new GameObject("[Auto] SaveTest");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveTest>();
    }

    private void Start()
    {
        if (hasRun)
        {
            return;
        }

        hasRun = true;
        RunSmokeTest();
    }

    private void RunSmokeTest()
    {
        SaveCore.CurrentFormat = SerializerType.Json;

        var saveData = new SavePayloadData
        {
            State = new SavePayload
            {
                Counter = UnityEngine.Random.Range(1, 10000),
                LastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            }
        };

        SaveCore.Save(saveData);

        var loadData = new SavePayloadData();
        SaveCore.Load(loadData);

        bool success = loadData.State != null
            && loadData.State.Counter == saveData.State.Counter
            && loadData.State.LastSaveTime == saveData.State.LastSaveTime;

        Debug.Log($"[SaveTest] Save/Load {(success ? "Success" : "Failed")} | Counter={loadData.State?.Counter} | Time={loadData.State?.LastSaveTime}");
    }
}
