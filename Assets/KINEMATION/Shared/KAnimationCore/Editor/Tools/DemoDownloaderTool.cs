// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Tools
{
    public struct ContentLicense
    {
        public string contentAuthor;
        public string contentName;
        public List<Tag> tags;
    }
    
    public struct Tag
    {
        public string text;
        public string tooltip;
        public string url;

        public Tag(string text, string tooltip = "", string url = "")
        {
            this.text = text;
            this.tooltip = tooltip;
            this.url = url;
        }
    }
    
    public static class ContentLicenseWidget
    {
        private static GUIStyle _labelStyle;
        private static GUIStyle _chipStyle;

        private static void InitStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false
            };

            _chipStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                stretchWidth = false
            };
        }

        public static void DrawRow(ContentLicense contentLicense)
        {
            InitStyles();

            // Begin padded horizontal row вЂ” respects the vertical group padding!
            EditorGUILayout.BeginHorizontal();

            string rowString = $"{contentLicense.contentAuthor} вЂў {contentLicense.contentName}";

            // Draw label (auto expands)
            GUILayout.Label(new GUIContent(rowString, contentLicense.contentName), _labelStyle);

            // Draw chips (right side but attached to label, no snapping)
            if (contentLicense.tags != null)
            {
                Color originalColor = GUI.backgroundColor;
                Color chipColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 0.89f, 0.4f)
                    : new Color(1f, 0.86f, 0.34f);

                GUI.backgroundColor = chipColor;

                foreach (var chip in contentLicense.tags)
                {
                    var chipContent = new GUIContent(chip.text, chip.tooltip);

                    if (GUILayout.Button(chipContent, _chipStyle))
                    {
                        if (!string.IsNullOrEmpty(chip.url)) Application.OpenURL(chip.url);
                    }
                }

                GUI.backgroundColor = originalColor;
            }
        
            EditorGUILayout.EndHorizontal();
        }
    }
    
    public abstract class DemoDownloaderTool : IEditorTool
    {
        private static readonly string PackageDirectory = Path.Combine("Library", "KINEMATION");

        private WebClient _webClient;
        private float _downloadProgress = 0f;
        private string _progressLabel = "";
        private bool _isDownloading = false;
        private bool _cancelledDownload = false;
    
        private string _fullPackagePath;
        private List<ContentLicense> _contentLicences;

        protected virtual string GetPackageUrl()
        {
            return string.Empty;
        }
    
        protected virtual string GetPackageFileName()
        {
            return string.Empty;
        }

        protected virtual List<ContentLicense> GetContentLicenses()
        {
            return null;
        }
        
        public virtual void Init()
        {
            _contentLicences = GetContentLicenses();
        }

        public virtual void Render()
        {
            if (_contentLicences != null)
            {
                EditorGUILayout.HelpBox("This demo has third-party content:", MessageType.Warning);
                foreach (var contentLicense in _contentLicences) ContentLicenseWidget.DrawRow(contentLicense);
            }
            
            GUI.enabled = !_isDownloading;
            if (GUILayout.Button("Download and Import"))
            {
                StartDownload();
            }
            GUI.enabled = true;

            if (!_isDownloading) return;
            
            EditorGUILayout.Space(1f);
            
            Rect rect = EditorGUILayout.GetControlRect(false);
            Rect barRect = rect;
            barRect.width *= 0.7f;

            float padding = 5f;

            Rect buttonRect = rect;
            buttonRect.width *= 0.3f;
            buttonRect.width -= padding;
            buttonRect.x += barRect.width + padding;
            
            EditorGUI.ProgressBar(barRect, _downloadProgress, _progressLabel);
            if (GUI.Button(buttonRect, "Stop", EditorStyles.miniButton))
            {
                _webClient.CancelAsync();
                _cancelledDownload = true;
            }
            
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public virtual string GetToolCategory()
        {
            return "Demo Content/";
        }

        public virtual string GetToolName()
        {
            return "Demo Downloader";
        }

        public virtual string GetDocsURL()
        {
            return string.Empty;
        }

        public virtual string GetToolDescription()
        {
            return "Download demo projects for KINEMATION assets with this tool.";
        }

        private void StartDownload()
        {
            string fullDirPath = Path.Combine(Application.dataPath, "..", PackageDirectory);
            _fullPackagePath = Path.Combine(fullDirPath, GetPackageFileName() + ".unitypackage");

            if (!Directory.Exists(fullDirPath)) Directory.CreateDirectory(fullDirPath);

            _webClient = new WebClient();
            _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
            _webClient.DownloadFileCompleted += OnDownloadFileCompleted;

            _isDownloading = true;
            _cancelledDownload = false;
            _downloadProgress = 0f;
            _progressLabel = "Starting download...";
            
            try
            {
                _webClient.DownloadFileAsync(new Uri(GetPackageUrl()), _fullPackagePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Download error: " + ex.Message);
                _isDownloading = false;
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _downloadProgress = e.ProgressPercentage / 100f;
            _progressLabel = $"Downloaded {e.BytesReceived / 1024} KB of {e.TotalBytesToReceive / 1024} KB";
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _isDownloading = false;
            _downloadProgress = 1f;
            _progressLabel = "Download complete.";

            if (!_cancelledDownload)
            {
                if (e.Error != null)
                {
                    Debug.LogError("Download error: " + e.Error.Message);
                }
                else if (File.Exists(_fullPackagePath))
                {
                    AssetDatabase.ImportPackage(_fullPackagePath, true);
                }
                else
                {
                    Debug.LogError("Package file not found after download.");
                }
            }
            
            _webClient.Dispose();
            _webClient = null;
        }
    }
}