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



using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ReSharper disable StringLiteralTypo

// ReSharper disable once CheckNamespace
namespace MarrowMachine.Tools
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once InconsistentNaming
    [InitializeOnLoad]
    public static class CIBootstrap
    {
        private static readonly string ScriptsFolder;
        private static readonly string PackageFolder;

        static CIBootstrap()
        {
            var packagesRoot = new DirectoryInfo(Application.dataPath).Parent?.FullName;
            PackageFolder = GetDirectory(packagesRoot, Path.Combine("Packages", PackageConfig.RootFolder));
            ScriptsFolder = GetDirectory(PackageFolder, "Scripts");

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            CIUtils.GetCustomEnvironments()?.Initialize();
            
#if !CICD_CONFIG && UNITY_EDITOR
            try
            {
                CreateConfigPackage();
                CIUtils.OverrideDefinesForAllTargets("CICD_CONFIG", CIUtils.DefaultMask);
                Debug.Log("CIBootstrap > Config package created successfully, reimporting");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Can't create config package!\n{exception.Message}");
            }
#elif CICD_CONFIG
            var currentVersion = VersionUtil.LoadVersionFromPlayerSettings();

            if (!currentVersion.Equals(AppVersion.Current))
            {
                VersionUtil.ApplyVersion(EditorUserBuildSettings.activeBuildTarget, AppVersion.Current);                
            }

            EnvironmentUtil.SelectEnvironment(AppEnvironment.Current);
#endif
        }

        // ReSharper disable once UnusedMember.Local
        private static void CreateConfigPackage()
        {
            Debug.LogWarning($"CIBoostrap > creating config package at {PackageFolder}");

            CreateFile(PackageFolder, PackageConfig.AssemblyFile, GetAssemblyFileContent());
            CreateFile(PackageFolder, PackageConfig.PackageFile, GetPackageJsonContent());
            CreateAppBuildConfigFile(VersionUtil.DetectCurrentVersion(), EnvironmentUtil.GetSelectedEnvironment());
        }

        private static void CreateAppBuildConfigFile(Version version, AppEnvironment environment)
        {
            CreateFile(ScriptsFolder, PackageConfig.AppBuildConfigFile, GetAppBuildConfigContent(version, environment));
        }

        private static string GetDirectory(string root, string directory)
        {
            var dir = Path.Combine(root, directory);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private static void CreateFile(string root, string filename, string content)
        {
            var filePath = Path.Combine(root, filename);
            File.WriteAllText(filePath, content);
        }

        public static void SaveVersion(Version version)
        {
            AppVersion.ResetVersion(version);
            CreateAppBuildConfigFile(version, AppEnvironment.Current);
        }

        public static void SaveEnvironment(AppEnvironment environment)
        {
            AppEnvironment.ResetEnvironment(environment);
            CreateAppBuildConfigFile(AppVersion.Current, environment);
        }

        private static string GetAssemblyFileContent()
        {
            return @"{
    ""name"": ""MarrowMachine.Tools.CICD-Config"",
    ""rootNamespace"": """",
    ""references"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""noEngineReferences"": false,
    ""versionDefines"": []
}";
        }

        private static string GetPackageJsonContent()
        {
            return $@"{{
    ""category"": ""Config"",
    ""displayName"": ""CI Tools Config"",
    ""name"": ""{PackageConfig.PackageId}"",
    ""unity"": ""2018.3"",
    ""version"": ""1.0.1"",
    ""keywords"": [
        ""tools"",
        ""unity"",
        ""cicd""
    ],
    ""description"": ""Configuration module for CI/CD tools"",
    ""type"": ""module""
}}";
        }

        private static string GetAppBuildConfigContent(Version version, AppEnvironment appEnvironment)
        {
            return $@"/*
 * This script is modified automatically
 * Do not edit
 */

// ReSharper disable once CheckNamespace
namespace MarrowMachine.Tools
{{
    // ReSharper disable once UnusedType.Global
    public static class AppBuildConfig
    {{
        public static readonly string Version = ""{version.FormatToString()}"";
        public static readonly string EnvironmentId = ""{appEnvironment.Id}"";
    }}
}}";
        }

        private struct PackageConfig
        {
            public const string RootFolder = "CICD-Config";
            public const string PackageId = "io.MarrowMachine.tools.cicd-config";
            public const string AssemblyFile = "MarrowMachine.Tools.CICD-Config.asmdef";
            public const string PackageFile = "package.json";
            public const string AppBuildConfigFile = "AppBuildConfig.cs";
        }
    }
}