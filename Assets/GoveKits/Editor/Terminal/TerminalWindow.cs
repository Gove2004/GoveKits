using System;
using GoveKits.Utility;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor.Project
{
    /// <summary>
    /// IDE 风格终端窗口。
    /// </summary>
    public sealed class TerminalWindow : EditorWindow
    {
        private Vector2 outputScroll;
        private string commandInput = string.Empty;
        private string workingDirectory = string.Empty;
        private bool autoScroll = true;
        private int lastOutputLength;

        [MenuItem("GoveKits/Tools/Terminal")]
        public static void ShowWindow()
        {
            GetWindow<TerminalWindow>("T Terminal");
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Application.dataPath;
            }

            Terminal.OutputReceived += OnOutputReceived;
        }

        private void OnDisable()
        {
            Terminal.OutputReceived -= OnOutputReceived;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSessionInfo();
            DrawOutput();
            DrawInput();
            TryAutoScroll();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("T Terminal", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            autoScroll = GUILayout.Toggle(autoScroll, "AutoScroll", EditorStyles.toolbarButton);

            if (GUILayout.Button("Start", EditorStyles.toolbarButton))
            {
                Terminal.Start(customWorkingDirectory: workingDirectory);
            }

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton))
            {
                Terminal.Stop();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                Terminal.ClearOutput();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSessionInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Status", Terminal.IsRunning ? "Running" : "Stopped");
            EditorGUILayout.LabelField("Shell", string.IsNullOrEmpty(Terminal.ShellName) ? "<none>" : Terminal.ShellName);
            workingDirectory = EditorGUILayout.TextField("Working Dir", workingDirectory);
            EditorGUILayout.EndVertical();
        }

        private void DrawOutput()
        {
            string output = Terminal.GetOutputSnapshot();
            outputScroll = EditorGUILayout.BeginScrollView(outputScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            lastOutputLength = output.Length;
        }

        private void DrawInput()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("T_Command_Input");
            commandInput = EditorGUILayout.TextField(commandInput, GUILayout.ExpandWidth(true));

            bool sendClicked = GUILayout.Button("Send", GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            Event e = Event.current;
            bool enterPressed = e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "T_Command_Input";
            if (enterPressed)
            {
                e.Use();
            }

            if (sendClicked || enterPressed)
            {
                SendCommand();
                EditorGUI.FocusTextInControl("T_Command_Input");
            }
        }

        private void SendCommand()
        {
            if (string.IsNullOrWhiteSpace(commandInput))
            {
                return;
            }

            if (!Terminal.IsRunning)
            {
                Terminal.Start(customWorkingDirectory: workingDirectory);
            }

            Terminal.Send(commandInput);
            commandInput = string.Empty;
        }

        private void TryAutoScroll()
        {
            if (!autoScroll)
            {
                return;
            }

            string output = Terminal.GetOutputSnapshot();
            if (output.Length != lastOutputLength)
            {
                outputScroll.y = float.MaxValue;
                Repaint();
            }
        }

        private void OnOutputReceived(string _)
        {
            Repaint();
        }
    }
}
