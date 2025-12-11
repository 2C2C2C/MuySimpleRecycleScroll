using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
#endif

namespace RecycleScrollView
{
    /// <summary> 
    /// Helper class for logging messages in RecycleScrollView.
    /// Replace Debug.Log with custom logging methods.
    ///  </summary>
    public static class LogHelper
    {
        public static void Log(string msg, UnityObject context = null)
        {
            string formatedMsg = $"[RecycleScrollView] {msg} | Frame:{Time.frameCount}";
            Debug.Log(formatedMsg, context: context);
        }

        public static void LogError(string msg, UnityObject context = null)
        {
            string formatedMsg = $"[RecycleScrollView] {msg} | Frame:{Time.frameCount}";
            Debug.LogError(formatedMsg, context: context);
        }

        public static void LogWarning(string msg, UnityObject context = null)
        {
            string formatedMsg = $"[RecycleScrollView] {msg} | Frame:{Time.frameCount}";
            Debug.LogWarning(formatedMsg, context: context);
        }

#if UNITY_EDITOR

        // here are some methods only used in editor
        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool LocateTargetFile(int instanceID, int line)
        {
            const string SELF_FILE_NAME = "LogHelper.cs";
            UnityObject targetObj = UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
            string targetPath = AssetDatabase.GetAssetPath(targetObj);
            if (!targetPath.EndsWith(SELF_FILE_NAME))
            {
                // Target is this file itself, just open it directly
                return UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(targetPath, line, 0);
            }

            string stackTrace = GetStackTrace();
            string[] traceLines = stackTrace.Split('\n');
            bool result = false;
            for (int i = 0; i < traceLines.Length; i++)
            {
                TryGetFilePathFromStr(traceLines[i], out string filePath, out int lineNum);
                if (string.IsNullOrEmpty(filePath) || filePath.Contains(SELF_FILE_NAME))
                {
                    continue;
                }
                else
                {
                    filePath = filePath.Replace('/', '\\');
                    result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNum, 0);
                    break;
                }
            }
            return result;
        }

        private static string GetStackTrace()
        {
            Type consoleWindowType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            FieldInfo fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            object consoleWindowInstance = fieldInfo.GetValue(null);

            if (null != consoleWindowInstance)
            {
                if ((object)UnityEditor.EditorWindow.focusedWindow == consoleWindowInstance)
                {
                    fieldInfo = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
                    string activeText = fieldInfo.GetValue(consoleWindowInstance).ToString();
                    return activeText;
                }
            }
            return "";
        }

        public static bool TryGetFilePathFromStr(in string message, out string filePath, out int lineNum)
        {
            const string REGEX_MATCH_PAT_3 = @"\(at .*\.cs:[0-9]*\)";
            Match matche = Regex.Match(message, REGEX_MATCH_PAT_3, RegexOptions.IgnoreCase);
            bool result = matche.Success;
            if (result)
            {
                // HACK : get rid of "(at )"
                string tempStr = matche.Value.Substring(4, matche.Value.Length - 5);
                int splitIndex = tempStr.LastIndexOf(":");
                filePath = tempStr.Substring(0, splitIndex);
                lineNum = Convert.ToInt32(tempStr.Substring(splitIndex + 1));
                filePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + filePath;
            }
            else
            {
                filePath = null;
                lineNum = 0;
            }
            return result;
        }

#endif

    }
}