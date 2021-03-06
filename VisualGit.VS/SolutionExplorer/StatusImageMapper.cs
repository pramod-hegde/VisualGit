// VisualGit.VS\SolutionExplorer\StatusImageMapper.cs
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
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using VisualGit.Scc;
using System.IO;
using System.Diagnostics;
using SharpGit;

namespace VisualGit.VS.SolutionExplorer
{
    [GlobalService(typeof(IStatusImageMapper))]
    sealed class StatusImageMapper : VisualGitService, IStatusImageMapper
    {
        public StatusImageMapper(IVisualGitServiceProvider context)
            : base(context)
        {
        }

        ImageList _statusImageList;
        public ImageList StatusImageList
        {
            get { return _statusImageList ?? (_statusImageList = CreateStatusImageList()); }
        }

        public ImageList CreateStatusImageList()
        {
            using (Stream images = typeof(StatusImageMapper).Assembly.GetManifestResourceStream(typeof(StatusImageMapper).Namespace + ".StatusGlyphs.bmp"))
            {
                if (images == null)
                    return null;

                Bitmap bitmap = (Bitmap)Image.FromStream(images, true);

                ImageList imageList = new ImageList();
                imageList.ImageSize = new Size(8, bitmap.Height);
                bitmap.MakeTransparent(bitmap.GetPixel(0, 0));

                imageList.Images.AddStrip(bitmap);

                return imageList;
            }
        }

        public VisualGitGlyph GetStatusImageForGitItem(GitItem item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (item.IsConflicted || item.IsObstructed || item.IsTreeConflicted)
                return VisualGitGlyph.InConflict;
            else if (!item.IsVersioned)
            {
                if (!item.Exists)
                    return VisualGitGlyph.FileMissing;
                else if (item.IsIgnored)
                    return VisualGitGlyph.Ignored;
                else if (item.IsVersionable)
                    return item.InSolution ? VisualGitGlyph.ShouldBeAdded : VisualGitGlyph.Blank;
                else
                    return VisualGitGlyph.None;
            }
            
			switch (item.Status.State)
            {
                case GitStatus.Normal:
                    if (item.IsDocumentDirty)
                        return VisualGitGlyph.FileDirty;
                    else
                        return VisualGitGlyph.Normal;
                case GitStatus.Modified:
                    return VisualGitGlyph.Modified;
                case GitStatus.Added:
                    return item.Status.IsCopied ? VisualGitGlyph.CopiedOrMoved : VisualGitGlyph.Added;

                case GitStatus.Missing:
                    if (item.IsCasingConflicted)
                        return VisualGitGlyph.InConflict;
                    else
                        goto case GitStatus.Deleted;
                case GitStatus.Deleted:
                    return VisualGitGlyph.Deleted;

                case GitStatus.Conflicted: // Should have been handled above
                case GitStatus.Obstructed:
                    return VisualGitGlyph.InConflict;

                case GitStatus.Ignored: // Should have been handled above
                    return VisualGitGlyph.Ignored;

                case GitStatus.Incomplete:
                    return VisualGitGlyph.InConflict;

                case GitStatus.Zero:
                default:
                    return VisualGitGlyph.None;
            }
        }
    }
}
