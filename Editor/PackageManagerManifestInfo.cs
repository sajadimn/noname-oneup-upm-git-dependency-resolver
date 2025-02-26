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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NoName.OneUp.PackageManagerUtilities.Editor
{
    [Serializable]
    public struct PackageManagerManifestInfo
    {
        private const string PackageManagerManifestFilePath = "./Packages/manifest.json";

        public Dictionary<string, string> dependencies;

        public void Save()
        {
            File.WriteAllText(PackageManagerManifestFilePath, Json.Serialize(this, true));
        }

        public static bool TryLoad(out PackageManagerManifestInfo manifestInfo)
        {
            if (!File.Exists(PackageManagerManifestFilePath))
            {
                manifestInfo = default;
                return false;
            }

            string json = File.ReadAllText(PackageManagerManifestFilePath);
            var deserializedObjet = Json.Deserialize(json) as Dictionary<string, object>;

            if (deserializedObjet != null && deserializedObjet.TryGetValue(nameof(dependencies), out object result))
            {
                var dict = (IDictionary)result;
                manifestInfo = new PackageManagerManifestInfo();
                manifestInfo.dependencies = new Dictionary<string, string>();

                foreach (var packageName in dict.Keys)
                {
                    manifestInfo.dependencies.Add(packageName as string, dict[packageName] as string);
                }

                return true;
            }

            manifestInfo = default;
            return false;
        }
    }
}
