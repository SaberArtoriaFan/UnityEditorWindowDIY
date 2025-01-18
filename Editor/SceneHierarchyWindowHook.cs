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
    [InitializeOnLoad]
    internal class SceneHierarchyWindowHook
    {
        public const string BackgroundPNGKey = "SceneHierarchyWindowHookBackgroundPath";
        public const string BackgroundColorKey = "SceneHierarchyWindowHookBackgroundColor";
        public const string OpenKey = "SceneHierarchyWindowHookOpen";

        private static MethodHook _hook;

        static Type windowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static Texture2D backgroundTexture;
        static Texture2D BackgroundTexture
        {
            get
            {
                if(backgroundTexture == null)
                {
                    var path = EditorPrefs.GetString(BackgroundPNGKey, "Assets/background.png");
                    backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                return backgroundTexture;
            }
        }
        static Color color =default;


        static SceneHierarchyWindowHook()
        {
            Init();
        }
        public static void Refresh()
        {
            backgroundTexture = null;
            ColorUtility.TryParseHtmlString(EditorPrefs.GetString(BackgroundColorKey, "#FFFFFF4B"), out color);
            var isOpen = EditorPrefs.GetBool(OpenKey, false);
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
            var isOpen=EditorPrefs.GetBool(OpenKey, false);
            if (isOpen == false) return;
            if (_hook == null)
            {
                if (BackgroundTexture == null) return;//没图片
                ColorUtility.TryParseHtmlString(EditorPrefs.GetString(BackgroundColorKey, "#FFFFFF4B"), out color);
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

    internal class SceneHierarchyWindowHookSettingPanel:EditorWindow
    {
        [MenuItem("Window/编辑器DIY/SceneHierarchy背景图设置")]
        static void Open()
        {
            EditorWindow.GetWindow<SceneHierarchyWindowHookSettingPanel>().Show();
        }
        Texture2D texture2D=null;
        string texturePath;
        Color color = default;
        bool isOpen = false;
        private void OnEnable()
        {
            minSize = new Vector2(750, 500);
            maxSize = new Vector2(750, 500);

            texturePath = EditorPrefs.GetString(SceneHierarchyWindowHook.BackgroundPNGKey, "");
            if (string.IsNullOrEmpty(texturePath) == false)
                texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var colorStr= EditorPrefs.GetString(SceneHierarchyWindowHook.BackgroundColorKey, "#FFFFFF4B");
            ColorUtility.TryParseHtmlString(colorStr, out color);
            isOpen = EditorPrefs.GetBool(SceneHierarchyWindowHook.OpenKey, false);

        }
        private void OnGUI()
        {
            bool isChanged = false;
            GUILayout.BeginHorizontal();
            var width = position.width;
            GUILayout.BeginVertical(GUILayout.Width(width/2-10));

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片路径:", GUILayout.Width(60));  // 设置标签宽度
            var tp = EditorGUILayout.TextField(texturePath);
            // 处理拖放操作
            Rect dropArea = GUILayoutUtility.GetLastRect();
            var dragPath = HandleDragAndDrop(dropArea);
            if (string.IsNullOrEmpty(dragPath) == false) tp = dragPath;
            EditorGUILayout.EndHorizontal();
            if (tp != texturePath&& File.Exists(tp))
            {
               var pic = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                if (pic != null)
                {
                    texture2D = pic;
                    texturePath = tp;
                    EditorPrefs.SetString(SceneHierarchyWindowHook.BackgroundPNGKey, tp);
                    isChanged = true;
                }
            }
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片颜色:", GUILayout.Width(60));  // 设置标签宽度
            var tempColor = EditorGUILayout.ColorField(color);
            if(tempColor != color)
            {
                color = tempColor;
                EditorPrefs.SetString(SceneHierarchyWindowHook.BackgroundColorKey, $"#{ColorUtility.ToHtmlStringRGBA(color)}");
                isChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("是否启用:", GUILayout.Width(60));  // 设置标签宽度
            var open = EditorGUILayout.Toggle(isOpen);
            if (open != isOpen)
            {
                isOpen = open;
                EditorPrefs.SetBool(SceneHierarchyWindowHook.OpenKey, isOpen);
                isChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if(texture2D!=null)
                GUI.DrawTexture(new Rect(width / 2, 0, width / 2, position.height), texture2D, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
            GUILayout.EndHorizontal();
            if (isChanged)
                SceneHierarchyWindowHook.Refresh();
        }
        private string HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            string result = "";
            // 当拖拽图片到输入框区域时
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
                {
                    // 检查拖拽的内容是否是文件
                    if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Texture2D)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (currentEvent.type == EventType.DragPerform)
                        {
                            // 获取拖拽的图片路径
                            Texture2D draggedTexture = (Texture2D)DragAndDrop.objectReferences[0];
                            string path = AssetDatabase.GetAssetPath(draggedTexture);
                            result = path;
                            DragAndDrop.AcceptDrag();
                            Repaint();
                        }
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }

                    currentEvent.Use();
                }
            }
            return result;
        }
        }
}
#endif