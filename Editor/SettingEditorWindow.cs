using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MonoHook
{
    [AttributeUsage(AttributeTargets.Class )]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class BackgroundWindowAttribute:Attribute
    {

    }
    internal class SettingEditorWindow :EditorWindow
    {
        [MenuItem("Window/编辑器DIY/Setting")]
        static void Open()
        {
            EditorWindow.GetWindow<SettingEditorWindow>().Show();
        }
        Texture2D texture2D = null;
        string texturePath;
        Color color = default;
        bool isOpen = false;
        private int selectedTabIndex = 0; // 当前选中的页签索引
        private string[] tabNames ; // 页签名称

        private void OnEnable()
        {
            // 获取当前程序集中所有被 BackgroundWindowAttribute 特性修饰的类
            var classesWithAttribute = Assembly.GetExecutingAssembly()
                .GetTypes()  // 获取所有类型
                .Where(t => t.GetCustomAttribute<BackgroundWindowAttribute>() != null)  // 查找有特性的类
                .ToList();
            tabNames = classesWithAttribute.Select(t => t.Name).ToArray();
            selectedTabIndex = 0;
            InitItem();
        }
        void InitItem()
        {
            string curr = tabNames[selectedTabIndex];
            texturePath = SettingPrefs.GetString($"{curr}BackgroundPath", "");
            if (string.IsNullOrEmpty(texturePath) == false)
                texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var colorStr = SettingPrefs.GetString($"{curr}BackgroundColor", "#FFFFFF4B");
            ColorUtility.TryParseHtmlString(colorStr, out color);
            isOpen = SettingPrefs.GetBool($"{curr}Open", false);
        }
        private void OnGUI()
        {   // 使用 EditorGUILayout.Toolbar 创建页签按钮
            var index = GUILayout.Toolbar(selectedTabIndex, tabNames,  EditorStyles.toolbarButton);
            if (index != selectedTabIndex)
            {
                selectedTabIndex = index;
                InitItem();
            }


            bool isChanged = false;
            GUILayout.BeginHorizontal();
            var width = position.width;
            GUILayout.BeginVertical(GUILayout.Width(width / 2 - 10));

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片路径:", GUILayout.Width(60));  // 设置标签宽度
            var tp = EditorGUILayout.TextField(texturePath);
            // 处理拖放操作
            Rect dropArea = GUILayoutUtility.GetLastRect();
            var dragPath = HandleDragAndDrop(dropArea);
            if (string.IsNullOrEmpty(dragPath) == false) tp = dragPath;
            EditorGUILayout.EndHorizontal();
            if (tp != texturePath && File.Exists(tp))
            {
                var pic = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                if (pic != null)
                {
                    texture2D = pic;
                    texturePath = tp;
                    SettingPrefs.SetString(ConsoleWindowHook.BackgroundPNGKey, tp);
                    isChanged = true;
                }
            }
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片颜色:", GUILayout.Width(60));  // 设置标签宽度
            var tempColor = EditorGUILayout.ColorField(color);
            if (tempColor != color)
            {
                color = tempColor;
                SettingPrefs.SetString(ConsoleWindowHook.BackgroundColorKey, $"#{ColorUtility.ToHtmlStringRGBA(color)}");
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
                SettingPrefs.SetBool(ConsoleWindowHook.OpenKey, isOpen);
                isChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (texture2D != null)
                GUI.DrawTexture(new Rect(width / 2, 0, width / 2, position.height), texture2D, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
            GUILayout.EndHorizontal();
            if (isChanged)
                ConsoleWindowHook.Refresh();
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
