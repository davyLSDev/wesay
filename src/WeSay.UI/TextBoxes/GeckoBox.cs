using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Gecko;
using Gecko.DOM;
using Palaso.WritingSystems;

namespace WeSay.UI.TextBoxes
{
	public partial class GeckoBox : UserControl, IWeSayTextBox, IControlThatKnowsWritingSystem
	{
		private GeckoWebBrowser _browser;
		private bool _browserIsReadyToNavigate;
		private string _pendingHtmlLoad;
		private IWritingSystemDefinition _writingSystem;
		private bool keyPressed;
		private GeckoDivElement _divElement;
		private EventHandler _loadHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyDownHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyUpHandler;
		private EventHandler<GeckoDomEventArgs> _domFocusHandler;
		private EventHandler _textChangedHandler;

		public GeckoBox(IWritingSystemDefinition writingSystem, string name)
		{
			InitializeComponent();

			_writingSystem = writingSystem;
			Name = name;
			keyPressed = false;

			var designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
			if (designMode)
				return;

			_browser = new GeckoWebBrowser();
			_browser.Dock = DockStyle.Fill;

			_browser.Parent = this;
			_loadHandler = new EventHandler(GeckoBox_Load);
			this.Load += _loadHandler;
			Controls.Add(_browser);

			_domKeyDownHandler = new EventHandler<GeckoDomKeyEventArgs>(OnDomKeyDown);
			_browser.DomKeyDown += _domKeyDownHandler;
			_domKeyUpHandler = new EventHandler<GeckoDomKeyEventArgs>(OnDomKeyUp);
			_browser.DomKeyUp += _domKeyUpHandler;
			_domFocusHandler = new EventHandler<GeckoDomEventArgs>(_browser_DomFocus);
			_browser.DomFocus += _domFocusHandler;

			_textChangedHandler = new EventHandler(OnTextChanged);
			this.TextChanged += _textChangedHandler;
		}

		public void Closing()
		{
			this.Load -= _loadHandler;
			_browser.DomKeyDown -= _domKeyDownHandler;
			_browser.DomKeyUp -= _domKeyUpHandler;
			_browser.DomFocus -= _domFocusHandler;
			this.TextChanged -= _textChangedHandler;
			_loadHandler = null;
			_domKeyDownHandler = null;
			_domKeyUpHandler = null;
			_textChangedHandler = null;
			_domFocusHandler = null;
			_divElement = null;
			_browser.Stop();
			_browser.Dispose();
			_browser = null;
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
			var content = _browser.Document.GetElementById("main");
			if (content != null)
			{
				if (content is GeckoDivElement)
				{
					_divElement = (GeckoDivElement) content;
					_divElement.Focus();
				}
			}
		}
		private void OnDomKeyUp(object sender, GeckoDomKeyEventArgs e)
		{
			var content = _browser.Document.GetElementById("main");
			keyPressed = true;

			//			Debug.WriteLine(content.TextContent);
			Text = content.TextContent;
		}

		private void OnDomKeyDown(object sender, GeckoDomKeyEventArgs e)
		{
			if (!MultiParagraph && e.KeyCode == 13) // carriage return
			{
				e.Handled = true;
			}
			else if ((e.KeyCode == 9) && !e.CtrlKey && !e.AltKey && !e.ShiftKey)
			{
				ParentForm.SelectNextControl(this,true,true,true,true);
			}
			else
			{
				this.RaiseKeyEvent(Keys.A, new KeyEventArgs(Keys.A));
			}
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

		private string UTF8Encode(string standardText)
		{
			UTF8Encoding utf8 = new UTF8Encoding();
//			byte[] encodedBytes = utf8.GetBytes(standardText);
			byte[] encodedBytes = Encoding.Unicode.GetBytes(standardText);
//			string encodedString = utf8.GetString(encodedBytes);
			string encodedString = Encoding.UTF8.GetString(encodedBytes);
			return encodedString;
		}
		private void SetText(string s)
		{
			var html = string.Format("<html><header><meta charset=\"UTF-8\"></head><body style='background:#FFFFFF'><div style='min-height:10px; font-family:{0}; font-size:{1}pt' id='main' name='textArea' contentEditable='true'>{2}</div></body></html>", WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), s);
//			var html = string.Format("<html><body style='background:#CEECF5'><div style='min-height:15px; font-family:{0}; font-size:{1}pt' id='main' name='textArea' contentEditable='true'>{2}</div></body></html>", WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), UTF8Encode(s));
//			string htmlString = string.Format("<html><body style='background:#CEECF5'><div style='min-height:15px; font-family:{0}; font-size:{1}pt' id='main' name='textArea' contentEditable='true'>{2}</div></body></html>", WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), s);
//			var html = UTF8Encode(htmlString);
			if (!_browserIsReadyToNavigate)
			{
				_pendingHtmlLoad = html;
			}
			else
			{
				if (!keyPressed)
				{

					_browser.LoadHtml(html);
				}
				keyPressed = false;
			}
		}

		public IWritingSystemDefinition WritingSystem
		{
			get
			{
				if (_writingSystem == null)
				{
					throw new InvalidOperationException(
						"Input system must be initialized prior to use.");
				}
				return _writingSystem;
			}

			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				_writingSystem = value;
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
			if (_writingSystem == null)
			{
				throw new InvalidOperationException(
					"Input system must be initialized prior to use.");
			}

			_writingSystem.LocalKeyboard.Activate();
		}

		public void ClearKeyboard()
		{
			if (_writingSystem == null)
			{
				throw new InvalidOperationException(
					"Input system must be initialized prior to use.");
			}

			Keyboard.Controller.ActivateDefaultKeyboard();
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

		/// <summary>
		/// for automated tests
		/// </summary>
		public void PretendLostFocus()
		{
			OnLostFocus(new EventArgs());
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			// this.BackColor = System.Drawing.Color.White;
			ClearKeyboard();
		 }
		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			AssignKeyboardFromWritingSystem();
		}

	}
}
