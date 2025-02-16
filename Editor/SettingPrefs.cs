using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MonoHook
{
    internal static class SettingPrefs
    {
        [Serializable]
        class SettingInfo
        {
            public SettingItem[] arr=new SettingItem[0];
        }
        [Serializable]
        class SettingItem
        {
            public string key;
            public string value_Str;
            public int value_Int;
        }
        static Dictionary<string,string> strDict=null;
        static Dictionary<string,int> intDict = null;
        static bool isDirty = false;
        static string SettingSavePath = $"{Application.dataPath}/../ProjectSettings/EditorDIYSetting.json";
        static void Read()
        {
            if(File.Exists(SettingSavePath)==false)
            {
                SettingInfo tsi=new SettingInfo ();
                File.WriteAllText(SettingSavePath,JsonUtility.ToJson(tsi));
            }
            SettingInfo si= JsonUtility.FromJson< SettingInfo>(File.ReadAllText(SettingSavePath));
            strDict = new Dictionary<string, string>();
            intDict = new Dictionary<string, int>();
            foreach(var v in si.arr)
            {
                if(string.IsNullOrEmpty(v.value_Str))
                    intDict.Add(v.key, v.value_Int);
                else
                    strDict.Add(v.key,v.value_Str);
            }
        }
        static void Write()
        {
            CheckInit();
            List<SettingItem> list = new List<SettingItem>();
            foreach (var v in strDict)
                list.Add(new SettingItem() { key = v.Key, value_Str = v.Value });
            foreach(var v in intDict)
                list.Add(new SettingItem() { key=v.Key, value_Int = v.Value });
            SettingInfo si= new SettingInfo() { arr=list.ToArray()};
            File.WriteAllText(SettingSavePath, JsonUtility.ToJson(si));
        }
        static void CheckInit()
        {
            if (strDict == null || intDict == null)
                Read();
        }
        static void CheckDirty()
        {
            if (isDirty) Read();
        }
        public static string GetString(string key, string defaultValue)
        {
            CheckInit();
            CheckDirty();
            if(strDict.TryGetValue(key, out var value))return value;    
            return defaultValue;
        }
        public static void SetString(string key, string value)
        {
            CheckInit();
            if (strDict.ContainsKey(key)) strDict[key] = value;
            else strDict.Add(key, value);
            Write();
        }
        public static int GetInt(string key, int defaultValue)
        {
            CheckInit();
            CheckDirty();
            if (intDict.TryGetValue(key, out var value)) return value;
            return defaultValue;
        }
        public static void SetInt(string key, int value)
        {
            CheckInit();
            if (intDict.ContainsKey(key)) intDict[key] = value;
            else intDict.Add(key, value);
            Write();
        }
        public static bool GetBool(string key, bool defaultValue)
        {
            CheckInit();
            CheckDirty();
            if (intDict.TryGetValue(key, out var value)) return value==1;
            return defaultValue;
        }
        public static void SetBool(string key, bool value)
        {
            CheckInit();
            var intValue = value ? 1 : 0;
            if (intDict.ContainsKey(key)) intDict[key] = intValue;
            else intDict.Add(key, intValue);
            Write();
        }
        public static void RemoveAll()
        {
            CheckInit();
            intDict.Clear();
            strDict.Clear();
            Write();
        }
    }
}
