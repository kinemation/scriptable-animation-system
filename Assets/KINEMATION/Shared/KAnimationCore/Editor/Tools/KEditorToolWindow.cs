// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using KINEMATION.Shared.KAnimationCore.Editor.Widgets;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Tools
{
    public class KEditorToolWindow : EditorWindow
    {
        private static Vector2 MinSize = new Vector2(640f, 360f);

        private KSplitterWidget _splitterWidget;
        private List<IEditorTool> _tools;
        private readonly List<DisplayCategoryNode> _categories = new List<DisplayCategoryNode>();
        private IEditorTool _selectedTool;
        
        private const float RowHeight = 18f;
        private const float Indent = 16f;
        private const float HoverPad = 4f;

        private GUIStyle _toolWindowStyle;
        private GUIStyle _toolNameStyle;
        private GUIStyle _docsLinkStyle;

        [SerializeField] private string selectedToolTypeName;
        
        [MenuItem(KEditorUtility.EditorToolsPath)]
        public static void CreateWindow()
        {
            var window = GetWindow<KEditorToolWindow>("KINEMATION Tools");
            window.minSize = MinSize;
        }

        private void OnEnable()
        {
            _toolWindowStyle = new GUIStyle()
            {
                padding = new RectOffset(6, 0, 6, 6)
            };

            _tools = new List<IEditorTool>();
            var toolTypes = TypeCache.GetTypesDerivedFrom<IEditorTool>().ToArray();

            foreach (var toolType in toolTypes)
            {
                if (toolType.IsAbstract) continue;

                var toolInstance = Activator.CreateInstance(toolType) as IEditorTool;
                if (toolInstance == null) continue;
                
                toolInstance.Init();
                _tools.Add(toolInstance);
            }

            BuildCatalog();

            if (!string.IsNullOrEmpty(selectedToolTypeName))
            {
                foreach (var tool in _tools)
                {
                    if (!selectedToolTypeName.Equals(tool.GetType().Name)) continue;
                    _selectedTool = tool;
                }
            }

            _splitterWidget = new KSplitterWidget
            {
                onDrawFirstGUI = RenderToolsList,
                onDrawSecondGUI = RenderTool,
                orientation = SplitOrientation.Horizontal
            };
        }

        private void OnGUI()
        {
            EnsureDocsLinkStyle();
            _splitterWidget.OnGUI(position);
        }

        private void BuildCatalog()
        {
            _categories.Clear();

            foreach (var tool in _tools)
            {
                string[] categoryPath = SplitPath(tool.GetToolCategory());
                string categoryLabel = categoryPath.Length > 0 ? categoryPath[0] : "General";
                string[] groupPath = categoryPath.Length > 1 ? categoryPath.Skip(1).ToArray() : Array.Empty<string>();

                var categoryNode = _categories.FirstOrDefault(node => node.label == categoryLabel);
                if (categoryNode == null)
                {
                    categoryNode = new DisplayCategoryNode { label = categoryLabel };
                    _categories.Add(categoryNode);
                }

                if (groupPath.Length == 0)
                {
                    categoryNode.tools.Add(new DisplayToolNode { tool = tool });
                }
                else
                {
                    DisplayGroupNode currentGroup = null;

                    for (int index = 0; index < groupPath.Length; index++)
                    {
                        string segment = groupPath[index];

                        if (index == 0)
                        {
                            var nextGroup = categoryNode.groups.FirstOrDefault(group => group.label == segment);
                            if (nextGroup == null)
                            {
                                nextGroup = new DisplayGroupNode { label = segment };
                                categoryNode.groups.Add(nextGroup);
                            }
                            currentGroup = nextGroup;
                        }
                        else
                        {
                            var nextGroup = currentGroup.groups.FirstOrDefault(group => group.label == segment);
                            if (nextGroup == null)
                            {
                                nextGroup = new DisplayGroupNode { label = segment };
                                currentGroup.groups.Add(nextGroup);
                            }
                            currentGroup = nextGroup;
                        }
                    }

                    currentGroup.tools.Add(new DisplayToolNode { tool = tool });
                }
            }

            _categories.Sort((left, right) =>
                string.Compare(left.label, right.label, StringComparison.OrdinalIgnoreCase));

            foreach (var categoryNode in _categories)
            {
                SortGroupRecursive(categoryNode);
            }
        }

        private static void SortGroupRecursive(DisplayCategoryNode categoryNode)
        {
            categoryNode.groups.Sort((left, right) =>
                string.Compare(left.label, right.label, StringComparison.OrdinalIgnoreCase));
            categoryNode.tools.Sort((left, right) =>
                string.Compare(left.tool.GetToolName(), right.tool.GetToolName(), StringComparison.OrdinalIgnoreCase));

            foreach (var childGroup in categoryNode.groups)
            {
                SortGroupRecursive(childGroup);
            }
        }

        private static void SortGroupRecursive(DisplayGroupNode groupNode)
        {
            groupNode.groups.Sort((left, right) =>
                string.Compare(left.label, right.label, StringComparison.OrdinalIgnoreCase));
            groupNode.tools.Sort((left, right) =>
                string.Compare(left.tool.GetToolName(), right.tool.GetToolName(), StringComparison.OrdinalIgnoreCase));

            foreach (var childGroup in groupNode.groups)
            {
                SortGroupRecursive(childGroup);
            }
        }

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();
            return path
                .Split(new[] { '/', '\\', '>' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => !string.IsNullOrEmpty(segment))
                .ToArray();
        }

        private void RenderToolsList()
        {
            EditorGUILayout.BeginVertical(_toolWindowStyle);
            
            foreach (var categoryNode in _categories)
            {
                EditorGUILayout.LabelField(categoryNode.label, KEditorUtility.boldLabel);

                foreach (var groupNode in categoryNode.groups)
                {
                    DrawGroup(groupNode, 0);
                }

                foreach (var toolNode in categoryNode.tools)
                {
                    DrawTool(toolNode, 0);
                }
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGroup(DisplayGroupNode groupNode, int depth)
        {
            Rect rowRect = GUILayoutUtility.GetRect(1f, RowHeight, GUILayout.ExpandWidth(true));
            DrawHoverCard(rowRect, depth, false);

            Rect foldoutRect = rowRect;
            foldoutRect.xMin += depth * Indent + 6f;
            groupNode.expanded = EditorGUI.Foldout(foldoutRect, groupNode.expanded, GUIContent.none, true);

            Rect labelRect = rowRect;
            labelRect.xMin = foldoutRect.xMin + 12f;
            EditorGUI.LabelField(labelRect, groupNode.label);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rowRect.Contains(Event.current.mousePosition))
            {
                groupNode.expanded = !groupNode.expanded;
                Event.current.Use();
            }

            if (groupNode.expanded)
            {
                foreach (var childGroup in groupNode.groups) DrawGroup(childGroup, depth + 1);
                foreach (var toolNode in groupNode.tools) DrawTool(toolNode, depth + 1);
            }
        }

        private void DrawTool(DisplayToolNode toolNode, int depth)
        {
            Rect rowRect = GUILayoutUtility.GetRect(1f, RowHeight, GUILayout.ExpandWidth(true));
            DrawHoverCard(rowRect, depth, toolNode.tool == _selectedTool);

            Rect labelRect = rowRect;
            labelRect.xMin += depth * Indent + 18f;
            EditorGUI.LabelField(labelRect, toolNode.tool.GetToolName());

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rowRect.Contains(Event.current.mousePosition))
            {
                _selectedTool = toolNode.tool;
                selectedToolTypeName = _selectedTool.GetType().Name;
                Event.current.Use();
            }
        }

        private void DrawHoverCard(Rect rowRect, int depth, bool active)
        {
            bool hovered = rowRect.Contains(Event.current.mousePosition) || active;
            if (!hovered) return;

            Rect paddedRect = new Rect(
                rowRect.x + depth * Indent + HoverPad,
                rowRect.y,
                rowRect.width - depth * Indent - HoverPad * 2f,
                rowRect.height
            );

            GUI.Box(paddedRect, GUIContent.none, EditorStyles.helpBox);
        }

        private void RenderTool()
        {
            EditorGUILayout.BeginVertical(_toolWindowStyle);
            
            if (_selectedTool == null)
            {
                EditorGUILayout.HelpBox("Select a tool on the left to get started.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(_selectedTool.GetToolName(), _toolNameStyle);

                var content = EditorGUIUtility.TrTextContent("Documentation");
                var size = _docsLinkStyle.CalcSize(content);
                var rect = GUILayoutUtility.GetRect(size.x, size.y, _docsLinkStyle, 
                    GUILayout.ExpandWidth(false));

                if (GUI.Button(rect, "Documentation", _docsLinkStyle))
                {
                    string url = _selectedTool.GetDocsURL();
                    if(!string.IsNullOrEmpty(url)) Application.OpenURL(url);
                }

                EditorGUILayout.Space(2f);
                EditorGUILayout.HelpBox(_selectedTool.GetToolDescription(), MessageType.Info);
                EditorGUILayout.Space();
                
                _selectedTool.Render();
            }

            EditorGUILayout.EndVertical();
        }

        private void EnsureDocsLinkStyle()
        {
            if (_toolNameStyle == null)
            {
                _toolNameStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
            }
            
            if (_docsLinkStyle != null) return;
            
            bool isProSkin = EditorGUIUtility.isProSkin;
            string hexNormal = isProSkin ? "#60A5FA" : "#2563EB";
            string hexHover = isProSkin ? "#93C5FD" : "#1D4ED8";
            string hexActive = isProSkin ? "#3B82F6" : "#1E40AF";

            Color normal, hover, active;
            ColorUtility.TryParseHtmlString(hexNormal, out normal);
            ColorUtility.TryParseHtmlString(hexHover, out hover);
            ColorUtility.TryParseHtmlString(hexActive, out active);

            _docsLinkStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };

            // No backgrounds — render like a plain label
            _docsLinkStyle.normal.background = null;
            _docsLinkStyle.hover.background = null;
            _docsLinkStyle.active.background = null;
            _docsLinkStyle.focused.background = null;
            _docsLinkStyle.onNormal.background = null;
            _docsLinkStyle.onHover.background = null;
            _docsLinkStyle.onActive.background = null;
            _docsLinkStyle.onFocused.background = null;

            // Link colors
            _docsLinkStyle.normal.textColor = normal;
            _docsLinkStyle.hover.textColor = hover;
            _docsLinkStyle.active.textColor = active;
            _docsLinkStyle.focused.textColor = hover;
            _docsLinkStyle.onNormal.textColor = normal;
            _docsLinkStyle.onHover.textColor = hover;
            _docsLinkStyle.onActive.textColor = active;
            _docsLinkStyle.onFocused.textColor = hover;
        }

        private sealed class DisplayCategoryNode
        {
            public string label;
            public List<DisplayGroupNode> groups = new List<DisplayGroupNode>();
            public List<DisplayToolNode> tools = new List<DisplayToolNode>();
        }

        private sealed class DisplayGroupNode
        {
            public string label;
            public bool expanded;
            public List<DisplayGroupNode> groups = new List<DisplayGroupNode>();
            public List<DisplayToolNode> tools = new List<DisplayToolNode>();
        }

        private sealed class DisplayToolNode
        {
            public IEditorTool tool;
        }
    }
}