// VisualGit.UI\PendingChanges\PendingCommitsPage.cs
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
using System.ComponentModel.Design;
using VisualGit.Scc;
using VisualGit.Commands;
using VisualGit.VS;
using VisualGit.UI.PendingChanges.Commits;
using VisualGit.Configuration;

namespace VisualGit.UI.PendingChanges
{
    partial class PendingCommitsPage : PendingChangesPage, ILastChangeInfo
    {
        public PendingCommitsPage()
        {
            InitializeComponent();
        }

        VisualGitConfig Config
        {
            get { return ConfigurationService.Instance; }
        }

        IVisualGitCommandService _commandService;
        IVisualGitCommandService CommandService
        {
            get { return _commandService ?? (_commandService = Context.GetService<IVisualGitCommandService>()); }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (pendingCommits != null)
            {
                pendingCommits.SelectionPublishServiceProvider = Context;
                pendingCommits.Context = Context;
                pendingCommits.HookCommands();
                pendingCommits.ColumnWidthChanged += new ColumnWidthChangedEventHandler(pendingCommits_ColumnWidthChanged);
                IDictionary<string, int> widths = ConfigurationService.GetColumnWidths(GetType());
                pendingCommits.SetColumnWidths(widths);
            }

            Context.GetService<IServiceContainer>().AddService(typeof(ILastChangeInfo), this);

            HookList();
        }

        protected void pendingCommits_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            IDictionary<string, int> widths = pendingCommits.GetColumnWidths();
            ConfigurationService.SaveColumnsWidths(GetType(), widths);
        }

        IPendingChangesManager _manager;
        private void HookList()
        {
            if (_manager != null || Context == null)
                return;

            if (pendingCommits.SmallImageList == null)
            {
                IFileIconMapper mapper = Context.GetService<IFileIconMapper>();

                pendingCommits.SmallImageList = mapper.ImageList;
            }

            _manager = Context.GetService<IPendingChangesManager>();

            if (_manager == null)
                return;

            _manager.Added += new EventHandler<PendingChangeEventArgs>(OnPendingChangeAdded);
            _manager.Removed += new EventHandler<PendingChangeEventArgs>(OnPendingChangeRemoved);
            _manager.Changed += new EventHandler<PendingChangeEventArgs>(OnPendingChangesChanged);
            _manager.InitialUpdate += new EventHandler<PendingChangeEventArgs>(OnPendingChangesInitialUpdate);
            _manager.IsActiveChanged += new EventHandler<PendingChangeEventArgs>(OnPendingChangesActiveChanged);
            _manager.ListFlushed += new EventHandler<PendingChangeEventArgs>(OnPendingChangesListFlushed);

            if (!_manager.IsActive)
            {
                _manager.IsActive = true;
                _manager.FullRefresh(false);
            }
            else
                PerformInitialUpdate(_manager);

            VisualGitServiceEvents ev = Context.GetService<VisualGitServiceEvents>();

            ev.SolutionClosed += new EventHandler(OnSolutionRefresh);
            ev.SolutionOpened += new EventHandler(OnSolutionRefresh);
            OnSolutionRefresh(this, EventArgs.Empty);
        }

        void OnSolutionRefresh(object sender, EventArgs e)
        {
        }

        protected IPendingChangesManager Manager
        {
            get
            {
                if (_manager == null)
                    HookList();

                return _manager;
            }
        }

        protected override Type PageType
        {
            get
            {
                return typeof(PendingCommitsPage);
            }
        }

        public bool LogMessageVisible
        {
            get { return !splitContainer.Panel1Collapsed; }
            set { splitContainer.Panel1Collapsed = !value; }
        }

        readonly Dictionary<string, PendingCommitItem> _listItems = new Dictionary<string, PendingCommitItem>(StringComparer.OrdinalIgnoreCase);

        void OnPendingChangeAdded(object sender, PendingChangeEventArgs e)
        {
            PendingCommitItem pci;

            string path = e.Change.FullPath;

            if (_listItems.TryGetValue(path, out pci))
            {
                // Should never happend; will refresh checkbox, etc.
                _listItems.Remove(path);
                pci.Remove();
            }

            pci = new PendingCommitItem(pendingCommits, e.Change);
            _listItems.Add(path, pci);
            pendingCommits.Items.Add(pci);

            // TODO: Maybe add something like
            //pendingCommits.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        HybridCollection<string> _checkedItems;
        void OnPendingChangesListFlushed(object sender, PendingChangeEventArgs e)
        {
            if (_listItems.Count > 0)
            {
                _checkedItems = new HybridCollection<string>(StringComparer.OrdinalIgnoreCase);
                foreach (PendingCommitItem pci in _listItems.Values)
                {
                    if (pci.Checked && !_checkedItems.Contains(pci.FullPath))
                        _checkedItems.Add(pci.FullPath);
                }
                _listItems.Clear();
                pendingCommits.ClearItems();
            }
        }

        void OnPendingChangesActiveChanged(object sender, PendingChangeEventArgs e)
        {
            // Just ignore for now
            Enabled = e.Manager.IsActive;
        }

        void OnPendingChangesInitialUpdate(object sender, PendingChangeEventArgs e)
        {
            PerformInitialUpdate(e.Manager);
        }

        void PerformInitialUpdate(IPendingChangesManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            pendingCommits.BeginUpdate();
            _listItems.Clear(); // Make sure we are clear
            pendingCommits.ClearItems();
            try
            {
                foreach (PendingChange pc in manager.GetAll())
                {
                    PendingCommitItem pi = new PendingCommitItem(pendingCommits, pc);
                    _listItems.Add(pc.FullPath, pi);

                    if (_checkedItems != null)
                        pi.Checked = _checkedItems.Contains(pc.FullPath);

                    pendingCommits.Items.Add(pi);
                }

                _checkedItems = null;
            }
            finally
            {
                pendingCommits.EndUpdate();
                pendingCommits.Invalidate();
            }
        }

        void OnPendingChangesChanged(object sender, PendingChangeEventArgs e)
        {
            PendingCommitItem pci;

            string path = e.Change.FullPath;

            if (!_listItems.TryGetValue(path, out pci))
            {
                pci = new PendingCommitItem(pendingCommits, e.Change);
                _listItems.Add(path, pci);
                pendingCommits.Items.Add(pci);
            }
            else
            {
                pci.RefreshText(Context);
            }
        }

        void OnPendingChangeRemoved(object sender, PendingChangeEventArgs e)
        {
            PendingCommitItem pci;

            string path = e.Change.FullPath;

            if (_listItems.TryGetValue(path, out pci))
            {
                _listItems.Remove(path);
                pci.Remove();
                pendingCommits.RefreshGroupsAvailable();
            }
        }

        public override bool CanRefreshList
        {
            get { return true; }
        }

        public override void RefreshList()
        {
            Context.GetService<IFileStatusCache>().ClearCache();

            IVisualGitOpenDocumentTracker dt = Context.GetService<IVisualGitOpenDocumentTracker>();

            if (dt != null)
                dt.RefreshDirtyState();

            Manager.FullRefresh(true);
        }

        private void pendingCommits_ResolveItem(object sender, PendingCommitsView.ResolveItemEventArgs e)
        {
            PendingChange pc = e.SelectionItem as PendingChange;

            PendingCommitItem pci;
            if (pc != null && this._listItems.TryGetValue(pc.FullPath, out pci))
            {
                e.Item = pci;
            }
        }

        private void pendingCommits_KeyUp(object sender, KeyEventArgs e)
        {
            // TODO: Replace with VS command handling, instead of hooking it with Winforms
            if (e.KeyCode == Keys.Enter)
            {
                // TODO: We should probably open just the focused file instead of the selection in the ItemOpenVisualStudio case to make it more deterministic what file is active after opening
                if (CommandService != null)
                    CommandService.ExecCommand(Config.PCDoubleClickShowsChanges
                        ? VisualGitCommand.ItemShowChanges : VisualGitCommand.ItemOpenVisualStudio, true);
            }
        }

        private void pendingCommits_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = pendingCommits.HitTest(e.X, e.Y);

            if (info == null || info.Location == ListViewHitTestLocations.None)
                return;

            if (info.Location == ListViewHitTestLocations.StateImage)
                return; // Just check the item

            if (CommandService != null)
                CommandService.ExecCommand(Config.PCDoubleClickShowsChanges
                    ? VisualGitCommand.ItemShowChanges : VisualGitCommand.ItemOpenVisualStudio, true);
        }
        internal void OnUpdate(VisualGit.Commands.CommandUpdateEventArgs e)
        {
            switch (e.Command)
            {
                case VisualGitCommand.PcLogEditorPasteFileList:
                    foreach (PendingCommitItem pci in _listItems.Values)
                    {
                        if (pci.Checked)
                            return;
                    }
                    e.Enabled = false;
                    return;
                case VisualGitCommand.PcLogEditorPasteRecentLog:
                    return;
            }
        }

        internal void OnExecute(VisualGit.Commands.CommandEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            switch (e.Command)
            {
                case VisualGitCommand.PcLogEditorPasteRecentLog:
                    break;
            }
            if (sb.Length > 0)
                logMessageEditor.PasteText(sb.ToString());
        }

        #region ILastChangeInfo Members

        void ILastChangeInfo.SetLastChange(string caption, string value)
        {
            if (string.IsNullOrEmpty(caption))
                lastRevBox.Enabled = lastRevBox.Visible = lastRevLabel.Enabled = lastRevLabel.Visible = false;
            else
            {
                lastRevLabel.Text = caption ?? "";
                lastRevBox.Text = value ?? "";

                lastRevBox.Enabled = lastRevBox.Visible = lastRevLabel.Enabled = lastRevLabel.Visible = true;
            }
        }

        #endregion

        public void DoCommit()
        {
            List<PendingChange> changes = new List<PendingChange>();

            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (pci.Checked)
                {
                    changes.Add(pci.PendingChange);
                }
            }

            IPendingChangeHandler pch = Context.GetService<IPendingChangeHandler>();

            PendingChangeCommitArgs a = new PendingChangeCommitArgs();
            a.LogMessage = logMessageEditor.Text;
            a.AmendLastCommit = amendBox.Checked;

            if (pch.Commit(changes, a))
            {
                logMessageEditor.Clear(true);
                amendBox.Checked = false;
            }
        }

        public void DoCreatePatch(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            PendingChangeCreatePatchArgs a = new PendingChangeCreatePatchArgs();
            a.FileName = fileName;

            IVisualGitSolutionSettings ss = Context.GetService<IVisualGitSolutionSettings>();
            a.RelativeToPath = ss.ProjectRoot;
            a.AddUnversionedFiles = true;

            List<PendingChange> changes = new List<PendingChange>();

            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (pci.Checked)
                {
                    changes.Add(pci.PendingChange);
                }
            }

            if (Context.GetService<IPendingChangeHandler>().CreatePatch(changes, a))
            {
            }
        }

        internal bool CanCommit()
        {
            if (_listItems.Count == 0)
                return false;

            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (!pci.Checked)
                    continue;

                return true;
            }

            return false;
        }

        internal bool CanCreatePatch()
        {
            if (!CanCommit())
                return false;

            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (!pci.Checked)
                    continue;
                PendingChange pc = pci.PendingChange;

                if (pc.GitItem.IsModified)
                    return true;
                else if (!pc.GitItem.IsVersioned && pc.GitItem.IsVersionable && pc.GitItem.InSolution)
                    return true; // Will be added                
            }

            return false;
        }

        internal bool CanApplyToWorkingCopy()
        {
            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (!pci.Checked)
                    continue;

                if (pci.PendingChange.CanApply)
                    return true;
            }

            return false;
        }

        internal void ApplyToWorkingCopy()
        {
            List<PendingChange> changes = new List<PendingChange>();

            foreach (PendingCommitItem pci in _listItems.Values)
            {
                if (!pci.Checked)
                    continue;

                changes.Add(pci.PendingChange);
            }

            PendingChangeApplyArgs args = new PendingChangeApplyArgs();

            if (Context.GetService<IPendingChangeHandler>().ApplyChanges(changes, args))
            {
            }
        }

        internal Microsoft.VisualStudio.TextManager.Interop.IVsTextView TextView
        {
            get
            {
                IVisualGitHasVsTextView tv = logMessageEditor.ActiveControl as IVisualGitHasVsTextView;

                if (tv != null)
                    return tv.TextView;

                return null;
            }
        }

        internal Microsoft.VisualStudio.TextManager.Interop.IVsFindTarget FindTarget
        {
            get
            {
                IVisualGitHasVsTextView tv = logMessageEditor.ActiveControl as IVisualGitHasVsTextView;

                if (tv != null)
                    return tv.FindTarget;

                return null;
            }
        }

        private void PendingCommitsPage_Load(object sender, EventArgs e)
        {
            CorrectLastRevisionMargins();
        }

        private void lastRevLabel_SizeChanged(object sender, EventArgs e)
        {
            CorrectLastRevisionMargins();
        }

        private void CorrectLastRevisionMargins()
        {
            int margin = (lastRevLabel.Height + lastRevLabel.Margin.Vertical) - lastRevBox.Height;
            int topMargin = margin / 2 + margin % 2;

            lastRevBox.Margin = new Padding(
                lastRevBox.Margin.Left,
                topMargin,
                lastRevBox.Margin.Right,
                Math.Max(margin - topMargin, 2)
            );
        }
    }
}
