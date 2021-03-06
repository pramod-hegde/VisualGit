// SharpGit\GitInfoCommand.cs
//
// Copyright 2008-2011 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//
// Changes and additions made for VisualGit Copyright 2011 Pieter van Ginkel.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGit.Revwalk;
using NGit.Dircache;
using System.Diagnostics;
using System.IO;
using NGit;

namespace SharpGit
{
    internal class GitInfoCommand : GitCommand<GitInfoArgs>
    {
        public GitInfoCommand(GitClient client, GitClientArgs args)
            : base(client, args)
        {
        }

        public GitInfoEventArgs Execute(string fullPath)
        {
            Debug.Assert(Args.PrepareMerge, "Only function of GitInfo is to prepare for a merge");

            var repositoryEntry = Client.GetRepository(fullPath);

            using (repositoryEntry.Lock())
            {
                var repository = repositoryEntry.Repository;
                string relativePath = repository.GetRepositoryPath(fullPath);

                var dirCache = repository.ReadDirCache();
                var stages = new DirCacheEntry[4];

                for (int i = 0, count = dirCache.GetEntryCount(); i < count; i++)
                {
                    var entry = dirCache.GetEntry(i);

                    if (String.Equals(entry.PathString, relativePath, FileSystemUtil.StringComparison))
                    {
                        stages[entry.Stage] = entry;
                    }
                }

                var result = new GitInfoEventArgs();

                var reader = repository.NewObjectReader();

                try
                {
                    result.ConflictOld = StoreMergeFile(reader, fullPath, stages[1], "BASE");
                    result.ConflictWork = StoreMergeFile(reader, fullPath, stages[2], "LOCAL");
                    result.ConflictNew = StoreMergeFile(reader, fullPath, stages[3], "REMOTE");
                }
                finally
                {
                    reader.Release();
                }

                return result;
            }
        }

        private string StoreMergeFile(ObjectReader reader, string fullPath, DirCacheEntry entry, string type)
        {
            if (entry == null)
                return null;

            string targetFileName;
            int index = 0;

            do
            {
                targetFileName = fullPath + "." + type;

                if (index > 0)
                    targetFileName += "_" + index;

                index++;
            }
            while (File.Exists(targetFileName));

            using (var stream = File.Create(targetFileName))
            {
                var loader = reader.Open(entry.GetObjectId());

                using (var inStream = new ObjectStreamWrapper(loader.OpenStream()))
                {
                    inStream.CopyTo(stream);
                }
            }

            return targetFileName;
        }
    }
}
