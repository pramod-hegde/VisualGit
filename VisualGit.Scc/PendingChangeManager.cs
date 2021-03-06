// VisualGit.Scc\PendingChangeManager.cs
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
using VisualGit.Commands;
using VisualGit.Selection;
using VisualGit.Scc.StatusCache;

namespace VisualGit.Scc
{
    /// <summary>
    /// 
    /// </summary>
    [GlobalService(typeof(IPendingChangesManager))]
    partial class PendingChangeManager : VisualGitService, IPendingChangesManager
    {
        bool _isActive;
        bool _solutionOpen;
        public PendingChangeManager(IVisualGitServiceProvider context)
            : base(context)
        {

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            IsActive = true;

            VisualGitServiceEvents events = GetService<VisualGitServiceEvents>();

            events.SolutionOpened += new EventHandler(OnSolutionOpened);
            events.SolutionClosed += new EventHandler(OnSolutionClosed);

            _solutionOpen = !string.IsNullOrEmpty(GetService<ISelectionContext>().SolutionFilename);
        }

        void OnSolutionOpened(object sender, EventArgs e)
        {
            _solutionOpen = true;
            ScheduleRefresh();
        }

        void OnSolutionClosed(object sender, EventArgs e)
        {
            _solutionOpen = false;
        }

        #region IPendingChangesManager Members

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (value != _isActive)
                {
                    _isActive = value;

                    if (Change != null)
                    {
                        if (value)
                            Change.GitItemsChanged += new EventHandler<GitItemsEventArgs>(OnGitItemsChanged);
                        else
                            Change.GitItemsChanged -= new EventHandler<GitItemsEventArgs>(OnGitItemsChanged);
                    }

                    OnIsActiveChanged(new PendingChangeEventArgs(this, null));
                }
            }
        }

        IGitItemChange _change;
        IGitItemChange Change
        {
            get { return _change ?? (_change = GetService<IGitItemChange>()); }
        }

        readonly HybridCollection<string> _toRefresh = new HybridCollection<string>(StringComparer.OrdinalIgnoreCase);
        readonly List<string> _toMonitor = new List<string>();
        bool _fullRefresh;
        internal void OnTickRefresh()
        {
            List<string> toRefresh;
            bool fullRefresh = true;
            lock (_toRefresh)
            {
                _refreshScheduled = false;

                if (_fullRefresh)
                {
                    fullRefresh = true;
                    toRefresh = null;
                }
                else
                {
                    fullRefresh = false;
                    toRefresh = new List<string>(_toRefresh);
                }
                _toRefresh.Clear();
                _fullRefresh = false;

                _extraFiles.UniqueAddRange(_toMonitor);
                _toMonitor.Clear();
            }

            using (new FileStatusRefreshHint(true))
            {
                if (fullRefresh)
                {
                    InnerRefresh();
                }
                else
                {
                    foreach (string path in toRefresh)
                    {
                        ItemRefresh(path);
                    }
                }
            }
        }

        void OnGitItemsChanged(object sender, GitItemsEventArgs e)
        {
            lock (_toRefresh)
            {
                if (_fullRefresh || !_solutionOpen)
                    return;

                foreach (GitItem item in e.ChangedItems)
                {
                    if (!_toRefresh.Contains(item.FullPath))
                        _toRefresh.Add(item.FullPath);
                }

                ScheduleRefresh();
            }
        }

        bool _refreshScheduled;
        void ScheduleRefresh()
        {
            lock (_toRefresh)
            {
                ScheduleRefreshPreLocked();
            }
        }

        IVisualGitCommandService _commandService;
        IVisualGitCommandService CommandService
        {
            get { return _commandService ?? (_commandService = GetService<IVisualGitCommandService>()); }
        }

        void ScheduleRefreshPreLocked()
        {
            if (!_solutionOpen)
                return;

            if (!_refreshScheduled)
                CommandService.PostTickCommand(ref _refreshScheduled, VisualGitCommand.TickRefreshPendingTasks);
        }

        public void FullRefresh(bool clearStateCache)
        {
            if (clearStateCache && Cache != null)
                Cache.ClearCache();

            lock (_toRefresh)
            {
                _pendingChanges.Clear();
            }

            PendingChangeEventArgs ee = new PendingChangeEventArgs(this, null);
            OnListFlushed(ee);

            lock (_toRefresh)
            {
                _fullRefresh = true;
                _toRefresh.Clear();

                ScheduleRefreshPreLocked();
            }
        }

        public void Clear()
        {
            PendingChangeEventArgs ee = new PendingChangeEventArgs(this, null);
            OnListFlushed(ee);

            lock (_toRefresh)
            {
                _toRefresh.Clear();
                _fullRefresh = false;
                _pendingChanges.Clear();
                _extraFiles.Clear();
            }
        }

        public void Refresh(string path)
        {
            if (path != null && string.IsNullOrEmpty(path)) // path == ""
                throw new ArgumentNullException("path");

            if (!_isActive || !_solutionOpen)
                return;

            lock (_toRefresh)
            {
                if (path == null)
                    _fullRefresh = true;
                else if (!_fullRefresh && !_toRefresh.Contains(path))
                    _toRefresh.Add(path);

                ScheduleRefreshPreLocked();
            }
        }

        public void Refresh(IEnumerable<string> paths)
        {
            if (paths == null)
                throw new ArgumentNullException("paths");

            if (!_isActive || !_solutionOpen)
                return;

            lock (_toRefresh)
            {
                if (!_fullRefresh)
                {
                    foreach (string path in paths)
                    {
                        if (!string.IsNullOrEmpty(path) && !_toRefresh.Contains(path))
                            _toRefresh.Add(path);
                    }
                }

                ScheduleRefreshPreLocked();
            }
        }

        /// <summary>
        /// Gets the <see cref="VisualGit.Scc.PendingChange"/> with the specified full path.
        /// </summary>
        /// <value></value>
        public PendingChange this[string fullPath]
        {
            get
            {
                PendingChange pc;

                if (_pendingChanges.TryGetValue(fullPath, out pc))
                {
                    return pc;
                }
                return null;
            }
        }

        /// <summary>
        /// Raised when a pending change item has been added
        /// </summary>
        /// <remarks>Handlers should also hook the <see cref="FullRefresh"/> event</remarks>
        public event EventHandler<PendingChangeEventArgs> Added;

        /// <summary>
        /// Raises the <see cref="E:Added"/> event.
        /// </summary>
        /// <param name="e">The <see cref="VisualGit.Scc.PendingChangeEventArgs"/> instance containing the event data.</param>
        void OnAdded(PendingChangeEventArgs e)
        {
            if (Added != null)
                Added(this, e);
        }

        /// <summary>
        /// Raised when a pending change item has been removed
        /// </summary>
        /// <remarks>Handlers should also hook the <see cref="FullRefresh"/> event</remarks>
        public event EventHandler<PendingChangeEventArgs> Removed;

        /// <summary>
        /// Raises the <see cref="E:Removed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="VisualGit.Scc.PendingChangeEventArgs"/> instance containing the event data.</param>
        void OnRemoved(PendingChangeEventArgs e)
        {
            if (Removed != null)
                Removed(this, e);
        }

        /// <summary>
        /// Raised when the properties of a pending change have changed
        /// </summary>
        /// <remarks>Handlers should also hook the <see cref="FullRefresh"/> event</remarks>
        public event EventHandler<PendingChangeEventArgs> Changed;

        /// <summary>
        /// Raises the <see cref="E:Changed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="VisualGit.Scc.PendingChangeEventArgs"/> instance containing the event data.</param>
        void OnChanged(PendingChangeEventArgs e)
        {
            if (Changed != null)
                Changed(this, e);
        }

        /// <summary>
        /// Raised when the complete pending change state has been flushed; All listeners should
        /// use GetAll() to get a new initial state
        /// </summary>
        /// <remarks>Handlers should also hook the <see cref="FullRefresh"/> event</remarks>
        public event EventHandler<PendingChangeEventArgs> ListFlushed;

        /// <summary>
        /// Raises the <see cref="E:ListFlushed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="VisualGit.Scc.PendingChangeEventArgs"/> instance containing the event data.</param>
        void OnListFlushed(PendingChangeEventArgs e)
        {
            if (ListFlushed != null)
                ListFlushed(this, e);

        }

        /// <summary>
        /// Raised when the pending changes manager is activated or disabled
        /// </summary>
        public event EventHandler<PendingChangeEventArgs> IsActiveChanged;

        /// <summary>
        /// Raises the <see cref="E:IsActiveChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="VisualGit.Scc.PendingChangeEventArgs"/> instance containing the event data.</param>
        private void OnIsActiveChanged(PendingChangeEventArgs e)
        {
            if (IsActiveChanged != null)
                IsActiveChanged(this, e);
        }

        #endregion

        internal void ScheduleMonitor(string path)
        {
            lock (_toRefresh)
            {
                _toMonitor.Add(path);

                if (!_fullRefresh && !_toRefresh.Contains(path))
                    _toRefresh.Add(path);

                if (!_isActive || !_solutionOpen)
                    return;

                ScheduleRefreshPreLocked();
            }
        }

        internal void ScheduleMonitor(IEnumerable<string> paths)
        {
            lock (_toRefresh)
            {
                _toMonitor.AddRange(paths);

                if (!_isActive || !_solutionOpen)
                    return;

                _toRefresh.UniqueAddRange(paths);

                ScheduleRefreshPreLocked();
            }
        }

        internal void StopMonitor(string path)
        {
            lock (_toRefresh)
            {
                _extraFiles.Remove(path);

                if (!_isActive || !_solutionOpen)
                    return;

                if (!_fullRefresh && !_toRefresh.Contains(path))
                    _toRefresh.Add(path);

                ScheduleRefreshPreLocked();
            }
        }
    }
}
