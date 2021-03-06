// VisualGit.UI\WorkingCopyExplorer\Nodes\WCTreeNode.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VisualGit.UI.WorkingCopyExplorer.Nodes
{
    abstract class WCTreeNode
    {
        readonly IVisualGitServiceProvider _context;
        WCTreeNode _parent;

        public WCTreeNode(IVisualGitServiceProvider context, WCTreeNode parent)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            _context = context;
            _parent = parent;
        }

        FileSystemTreeNode _treeNode;
        public FileSystemTreeNode TreeNode
        {
            get
            {
                return _treeNode;
            }
            internal set { _treeNode = value; }
        }

        public virtual bool IsContainer
        {
            get { return true; }
        }

        protected IVisualGitServiceProvider Context
        {
            get { return _context; }
        }



        public abstract string Title
        {
            get;
        }

        public abstract IEnumerable<WCTreeNode> GetChildren();

        /// <summary>
        /// Gets the index of the image.
        /// </summary>
        /// <value>The index of the image.</value>
        public abstract int ImageIndex
        {
            get;
        }

        /// <summary>
        /// The parent node of this node.
        /// </summary>
        public WCTreeNode Parent
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return _parent; }
        }

        /// <summary>
        /// Derived classes implement this method to append their resources
        /// to the list.
        /// </summary>
        /// <param name="list"></param>
        public abstract void GetResources(Collection<GitItem> list, bool getChildItems,
            Predicate<GitItem> filter);

        public void Refresh()
        {
            RefreshCore(true);
        }

        public void Refresh(bool rescan)
        {
            RefreshCore(rescan);
        }

        protected abstract void RefreshCore(bool rescan);

        internal abstract bool ContainsDescendant(string path);
    }
}
