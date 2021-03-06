// VisualGit.UI\PendingChanges\Commits\PendingCommitItem.cs
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
using System.Text;
using System.Windows.Forms;
using VisualGit.Scc;
using VisualGit.VS;
using VisualGit.UI.VSSelectionControls;
using SharpGit;

namespace VisualGit.UI.PendingChanges.Commits
{
    class PendingCommitItem : SmartListViewItem
    {
        readonly PendingChange _change;

        public PendingCommitItem(PendingCommitsView view, PendingChange change)
            : base(view)
        {
            if (change == null)
                throw new ArgumentNullException("change");

            _change = change;

            Checked = true;

            RefreshText(view.Context);
        }

        public void RefreshText(IVisualGitServiceProvider context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IFileStatusCache cache = context.GetService<IFileStatusCache>();

            ImageIndex = PendingChange.IconIndex;
            GitItem item = cache[FullPath];

            if (item == null)
                throw new InvalidOperationException(); // Item no longer valued

            PendingChangeStatus pcs = PendingChange.Change ?? new PendingChangeStatus(PendingChangeKind.None);

            SetValues(
                pcs.PendingCommitText,
                "", // Change list
                GetDirectory(item),
                PendingChange.FullPath,
                "", // Locked
                SafeDate(item.Modified), // Modified
                PendingChange.Name,
                PendingChange.RelativePath,
                PendingChange.Project,
                GetRevision(PendingChange),
                PendingChange.FileType,
                SafeWorkingCopy(item));

            if (!SystemInformation.HighContrast)
            {
                System.Drawing.Color clr = System.Drawing.Color.Black;

                if (item.IsConflicted || PendingChange.Kind == PendingChangeKind.WrongCasing)
                    clr = System.Drawing.Color.Red;
                else if (item.IsDeleteScheduled)
                    clr = System.Drawing.Color.DarkRed;
                else if (item.Status.IsCopied || item.Status.State == GitStatus.Added)
                    clr = System.Drawing.Color.FromArgb(100, 0, 100);
                else if (!item.IsVersioned)
                {
                    if (item.InSolution && !item.IsIgnored)
                        clr = System.Drawing.Color.FromArgb(100, 0, 100); // Same as added+copied
                    else
                        clr = System.Drawing.Color.Black;
                }
                else if (item.IsModified)
                    clr = System.Drawing.Color.DarkBlue;

                ForeColor = clr;
            }
        }

        private string GetRevision(PendingChange PendingChange)
        {
            if (PendingChange.Revision.HasValue)
                return PendingChange.Revision.ToString();
            else
                return "";
        }

        private string SafeDate(DateTime dateTime)
        {
            if (dateTime.Ticks == 0 || dateTime.Ticks == 1)
                return "";

            DateTime n = dateTime.ToLocalTime();

            if (n < DateTime.Now - new TimeSpan(24, 0, 0))
                return n.ToString("d");
            else
                return n.ToString("T");
        }

        private string GetDirectory(GitItem gitItem)
        {
            if (gitItem.IsDirectory)
                return gitItem.FullPath;
            else
                return gitItem.Directory;
        }

        static string SafeWorkingCopy(GitItem gitItem)
        {
            GitWorkingCopy wc = gitItem.WorkingCopy;
            if (wc == null)
                return "";

            return wc.FullPath;
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public PendingChange PendingChange
        {
            get { return _change; }
        }

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath
        {
            get { return _change.FullPath; }
        }
    }
}
