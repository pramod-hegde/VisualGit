// VisualGit.Services\VS\IVisualGitSolutionSettings.cs
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

namespace VisualGit.VS
{
    /// <summary>
    /// 
    /// </summary>
    public interface IVisualGitSolutionSettings
    {
        /// <summary>
        /// Gets or sets the full path of the solution root including a final '\'
        /// </summary>
        /// <remarks>The project root is stored as a relative path from the solution file</remarks>
        string ProjectRootWithSeparator { get; }

        /// <summary>
        /// Gets or sets the project root. (As a normalized path)
        /// </summary>
        /// <value>The project root.</value>
        string ProjectRoot { get; set; }

        /// <summary>
        /// Gets the solution filename.
        /// </summary>
        /// <value>The solution filename.</value>
        string SolutionFilename { get; }

        /// <summary>
        /// Default path where VS stores its new projects
        /// </summary>
        string NewProjectLocation { get; }


        Version VisualStudioVersion { get; }

        /// <summary>
        /// Gets the project root SVN item.
        /// </summary>
        /// <value>The project root SVN item.</value>
        GitItem ProjectRootGitItem { get; }

        /// <summary>
        /// Gets all project extensions filter.
        /// </summary>
        /// <value>All project extensions filter.</value>
        string AllProjectExtensionsFilter { get; }

        /// <summary>
        /// Gets the name of the open project filter.
        /// </summary>
        /// <value>The name of the open project filter.</value>
        string OpenProjectFilterName { get; }


        /// <summary>
        /// Gets the open file filter.
        /// </summary>
        /// <value>The open file filter.</value>
        string OpenFileFilter { get; }

        /// <summary>
        /// Gets a value indicating whether [in ranu mode].
        /// </summary>
        /// <value><c>true</c> if [in ranu mode]; otherwise, <c>false</c>.</value>
        bool InRanuMode { get; }
        /// <summary>
        /// Gets the visual studio registry root.
        /// </summary>
        /// <value>The visual studio registry root.</value>
        string VisualStudioRegistryRoot { get; }
        /// <summary>
        /// Gets the visual studio user registry root.
        /// </summary>
        /// <value>The visual studio user registry root.</value>
        string VisualStudioUserRegistryRoot { get; }
        /// <summary>
        /// Gets the registry hive suffix.
        /// </summary>
        /// <value>The registry hive suffix.</value>
        string RegistryHiveSuffix { get; }


        /// <summary>
        /// Gets the solution filter.
        /// </summary>
        /// <value>The solution filter.</value>
        string SolutionFilter { get; }

        /// <summary>
        /// Tries to open the specified project or solution file.
        /// </summary>
        /// <param name="projectFile">The project file.</param>
        void OpenProjectFile(string projectFile);
    }
}
