using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gecko;

namespace GeckoTextBox
{
	public interface IWeSayTextBox
	{
		Size GetPreferredSize(Size proposedSize);

		[Browsable(false)]
		string Text { set; get; }

//		[Browsable(false)]
//		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
//		WritingSystemDefinition WritingSystem { get; set; }

		bool MultiParagraph { get; set; }
		bool IsSpellCheckingEnabled { get; set; }
		int SelectionStart { get; set; }
		void AssignKeyboardFromWritingSystem();
		void ClearKeyboard();

		/// <summary>
		/// for automated tests
		/// </summary>
		void PretendLostFocus();

		void AppendText(string text);
	}

	public partial class GeckoBox : UserControl , IWeSayTextBox
	{
		private GeckoWebBrowser _browser;
		private bool _browserIsReadyToNavigate;
		private string _pendingHtmlLoad;

		public GeckoBox()
		{
			InitializeComponent();

			var designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
			if (designMode)
				return;

			_browser = new GeckoWebBrowser();
			_browser.Dock = DockStyle.Fill;
			_browser.Parent = this;
			this.Load += new EventHandler(GeckoBox_Load);
			Controls.Add(_browser);
		}

		void GeckoBox_Load(object sender, EventArgs e)
		{
			_browserIsReadyToNavigate = true;
			if(_pendingHtmlLoad!=null)
			{
				_browser.LoadHtml(_pendingHtmlLoad);
				_pendingHtmlLoad = null;
			}
			else
			{
				Text = "";//make an empty, editable box
			}
		}

		public string Text
		{
			get
			{
				return _browser.DomDocument.TextContent;
			}
			set
			{
				var html = string.Format("<html><body><div id='main' contentEditable='true'>{0}</div></body></html>", value);
				if (!_browserIsReadyToNavigate)
				{
					_pendingHtmlLoad = html;
				}
				else
				{
					_browser.LoadHtml(html);
				}
			}
		}

		public bool MultiParagraph { get; set; }

		public bool IsSpellCheckingEnabled { get; set; }


		public int SelectionStart
		{
			get
			{
				//TODO
				return 0;
			}
			set
			{
				//TODO
			}
		}

		public void AssignKeyboardFromWritingSystem()
		{
			//TODO
		}

		public void ClearKeyboard()
		{
			//TODO
		}

		public void PretendLostFocus()
		{
			//TODO
		}

		public void AppendText(string text)
		{
			//TODO
		}
	}
}
