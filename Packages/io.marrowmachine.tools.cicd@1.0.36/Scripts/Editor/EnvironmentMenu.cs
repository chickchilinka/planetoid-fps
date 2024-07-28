// MarrowMachine CONFIDENTIAL
// __________________
// 
/*
    MarrowMachine CONFIDENTIAL
    __________________

    [2016] - [2022] MarrowMachine LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of MarrowMachine LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to MarrowMachine LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from MarrowMachine LLC.
*/

using UnityEditor;

// ReSharper disable once CheckNamespace
namespace MarrowMachine.Tools
{
    [InitializeOnLoad]
    public static class EnvironmentMenu
    {
        static EnvironmentMenu()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            var selectedId = AppEnvironment.Current.Id;
            var env = AppEnvironment.FromId(selectedId);
            UpdateCheckmarks(env);
        }

        private static void UpdateCheckmarks(AppEnvironment selected)
        {
            var allEnvs = AppEnvironment.GetAllEnvironments();
            foreach (var environment in allEnvs)
                Menu.SetChecked($"Custom/Environment/{environment.Id}", environment == selected);
        }

        private static void SelectEnvironment(AppEnvironment env)
        {
            EnvironmentUtil.SelectEnvironment(env);
            UpdateCheckmarks(env);
        }

        [MenuItem("Custom/Environment/Apply Current Environment", false, 1)]
        private static void ApplyCurrentEnvironment()
        {
            EnvironmentUtil.SelectEnvironment(EnvironmentUtil.GetSelectedEnvironment(), true);
        }

        [MenuItem("Custom/Environment/Get Selected Environment", false, 1)]
        private static void GetSelectedEnvironment()
        {
            EditorUtility.DisplayDialog("Environment", EnvironmentUtil.GetSelectedEnvironment().ToString(), "OK");
        }

        [MenuItem("Custom/Environment/PROD")]
        private static void SelectProduction()
        {
            SelectEnvironment(AppEnvironment.Prod);
        }

        [MenuItem("Custom/Environment/TEST")]
        private static void SelectTest()
        {
            SelectEnvironment(AppEnvironment.Test);
        }

        [MenuItem("Custom/Environment/DEV")]
        private static void SelectDevelopment()
        {
            SelectEnvironment(AppEnvironment.Dev);
        }
    }
}