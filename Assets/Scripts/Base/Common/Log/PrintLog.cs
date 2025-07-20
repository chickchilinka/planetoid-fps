using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Base.Common.Log
{
    public static class PrintLog
    {
        private static readonly StringBuilder StringBuilder = new StringBuilder();
        
        public static void Info(params string[] messages)
        {
            Debug.Log(PrepareLog(messages));
        }
        
        public static void Error(params string[] messages)
        {
            Debug.LogError(PrepareLog(messages));
        }
        
        public static void Warn(params string[] messages)
        {
            Debug.LogWarning(PrepareLog(messages));
        }

        private static string PrepareLog(params string[] messages)
        {
            StringBuilder.Clear();
            StringBuilder.AppendFormat("[{0}]", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            
            foreach (var message in messages)
                StringBuilder.AppendLine(message);

            return StringBuilder.ToString();
        }
    }
}