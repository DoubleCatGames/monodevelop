//
// MSBuildProperty.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Microsoft.Build.BuildEngine;
using System.Xml.Linq;
using MonoDevelop.Core;
using System.Globalization;
using System.Text;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProperty: MSBuildPropertyCore, IMetadataProperty, IMSBuildPropertyEvaluated
	{
		bool preserverCase;
		MSBuildValueType valueType = MSBuildValueType.Default;
		string value;
		string name;

		internal MSBuildProperty ()
		{
			NotifyChanges = true;
		}

		internal MSBuildProperty (string name): this ()
		{
			this.name = name;
		}

		internal override void Read (MSBuildXmlReader reader)
		{
			name = reader.LocalName;
			base.Read (reader);
		}

		internal override void ReadContent (MSBuildXmlReader reader)
		{
			value = ReadValue (reader.XmlReader);
		}

		internal override void WriteContent (XmlWriter writer, WriteContext context)
		{
			writer.WriteValue (value);
		}

		internal override string GetElementName ()
		{
			return name;
		}

		static XmlDocument helperDoc = new XmlDocument ();

		string ReadValue (XmlReader reader)
		{
			// This code is from Microsoft.Build.Internal.Utilities

			XmlElement elem;
			lock (helperDoc)
				elem = (XmlElement) helperDoc.ReadNode (reader);

			if (!elem.HasChildNodes)
				return string.Empty;

			if (elem.ChildNodes.Count == 1 && (elem.FirstChild.NodeType == XmlNodeType.Text || elem.FirstChild.NodeType == XmlNodeType.CDATA))
				return elem.InnerText.Trim ();

			string innerXml = elem.InnerXml;

			// If there is no markup under the XML node (detected by the presence
			// of a '<' sign
			int firstLessThan = innerXml.IndexOf('<');
			if (firstLessThan == -1) {
				// return the inner text so it gets properly unescaped
				return elem.InnerText.Trim ();
			}

			bool containsNoTagsOtherThanComments = ContainsNoTagsOtherThanComments (innerXml, firstLessThan);

			// ... or if the only XML is comments,
			if (containsNoTagsOtherThanComments) {
				// return the inner text so the comments are stripped
				// (this is how one might comment out part of a list in a property value)
				return elem.InnerText.Trim ();
			}

			// ...or it looks like the whole thing is a big CDATA tag ...
			bool startsWithCData = (innerXml.IndexOf("<![CDATA[", StringComparison.Ordinal) == 0);

			if (startsWithCData) {
				// return the inner text so it gets properly extracted from the CDATA
				return elem.InnerText.Trim ();
			}

			// otherwise, it looks like genuine XML; return the inner XML so that
			// tags and comments are preserved and any XML escaping is preserved
			return innerXml;
		}

		internal virtual MSBuildProperty Clone (XmlDocument newOwner = null)
		{
			var prop = (MSBuildProperty)MemberwiseClone ();
			prop.ParentObject = null;
			return prop;
		}

		internal override string GetName ()
		{
			return name;
		}

		public bool IsImported {
			get;
			set;
		}

		public bool MergeToMainGroup { get; set; }

		internal bool Overwritten { get; set; }

		internal MSBuildPropertyGroup Owner { get; set; }

		internal bool HasDefaultValue { get; set; }

		internal bool NotifyChanges { get; set; }

		internal MSBuildValueType ValueType {
			get { return valueType; }
		}

		internal MergedProperty CreateMergedProperty ()
		{
			return new MergedProperty (Name, preserverCase, HasDefaultValue);
		}

		public void SetValue (string value, bool preserveCase = false, bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = preserveCase;
			valueType = preserveCase ? MSBuildValueType.Default : MSBuildValueType.DefaultPreserveCase;

			if (value == null)
				value = String.Empty;

			if (preserveCase) {
				var current = GetPropertyValue ();
				if (current != null) {
					if (current.Equals (value, preserveCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
						return;
				}
			}
			SetPropertyValue (value);
			if (ParentProject != null && NotifyChanges)
				ParentProject.NotifyChanged ();
		}

		public void SetValue (FilePath value, bool relativeToProject = true, FilePath relativeToPath = default(FilePath), bool mergeToMainGroup = false)
		{
			MergeToMainGroup = mergeToMainGroup;
			this.preserverCase = false;
			valueType = MSBuildValueType.Path;

			string baseDir = null;
			if (relativeToPath != null) {
				baseDir = relativeToPath;
			} else if (relativeToProject) {
				if (ParentProject == null) {
					// The project has not been set, so we can't calculate the relative path.
					// Store the full path for now, and set the property type to UnresolvedPath.
					// When the property gets a value, the relative path will be calculated
					valueType = MSBuildValueType.UnresolvedPath;
					SetPropertyValue (value.ToString ());
					return;
				}
				baseDir = ParentProject.BaseDirectory;
			}

			// If the path is normalized in the property, keep the value
			if (!string.IsNullOrEmpty (Value) && new FilePath (MSBuildProjectService.FromMSBuildPath (baseDir, Value)).CanonicalPath == value.CanonicalPath)
				return;

			SetPropertyValue (MSBuildProjectService.ToMSBuildPath (baseDir, value, false));
			if (ParentProject != null && NotifyChanges)
				ParentProject.NotifyChanged ();
		}

		internal void ResolvePath ()
		{
			if (valueType == MSBuildValueType.UnresolvedPath) {
				var val = Value;
				SetPropertyValue (MSBuildProjectService.ToMSBuildPath (ParentProject.BaseDirectory, val, false));
			}
		}

		public void SetValue (object value, bool mergeToMainGroup = false)
		{
			if (value is bool) {
				if (Owner != null && Owner.UppercaseBools)
					SetValue ((bool)value ? "True" : "False", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
				else
					SetValue ((bool)value ? "true" : "false", preserveCase: true, mergeToMainGroup: mergeToMainGroup);
				valueType = MSBuildValueType.Boolean;
			}
			else
				SetValue (Convert.ToString (value, CultureInfo.InvariantCulture), false, mergeToMainGroup);
		}

		internal virtual void SetPropertyValue (string value)
		{
			this.value = value;
			if (ParentProject != null && NotifyChanges)
				ParentProject.NotifyChanged ();
		}

/*		internal virtual void SetPropertyValue (string value)
		{
			if (Element.IsEmpty && string.IsNullOrEmpty (value))
				return;

			if (value == null)
				value = string.Empty;

			// This code is from Microsoft.Build.Internal.Utilities

			if (value.IndexOf('<') != -1) {
				// If the value looks like it probably contains XML markup ...
				try {
					// Attempt to store it verbatim as XML.
					Element.InnerXml = value;
					if (Project != null)
						XmlUtil.FormatElement (Project.TextFormat, Element);
					return;
				}
				catch (XmlException) {
					// But that may fail, in the event that "value" is not really well-formed
					// XML.  Eat the exception and fall through below ...
				}
			}

			// The value does not contain valid XML markup.  Store it as text, so it gets 
			// escaped properly.
			Element.InnerText = value;
			if (Project != null && NotifyChanges)
				Project.NotifyChanged ();
		}*/

		internal override string GetPropertyValue ()
		{
			return value;
		}

		// This code is from Microsoft.Build.Internal.Utilities

		/// <summary>
		/// Figure out whether there are any XML tags, other than comment tags,
		/// in the string.
		/// </summary>
		/// <remarks>
		/// We know the string coming in is a valid XML fragment. (The project loaded after all.)
		/// So for example we can ignore an open comment tag without a matching closing comment tag.
		/// </remarks>
		static bool ContainsNoTagsOtherThanComments (string innerXml, int firstLessThan)
		{
			bool insideComment = false;
			for (int i = firstLessThan; i < innerXml.Length; i++)
			{
				if (!insideComment)
				{
					// XML comments start with exactly "<!--"
					if (i < innerXml.Length - 3
						&& innerXml[i] == '<'
						&& innerXml[i + 1] == '!'
						&& innerXml[i + 2] == '-'
						&& innerXml[i + 3] == '-')
					{
						// Found the start of a comment
						insideComment = true;
						i = i + 3;
						continue;
					}
				}

				if (!insideComment)
				{
					if (innerXml[i] == '<')
					{
						// Found a tag!
						return false;
					}
				}

				if (insideComment)
				{
					// XML comments end with exactly "-->"
					if (i < innerXml.Length - 2
						&& innerXml[i] == '-'
						&& innerXml[i + 1] == '-'
						&& innerXml[i + 2] == '>')
					{
						// Found the end of a comment
						insideComment = false;
						i = i + 2;
						continue;
					}
				}
			}

			// Didn't find any tags, except possibly comments
			return true;
		}

		public override string UnevaluatedValue {
			get {
				return Value;
			}
		}
	}

	class ItemMetadataProperty: MSBuildProperty
	{
		string value;
		string unevaluatedValue;
		string name;

		public ItemMetadataProperty (string name)
		{
			NotifyChanges = false;
			this.name = name;
		}

		public ItemMetadataProperty (string name, string value, string unevaluatedValue)
		{
			NotifyChanges = false;
			this.name = name;
			this.value = value;
			this.unevaluatedValue = unevaluatedValue;
		}

		internal override string GetName ()
		{
			return name;
		}

		internal override void SetPropertyValue (string value)
		{
			if (value != this.value)
				this.value = unevaluatedValue = value;
		}

		internal override string GetPropertyValue ()
		{
			return value;
		}

		public override string UnevaluatedValue {
			get {
				return unevaluatedValue;
			}
		}
	}

	class MergedProperty
	{
		public readonly string Name;
		public readonly bool IsDefault;
		public readonly bool PreserveExistingCase;

		public MergedProperty (string name, bool preserveExistingCase, bool isDefault)
		{
			this.Name = name;
			IsDefault = isDefault;
			this.PreserveExistingCase = preserveExistingCase;
		}
	}
}