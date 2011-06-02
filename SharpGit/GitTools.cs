﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpGit
{
    /// <summary>
    /// Rewrite of SvnTools to remove dependency on SharpSvn. See
    /// http://sharpsvn.open.collab.net/source/browse/sharpsvn/trunk/src/SharpSvn/SvnTools.cpp
    /// for original version.
    /// </summary>
    public static class GitTools
    {
        private const int MAX_PATH = 260;
        private static char[] _invalidChars;

        public static string GetNormalizedFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (PathContainsInvalidChars(path))
                throw new ArgumentException("Path contains invalid characters", "path");
            else if (IsNormalizedFullPath(path))
                return path;


            bool retry = true;

            if (path.Length < MAX_PATH)
            {
                try
                {
                    path = Path.GetFullPath(path);
                    retry = false;
                }
                catch (PathTooLongException) // Path grew by getting full path
                {
                    // Use the retry
                }
            }

            if (retry)
            {
                path = LongGetFullPath(path);

                if (GetPathRootPart(path) == null)
                    throw new PathTooLongException("Paths with a length above MAX_PATH must be rooted");
            }

            if (path.Length >= 2 && path[1] == ':')
            {
                char c = path[0];

                if ((c >= 'a') && (c <= 'z'))
                    path = char.ToUpperInvariant(c) + path.Substring(1);

                string r = path.TrimEnd('\\');

                if (r.Length > 3)
                    return r;
                else
                    return path.Substring(0, 3);
            }
            else if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
            {
                string root = GetPathRootPart(path);

                if (root != null && !path.StartsWith(root, StringComparison.Ordinal))
                    path = root + path.Substring(root.Length).TrimEnd('\\');
            }
            else
                path = path.TrimEnd('\\');

            return path;
        }

        public static bool PathContainsInvalidChars(string path)
        {
            char[] invalidChars = _invalidChars;

            if (invalidChars == null)
                _invalidChars = invalidChars = Path.GetInvalidPathChars();

            if (0 <= path.IndexOfAny(invalidChars))
                return true;

            return false;
        }

        public static bool IsNormalizedFullPath(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            int c = path.Length;

            if (path.Length < 3)
                return false;

            int i, n;
            if (IsDirSeparator(path, 0))
            {
                if (!IsDirSeparator(path, 1))
                    return false;

                for (i = 2; i < path.Length; i++)
                {
                    // Check hostname rules
                    if (!Char.IsLetterOrDigit(path, i) && 0 > "._-".IndexOf(path[i]))
                        break;
                }

                if (i == 2 || !IsDirSeparator(path, i))
                    return false;

                i++;

                n = i;

                for (; i < path.Length; i++)
                {
                    // Check share name rules
                    if (!Char.IsLetterOrDigit(path, i) && 0 > "._-".IndexOf(path[i]))
                        break;
                }

                if (i == n)
                    return false; // "\\server\"
                else if (i == c)
                    return true; // "\\server\path"
                else if (c > i && !IsDirSeparator(path, i))
                    return false;
                else if (c == i + 1)
                    return false; // "\\server\path\"

                i++;
            }
            else if ((path[1] != ':') || !IsDirSeparator(path, 2))
                return false;
            else if (!((path[0] >= 'A') && (path[0] <= 'Z')))
                return false;
            else
                i = 3;

            while (i < c)
            {
                if (i >= c && IsDirSeparator(path, i))
                    return false; // '\'-s behind each other

                if (i < c && path[i] == '.')
                {
                    int j = i;

                    while (j < c && path[j] == '.')
                        j++;

                    if (IsSeparator(path, j) || j >= c)
                        return false; // Relative path
                }

                n = i;

                while (i < c && !IsInvalid(path, i) && !IsDirSeparator(path, i) && path[i] != Path.AltDirectorySeparatorChar)
                    i++;

                if (n == i)
                    return false;
                else if (i == c)
                    return true;
                else if (!IsDirSeparator(path, i++))
                    return false;

                if (i == c)
                    return false; // We don't like paths with a '\' at the end
            }

            return true;
        }

        private static bool IsDirSeparator(string v, int index)
        {
            if (index < 0 || (index >= v.Length))
                return false;

            return (v[index] == Path.DirectorySeparatorChar);
        }

        private static bool IsSeparator(string v, int index)
        {
            if (index < 0 || (index >= v.Length))
                return false;

            char c = v[index];

            return (c == Path.DirectorySeparatorChar) || (c == Path.AltDirectorySeparatorChar);
        }


        private static bool IsInvalid(string v, int index)
        {
            if (index < 0 || (index >= v.Length))
                return false;

            char[] invalidChars = _invalidChars;

            if (invalidChars == null)
                _invalidChars = invalidChars = Path.GetInvalidPathChars();

            return 0 <= Array.IndexOf(invalidChars, v[index]);
        }

        public static string LongGetFullPath(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // Subversion does not have a problem with paths over MAX_PATH, as long as they are absolute
            if (!path.StartsWith("\\\\?\\"))
                path = "\\\\?\\" + path;

            var rPath = new StringBuilder();

            uint sz = (uint)path.Length * sizeof(char);
            uint c = GetFullPathName(path, sz, rPath, null);

            if (c == 0 || c >= sz)
                throw new PathTooLongException("GetFullPath for long paths failed");

            path = rPath.ToString();

            if (path.StartsWith("\\\\?\\"))
                path = path.Substring(4);

            return path;
        }

        private static string GetPathRootPart(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            if (path.Length >= 3 && path[1] == ':' && path[2] == '\\')
            {
                if (path[0] >= 'a' && path[0] <= 'z')
                    return Char.ToUpper(path[0]) + ":\\";
                else
                    return path.Substring(0, 3);
            }

            if (!path.StartsWith("\\\\"))
                return null;

            int nSlash = path.IndexOf('\\', 2);

            if (nSlash <= 2)
                return null; // No hostname

            int nEnd = path.IndexOf('\\', nSlash + 1);

            if (nEnd < 0)
                nEnd = path.Length;

            return "\\\\" + path.Substring(2, nSlash - 2).ToLowerInvariant() + path.Substring(nSlash, nEnd - nSlash);
        }

        public static string GetNormalizedDirectoryName(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            path = GetNormalizedFullPath(path);

            string root = GetPathRootPart(path);

            int nLs = path.LastIndexOf(Path.DirectorySeparatorChar);

            if (nLs > root.Length)
                return path.Substring(0, nLs);
            else if (nLs == root.Length || (nLs < root.Length && path.Length > root.Length))
                return root;
            else
                return null;
        }

        [DllImport("kernel32.dll")]
        static extern uint GetFullPathName(string lpFileName, uint nBufferLength, [Out] StringBuilder lpBuffer, [Out] StringBuilder lpFilePart);
    }
}