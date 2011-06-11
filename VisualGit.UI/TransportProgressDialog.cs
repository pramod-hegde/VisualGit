﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpGit;
using VisualGit.VS;

namespace VisualGit.UI
{
    public partial class TransportProgressDialog : ProgressDialogBase
    {
        GitTransportClientArgs _clientArgs;
        bool _canceling;
        static readonly object _syncLock = new object();
        static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        readonly Dictionary<string, string> _runCache = new Dictionary<string, string>();

        public TransportProgressDialog()
        {
            InitializeComponent();
        }

        public GitTransportClientArgs ClientArgs
        {
            get { return _clientArgs; }
            set { _clientArgs = value; }
        }

        public override IDisposable Bind(GitClient client)
        {
            if (_clientArgs == null)
                throw new InvalidOperationException();

            _clientArgs.Credentials += new EventHandler<GitCredentialsEventArgs>(_clientArgs_Credentials);
            _clientArgs.CredentialsSupported += new EventHandler<GitCredentialsEventArgs>(_clientArgs_CredentialsSupported);
            _clientArgs.Progress += new EventHandler<GitProgressEventArgs>(_clientArgs_Progress);

            return new UnbindDisposable(this);
        }

        private void Unbind()
        {
            _clientArgs.Credentials -= new EventHandler<GitCredentialsEventArgs>(_clientArgs_Credentials);
            _clientArgs.CredentialsSupported -= new EventHandler<GitCredentialsEventArgs>(_clientArgs_CredentialsSupported);
            _clientArgs.Progress -= new EventHandler<GitProgressEventArgs>(_clientArgs_Progress);

            // If everything went OK, the credentials were accepted and we
            // should re-use them for the next run.

            if (_clientArgs.LastException == null)
            {
                lock (_syncLock)
                {
                    foreach (var item in _runCache)
                    {
                        _cache[item.Key] = item.Value;
                    }
                }
            }
        }

        void _clientArgs_Progress(object sender, GitProgressEventArgs e)
        {
            e.Cancel = _canceling;

            BeginInvoke(
                new Action<string, int, int>(UpdateProgress),
                e.CurrentTask, e.TaskLength, e.TaskProgress
            );
        }

        private void UpdateProgress(string currentTask, int taskLength, int taskProgress)
        {
            if (IsDisposed)
                return;

            if (taskLength <= 0 || taskProgress < 0)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                if (progressBar.Style != ProgressBarStyle.Continuous)
                    progressBar.Style = ProgressBarStyle.Continuous;

                if (progressBar.Maximum != taskLength)
                    progressBar.Maximum = taskLength;

                progressBar.Value = Math.Min(taskProgress, taskLength);
            }

            progressLabel.Text = currentTask;
        }

        void _clientArgs_CredentialsSupported(object sender, GitCredentialsEventArgs e)
        {
            Invoke(
                new Action<GitCredentialsEventArgs, bool>(ProcessCredentials),
                e, true
            );
        }

        void _clientArgs_Credentials(object sender, GitCredentialsEventArgs e)
        {
            Invoke(
                new Action<GitCredentialsEventArgs, bool>(ProcessCredentials),
                e, false
            );
        }

        private void ProcessCredentials(GitCredentialsEventArgs e, bool checkSupports)
        {
            if (checkSupports)
            {
                foreach (var item in e.Items)
                {
                    switch (item.Type)
                    {
                        case GitCredentialsType.Informational:
                        case GitCredentialsType.Password:
                        case GitCredentialsType.String:
                        case GitCredentialsType.Username:
                        case GitCredentialsType.YesNo:
                            break;

                        default:
                            e.Cancel = true;
                            break;
                    }
                }
            }
            else
            {
                // First, see whether:
                // * We are processing a username/password combination;
                // * We can already process the information and yes/no types;
                // * All credentials are already present in cache.

                GitCredentialItem usernameItem = null;
                GitCredentialItem passwordItem = null;
                bool oneMissing = false;

                foreach (var item in e.Items)
                {
                    if (!ProcessCached(e.Uri, item))
                    {
                        oneMissing = true;

                        switch (item.Type)
                        {
                            case GitCredentialsType.Username:
                                usernameItem = item;
                                break;

                            case GitCredentialsType.Password:
                                passwordItem = item;
                                break;

                            case GitCredentialsType.Informational:
                                if (!ShowInformation(item))
                                    _canceling = true;
                                break;

                            case GitCredentialsType.YesNo:
                                if (!ShowYesNo(item))
                                    _canceling = true;
                                break;
                        }

                        if (_canceling)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                if (!oneMissing)
                    return;

                // When we have both a username and password, we display a
                // combined dialog for this one.

                bool hadUsernamePassword = usernameItem != null && passwordItem != null;

                if (hadUsernamePassword)
                {
                    using (UsernamePasswordCredentialsDialog dialog = new UsernamePasswordCredentialsDialog())
                    {
                        dialog.UsernameItem = usernameItem;
                        dialog.PasswordItem = passwordItem;

                        if (dialog.ShowDialog(Context, this) != DialogResult.OK)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                // Process the rest. These are processed in a generic dialog.
                // Only when the type is Username, will the dialog not be
                // displayed as a password dialog.

                foreach (var item in e.Items)
                {
                    switch (item.Type)
                    {
                        case GitCredentialsType.Informational:
                        case GitCredentialsType.YesNo:
                            // Already processed these above.
                            break;

                        case GitCredentialsType.Username:
                        case GitCredentialsType.Password:
                            if (!hadUsernamePassword)
                            {
                                if (!ShowGeneric(item))
                                    _canceling = true;
                            }
                            break;

                        default:
                            if (!ShowGeneric(item))
                                _canceling = true;
                            break;
                    }

                    if (_canceling)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                foreach (var item in e.Items)
                {
                    UpdateCache(e.Uri, item);
                }
            }
        }

        private bool ProcessCached(string uri, GitCredentialItem item)
        {
            lock (_syncLock)
            {
                switch (item.Type)
                {
                    case GitCredentialsType.Password:
                    case GitCredentialsType.String:
                    case GitCredentialsType.Username:
                        string result;

                        if (_runCache.TryGetValue(GetCacheKey(uri, item), out result))
                        {
                            item.Value = result;
                            return true;
                        }
                        else if (_cache.TryGetValue(GetCacheKey(uri, item), out result))
                        {
                            item.Value = result;
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private void UpdateCache(string uri, GitCredentialItem item)
        {
            _runCache[GetCacheKey(uri, item)] = item.Value;
        }

        private string GetCacheKey(string uri, GitCredentialItem item)
        {
            return (uri ?? "") + ":" + item.Type + ":" + (item.PromptText ?? "");
        }

        private bool ShowGeneric(GitCredentialItem item)
        {
            using (var dialog = new GenericCredentialsDialog())
            {
                dialog.Item = item;

                return dialog.ShowDialog(Context, this) == DialogResult.OK;
            }
        }

        private bool ShowYesNo(GitCredentialItem item)
        {
            var result = Context.GetService<IVisualGitDialogOwner>()
                .MessageBox.Show(item.PromptText,
                "Credentials", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        private bool ShowInformation(GitCredentialItem item)
        {
            Context.GetService<IVisualGitDialogOwner>()
                .MessageBox.Show(item.PromptText,
                "Credentials", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _canceling = true;
            base.OnClosing(e);
            e.Cancel = true;
        }

        private void CancelClick(object sender, System.EventArgs e)
        {
            _canceling = true;

            OnCancel(EventArgs.Empty);

            cancelButton.Text = "Cancelling...";
            cancelButton.Enabled = false;
        }

        private class UnbindDisposable : IDisposable
        {
            private bool _disposed;
            private TransportProgressDialog _dialog;

            public UnbindDisposable(TransportProgressDialog dialog)
            {
                _dialog = dialog;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _dialog.Unbind();

                    _disposed = true;
                }
            }
        }
    }
}