﻿#if UNITY_EDITOR
using MonoHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MonoHook
{
    [BackgroundWindow(3)]
    [InitializeOnLoad]
    internal static class ConsoleWindowHook
    {
        public const string BackgroundPNGKey = "ConsoleWindowHookBackgroundPath";
        public const string BackgroundColorKey = "ConsoleWindowHookBackgroundColor";
        public const string OpenKey = "ConsoleWindowHookOpen";

        private static MethodHook _hook;

        static Type windowType = typeof(Editor).Assembly.GetType("UnityEditor.ConsoleWindow");
        static Texture2D backgroundTexture;
        static Texture2D BackgroundTexture
        {
            get
            {
                if(backgroundTexture == null)
                {
                    var path = SettingPrefs.GetString(BackgroundPNGKey, "Assets/background.png");
                    backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                return backgroundTexture;
            }
        }
        static Color color =default;


        static ConsoleWindowHook()
        {
            Init();
        }
        public static void Refresh()
        {
            backgroundTexture = null;
            ColorUtility.TryParseHtmlString(SettingPrefs.GetString(BackgroundColorKey, SettingEditorWindow.DefaultColor), out color);
            var isOpen = SettingPrefs.GetBool(OpenKey, false);
            if (isOpen == false)
            {
                if (_hook != null) _hook.Uninstall();
                _hook = null;
            }
            else
            {
                if (_hook == null) Init();
            }

        }
        static void Init()
        {
            var isOpen=SettingPrefs.GetBool(OpenKey, false);
            if (isOpen == false) return;
            if (_hook == null)
            {
                if (BackgroundTexture == null) return;//没图片
                ColorUtility.TryParseHtmlString(SettingPrefs.GetString(BackgroundColorKey, SettingEditorWindow.DefaultColor), out color);
                var srcMethod = windowType.GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);

                MethodInfo miReplacement = new Action<EditorWindow>(Replacement).Method;
                MethodInfo miProxy = new Action<EditorWindow>(Proxyment).Method;

                _hook = new MethodHook(srcMethod, miReplacement, miProxy);
                _hook.Install();
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void Replacement(EditorWindow window)
        {
            var originDepth = GUI.depth;
 
            Proxyment(window);
            if (BackgroundTexture != null)
            {
                //Debug.Log("深度" + originDepth);
                // 获取窗口的大小
                Rect windowRect = new Rect(0, 0, window.position.width, window.position.height);
                // 绘制背景图片
                GUI.depth = -10;
                GUI.DrawTexture(windowRect, BackgroundTexture, ScaleMode.ScaleAndCrop,true,0, color,0,0);
            }
            GUI.depth = originDepth;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void Proxyment(EditorWindow go)
        {
            // dummy code
            Debug.Log("something" + go.ToString());
        }
    }

}
#endif