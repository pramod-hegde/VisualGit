﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGit
{
    public sealed class GitPullArgs : GitTransportClientArgs
    {
        public GitPullArgs()
            : base(GitCommandType.Pull)
        {
            TagOption = GitPullTagOption.Unset;
            MergeStrategy = GitMergeStrategy.Unset;
        }

        public string Remote { get; set; }

        public string RemoteUri { get; set; }

        public bool CheckFetchedObjects { get; set; }

        public bool RemoveDeletedRefs { get; set; }

        public GitRef RemoteBranch { get; set; }

        public GitPullTagOption TagOption { get; set; }

        public GitMergeStrategy MergeStrategy { get; set; }
    }
}