using System;
using UnityEngine;

namespace Inkshift
{
    public sealed class Campaign : ScriptableObject
    {
        [SerializeField] string menuScene;
        [SerializeField] StageInfo[] stages;

        public string MenuScene => menuScene;
        public int Count => stages.Length;
        public StageInfo this[int index] => stages[index];

        public int NextUncleared
        {
            get
            {
                for (int i = 0; i < Count; i++)
                    if (BestTime(i) == 0f)
                        return i;
                return 0;
            }
        }

        public int ClearedCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Count; i++)
                    if (BestTime(i) > 0f)
                        total++;
                return total;
            }
        }

        public bool IsUnlocked(int index) => index == 0 || BestTime(index - 1) > 0f;
        public float BestTime(int index) => PlayerPrefs.GetFloat(TimeKey(index));
        string TimeKey(int index) => $"inkshift.best.{stages[index].Id}";

        public bool RecordClear(int index, float seconds)
        {
            float previous = BestTime(index);
            if (previous > 0f && previous <= seconds)
                return false;

            PlayerPrefs.SetFloat(TimeKey(index), Mathf.Max(0.01f, seconds));
            PlayerPrefs.Save();
            return true;
        }
    }

    [Serializable]
    public sealed class StageInfo
    {
        public string Id;
        public string Scene;
        public string Title;
        public string Focus;
        [TextArea] public string Brief;
    }
}
