using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Gecko;
using Palaso.WritingSystems;

namespace WeSay.UI.TextBoxes
{
	public partial class GeckoBox : UserControl, IWeSayTextBox, IControlThatKnowsWritingSystem
	{
		private GeckoWebBrowser _browser;
		private bool _browserIsReadyToNavigate;
		private string _pendingHtmlLoad;

		public GeckoBox(WritingSystemDefinition writingSystem, string name)
		{
			InitializeComponent();

			WritingSystem = writingSystem;
			Name = name;

			var designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
			if (designMode)
				return;

			_browser = new GeckoWebBrowser();
			_browser.Dock = DockStyle.Fill;

			_browser.Parent = this;
			this.Load += new EventHandler(GeckoBox_Load);
			Controls.Add(_browser);

			_browser.DomKeyDown +=new EventHandler<GeckoDomKeyEventArgs>(OnDomKeyDown);
			_browser.DomFocus += new EventHandler<GeckoDomEventArgs>(_browser_DomFocus);

			this.TextChanged += new EventHandler(OnTextChanged);
		}

		/// <summary>
		/// called when the client changes our Control.Text... we need to them move that into the html
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnTextChanged(object sender, EventArgs e)
		{
			SetText(Text);
		}

		void _browser_DomFocus(object sender, GeckoDomEventArgs e)
		{
			//the ghostBinding is relying on Control.Enter
			//todo: how to raise the control's enter? or may it already is raised?
			//if we have to, we can switch it to rely on an event we add to IWeSayTextBox
		}

		private void OnDomKeyDown(object sender, GeckoDomKeyEventArgs e)
		{
			var content = _browser.Document.GetElementById("main");

			Debug.WriteLine(content.TextContent);
			Text = content.TextContent;

			//todo
			this.RaiseKeyEvent(Keys.A, new KeyEventArgs(Keys.A));
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
				SetText("");//make an empty, editable box
			}
		}

		private void SetText(string s)
		{
			var html = string.Format("<html><body style='background:#CEECF5'><div style='min-height:15px; font-family:{0}; font-size:{1}pt' id='main' contentEditable='true'>{2}</div></body></html>", WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), s);
			if (!_browserIsReadyToNavigate)
			{
				_pendingHtmlLoad = html;
			}
			else
			{
				_browser.LoadHtml(html);
			}
		}

		public WritingSystemDefinition WritingSystem { get; set; }

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

		public bool ReadOnly
		{
			get {
			//todo
				return false;
			}
			set { //todo
				}
		}

		public bool Multiline
		{
			get
			{
				//todo
				return false;
			}
			set
			{ //todo
			}
		}

		public bool WordWrap
		{
			get
			{
				//todo
				return false;
			}
			set
			{ //todo
			}
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
