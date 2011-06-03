using System;
using System.Collections.Generic;
using SharpGit;

namespace VisualGit.ExtensionPoints.IssueTracker
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CommitArgs
    {
        private readonly ICollection<Uri> _paths;
        private readonly GitRevision _revision;
        string _commitMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitArgs"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="revision">The revision.</param>
        /// <param name="commitMessage">The commit message.</param>
        protected CommitArgs(ICollection<Uri> paths, GitRevision revision, string commitMessage)
        {
            _paths = paths;
            _revision = revision;
            _commitMessage = commitMessage;
        }

        // ### BH: I think this should be a list of string, not Uri.
        /// <summary>
        /// Gets the paths.
        /// </summary>
        /// <value>The paths.</value>
        public ICollection<Uri> Paths
        {
            get { return _paths; }
        }

        /// <summary>
        /// Gets the revision.
        /// </summary>
        /// <value>The revision.</value>
        public GitRevision Revision
        {
            get { return _revision; }
        }

        /// <summary>
        /// Gets or sets the internal commit message.
        /// </summary>
        /// <value>The internal commit message.</value>
        protected string InternalCommitMessage
        {
            get { return _commitMessage; }
            set { _commitMessage = value; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class PreCommitArgs : CommitArgs
    {
        bool _cancel;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreCommitArgs"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="revision">The revision.</param>
        public PreCommitArgs(ICollection<Uri> paths, GitRevision revision)
            : this(paths, revision, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreCommitArgs"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="revision">The revision.</param>
        /// <param name="commitMessage">The commit message.</param>
        public PreCommitArgs(ICollection<Uri> paths, GitRevision revision, string commitMessage)
            : base(paths, revision, commitMessage)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PreCommitArgs"/> is cancel.
        /// </summary>
        /// <value><c>true</c> if cancel; otherwise, <c>false</c>.</value>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }

        /// <summary>
        /// Gets or sets the commit message.
        /// </summary>
        /// <value>The commit message.</value>
        public string CommitMessage
        {
            get { return InternalCommitMessage; }
            set { InternalCommitMessage = value; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class PostCommitArgs : CommitArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostCommitArgs"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="revision">The revision.</param>
        public PostCommitArgs(ICollection<Uri> paths, GitRevision revision)
            : this(paths, revision, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostCommitArgs"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="revision">The revision.</param>
        /// <param name="commitMessage">The commit message.</param>
        public PostCommitArgs(ICollection<Uri> paths, GitRevision revision, string commitMessage)
            : base(paths, revision, commitMessage)
        {
        }

        /// <summary>
        /// Gets the commit message.
        /// </summary>
        /// <value>The commit message.</value>
        public string CommitMessage
        {
            get { return InternalCommitMessage; }
        }
    }
}
