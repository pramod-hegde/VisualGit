using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using VisualGit.UI.RepositoryExplorer;
using VisualGit.Scc;
using SharpSvn;
using SharpGit;

namespace VisualGit.UI.Commands
{
    public partial class SwitchDialog : VSDialogForm
    {
        private GitBranchRef _repositoryBranch;

        public SwitchDialog()
        {
            InitializeComponent();
        }

        public bool Force
        {
            get { return forceBox.Checked; }
            set { forceBox.Checked = value; }
        }

        /// <summary>
        /// Gets or sets the local path.
        /// </summary>
        /// <value>The local path.</value>
        public string LocalPath
        {
            get { return pathBox.Text; }
            set { pathBox.Text = value; }
        }

        /// <summary>
        /// Gets or sets the switch to branch.
        /// </summary>
        /// <value>The switch to branch.</value>
        public GitBranchRef SwitchToBranch
        {
            get
            {
                return toBranchBox.SelectedItem as GitBranchRef;
            }
            set
            {
                toBranchBox.SelectedItem = value;
            }
        }

        private void toUrlBox_Validating(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            if (SwitchToBranch == null)
                errorProvider.SetError(toBranchBox, CommandStrings.SelectABranch);
            else if (SwitchToBranch == _repositoryBranch)
                errorProvider.SetError(toBranchBox, CommandStrings.RepositoryAlreadyAtThisBranch);
            else
            {
                errorProvider.SetError(toBranchBox, null);
                e.Cancel = false;
            }
        }

        private void SwitchDialog_Shown(object sender, EventArgs e)
        {
            _repositoryBranch = RepositoryUtil.GetCurrentBranch(LocalPath);

            using (var client = GetService<IGitClientPool>().GetNoUIClient())
            {
                var lba = new GitListBranchArgs();
                GitListBranchResult lbr;

                client.ListBranch(LocalPath, lba, out lbr);

                toBranchBox.BeginUpdate();
                toBranchBox.Items.Clear();


                foreach (var branch in lbr.Branches)
                {
                    toBranchBox.Items.Add(branch);
                }

                toBranchBox.EndUpdate();
            }

            if (SwitchToBranch == null)
                SwitchToBranch = _repositoryBranch;
        }
    }
}
