
using System;
using System.IO;
using System.Text;
using Palaso.DictionaryServices.Model;
using Palaso.Lift;
using Palaso.DictionaryServices.Lift;
using Palaso.Lift.Options;
using Palaso.Text;
using System.Xml;
using System.Xml.Xsl;
using WeSay.LexicalModel;
using WeSay.LexicalModel.Foundation;
using WeSay.Project;
using WeSay.UI;
using WeSay.LexicalTools.Properties;

namespace WeSay.LexicalTools
{

	/// <summary>
	/// Renders a single LexEntry as an HTML string.
	/// </summary>
	public static class HtmlRenderer
	{
		private static XslCompiledTransform transform = null;
		private static XmlReaderSettings readerSettings = null;

		public static string ToHtml(LexEntry entry,
								   CurrentItemEventArgs currentItem,
								   LexEntryRepository lexEntryRepository,
									ViewTemplate viewTemplate)
		{
			string html = "";
			StringBuilder builder = new StringBuilder();
			LiftWriter writer = new LiftWriter(builder, true, true);

			writer.Add(entry);
			writer.End();// Needed to flush output
			writer.Dispose();
			if (transform == null) initTransform();
			string xml = builder.ToString();
			System.IO.StringReader entryReader = new System.IO.StringReader(xml);
			XsltArgumentList xsltArgs = new XsltArgumentList();
			string basePath = "file://" + WeSayWordsProject.Project.ProjectDirectoryPath.Replace('\\','/');
			xsltArgs.AddParam("baseUrl", "", basePath);
			xsltArgs.AddParam("extraStyles", "", getWritingSystemStyles(viewTemplate));
			XmlTextReader entryXmlReader = new XmlTextReader(entryReader);
			TextWriter htmlTextWriter = new StringWriter();
			XmlWriter htmlWriter = new XmlTextWriter(htmlTextWriter);
			transform.Transform(entryXmlReader, xsltArgs, htmlWriter);
			html = htmlTextWriter.ToString();
			writer.Dispose();
			htmlWriter.Close();
#if DEBUG
			// temp hack
			try
			{
				string temp = System.Environment.GetEnvironmentVariable("TEMP");
				if (temp == null || temp.Length == 0) temp = "/tmp/";
				System.IO.File.WriteAllText(Path.Combine(temp, "LexicalEntry.xml"), xml);
				System.IO.File.WriteAllText(Path.Combine(temp, "LexicalEntry.html"), html);
			}
			catch (Exception e) {}
#endif
			return html;
		}

		private static string getWritingSystemStyles(ViewTemplate viewTemplate)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach (WritingSystem ws in viewTemplate.WritingSystems.Values)
			{
				// Note: It is possible to configure IE to ignore these styles.
				// In IE goto Internet Options dialog, General tab, Accessibility button,
				// make sure that "Ignore font styles specified on webpages" is not selected.

				// :lang is not supported, in IE8, so use traditional styles
				builder.Append(".lang_");
				// Are all abbreviations ok as class names? ISO639-3 should be fine.
				builder.Append(ws.Abbreviation);
				builder.Append(" { font-family: \"");
				builder.Append(ws.FontName);
				builder.Append("\"; font-size: ");
				builder.Append(ws.FontSize);
				builder.Append("pt;");
				builder.Append("}\n");

				// attribute styles below aren't supported in IE 8
				builder.Append(":lang(");
				builder.Append(ws.Abbreviation);
				builder.Append(") { font-family: \"");
				builder.Append(ws.FontName);
				builder.Append("\"; font-size: ");
				builder.Append(ws.FontSize);
				builder.Append("pt;");
				builder.Append("}\n");
			}
			return builder.ToString();
		}

		private static void initTransform()
		{
			transform = new XslCompiledTransform();
			readerSettings = new XmlReaderSettings();
			System.IO.StringReader sr =
					   new System.IO.StringReader(Resources.liftEntry2Html);
			XmlReader xsltReader = XmlReader.Create(sr, readerSettings);
			XsltSettings settings = new XsltSettings(true, true);
			transform.Load(xsltReader, settings, new XmlUrlResolver());
			xsltReader.Close();
		}

	}
}
