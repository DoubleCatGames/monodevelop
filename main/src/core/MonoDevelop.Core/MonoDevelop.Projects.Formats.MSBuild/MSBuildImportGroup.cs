﻿//
// MSBuildImportGroup.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildImportGroup: MSBuildElement
	{
		List<MSBuildImport> imports = new List<MSBuildImport> ();

		internal override void ReadChildElement (MSBuildXmlReader reader)
		{
			if (reader.LocalName == "Import") {
				var item = new MSBuildImport ();
				item.ParentObject = this;
				item.Read (reader);
				imports.Add (item);
			} else
				base.ReadChildElement (reader);
		}

		internal override string GetElementName ()
		{
			return "ImportGroup";
		}

		internal override IEnumerable<MSBuildObject> GetChildren ()
		{
			return imports;
		}

		public bool IsImported {
			get;
			set;
		}

		public MSBuildImport AddNewImport (string name, string condition = null, MSBuildImport beforeImport = null)
		{
			var import = new MSBuildImport ();
			import.Project = name;
			import.Condition = condition;

			int insertIndex = -1;
			if (beforeImport != null)
				insertIndex = imports.IndexOf (beforeImport);

			if (insertIndex != -1)
				imports.Insert (insertIndex, import);
			else
				imports.Add (import);

			import.ResetIndent (false);
			NotifyChanged ();
			return import;
		}

		public void RemoveImport (MSBuildImport import)
		{
			if (import.ParentObject == this) {
				import.RemoveIndent ();
				imports.Remove (import);
				NotifyChanged ();
			}
		}

		public IEnumerable<MSBuildImport> Imports {
			get { return imports; }
		}
	}
}
