﻿//
// AssemblyReference.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Projects
{
	public sealed class AssemblyReference
	{
		public AssemblyReference (FilePath path, string aliases = null)
		{
			FilePath = path;
			Aliases = aliases ?? "";
		}

		public FilePath FilePath { get; private set; }
		public string Aliases { get; private set; }

		public override bool Equals (object obj)
		{
			var ar = obj as AssemblyReference;
			return ar != null && ar.FilePath == FilePath && ar.Aliases == Aliases;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return FilePath.GetHashCode () ^ Aliases.GetHashCode ();
			}
		}

		/// <summary>
		/// Returns an enumerable collection of aliases. 
		/// </summary>
		public IEnumerable<string> EnumerateAliases ()
		{
			return Aliases.Split (',', ';').Where (a => !string.IsNullOrEmpty (a));
		}
	}
}