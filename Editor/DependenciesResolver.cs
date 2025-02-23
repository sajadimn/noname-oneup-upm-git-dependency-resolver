﻿/*
* Copyright (c) 2020 Marllon Vilano
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NoName.OneUp.PackageManagerUtilities.Editor
{
    [InitializeOnLoad]
    public static class DependenciesResolver
    {
        #region Confirmation Dialog Texts
        private const string DialogTitle = "Dependencies Resolver";

        private const string DialogMessage =
            "New Git Dependencies were detected in one or more Custom Packages." +
            "\n\n" +
            "The dependencies were added to './Packages/manifest.json'." +
            "\n\n" +
            "The Unity Package Manager will resolve them after Unity getting unfocused and focused again!";

        private const string DialogOkButton = "Ok";
        #endregion

        private const string CachedPackagesDirectory = "./Library/PackageCache";
        private const string LocalPackagesDirectory = "./Packages";

        static DependenciesResolver()
        {
            Resolve();

            // That is uses because InitializeOnLoad will not get invoked when there are code errors,
            // and that is exactly the main reason we want to resolve dependencies.
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                Resolve();
            }
        }

        [MenuItem("Window/Package Manager Utilities/Resolve Git Dependencies", false, 1502)]
        public static void Resolve()
        {
            if (TryGetUnresolvedGitDependencies(out var unresolvedGitDependencies))
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    if (PackageManagerManifestInfo.TryLoad(out PackageManagerManifestInfo manifestInfo))
                    {
                        var dependencies = manifestInfo.dependencies.ToList();

                        foreach (var item in unresolvedGitDependencies)
                        {
                            dependencies.Insert(0, new KeyValuePair<string, string>(item.Key, item.Value));
                        }

                        manifestInfo.dependencies = dependencies.Distinct().ToDictionary(pair => pair.Key, pair => pair.Value);
                        manifestInfo.Save();
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();

                    // Showing that dialog because Unity will not resolve the new entries before the Editor gets unfocused and focused again.
                    if (EditorUtility.DisplayDialog(DialogTitle, DialogMessage, DialogOkButton))
                    {
                        // Recompile the packages
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        private static bool TryGetUnresolvedGitDependencies(out KeyValuePair<string, string>[] result)
        {
            if (TryGetInstalledPackages(out var installedPackages)
                && TryGetGitDependencies(installedPackages, out var gitDependencies))
            {
                result = gitDependencies
                    .Where(x => !installedPackages.Exists(i => i.name.Equals(x.Key)))
                    .ToArray();

                return result?.Length > 0;
            }

            result = default;
            return false;
        }

        private static bool TryGetGitDependencies(List<PackageInfo> installedPackages, out KeyValuePair<string, string>[] result)
        {
            result = installedPackages
                .Where(x => !x.Equals(default) && x.gitDependencies?.Count > 0)
                .SelectMany(x => x.gitDependencies)
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .ToArray();

            return result?.Length > 0;
        }

        private static bool TryGetInstalledPackages(out List<PackageInfo> result)
        {
            result = Directory.GetDirectories(CachedPackagesDirectory)
                .Concat(Directory.GetDirectories(LocalPackagesDirectory))
                .Select(PackageInfo.GetPackageFromDirectory)
                .Where(x => !x.Equals(default))
                .ToList();

            return result?.Count > 0;
        }
    }
}