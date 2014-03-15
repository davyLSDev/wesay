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
		private bool _keyPressed;
		private GeckoDivElement _divElement;
		private EventHandler _loadHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyDownHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyUpHandler;
		private EventHandler<GeckoDomEventArgs> _domFocusHandler;
		private EventHandler _domDocumentChangedHandler;
		private EventHandler _textChangedHandler;
		private readonly string _nameForLogging;

		public GeckoBox()
		{
			InitializeComponent();

			if (_nameForLogging == null)
			{
				_nameForLogging = "??";
			}
			Name = _nameForLogging;
			_keyPressed = false;
			ReadOnly = false;

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
			_domDocumentChangedHandler = new EventHandler(_browser_DomDocumentChanged);
			_browser.DocumentCompleted += _domDocumentChangedHandler;

			_textChangedHandler = new EventHandler(OnTextChanged);
			this.TextChanged += _textChangedHandler;
		}

		public GeckoBox(IWritingSystemDefinition ws, string nameForLogging)
			: this()
		{
			_nameForLogging = nameForLogging;
			if (_nameForLogging == null)
			{
				_nameForLogging = "??";
			}
			Name = _nameForLogging;
			WritingSystem = ws;
		}

		public void Closing()
		{
			this.Load -= _loadHandler;
			_browser.DomKeyDown -= _domKeyDownHandler;
			_browser.DomKeyUp -= _domKeyUpHandler;
			_browser.DomFocus -= _domFocusHandler;
			_browser.DocumentCompleted -= _domDocumentChangedHandler;
			this.TextChanged -= _textChangedHandler;
			_loadHandler = null;
			_domKeyDownHandler = null;
			_domKeyUpHandler = null;
			_textChangedHandler = null;
			_domFocusHandler = null;
			_domDocumentChangedHandler = null;
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
		private void OnTextChanged(object sender, EventArgs e)
		{
			SetText(Text);
		}

		private void _browser_DomDocumentChanged(object sender, EventArgs e)
		{
			var content = _browser.Document.GetElementById("main");
			if (content != null)
			{
				if (content is GeckoDivElement)
				{
					_divElement = (GeckoDivElement) content;
					Height = _divElement.ClientHeight + 10;
				}
			}
		}

		private void _browser_DomFocus(object sender, GeckoDomEventArgs e)
		{
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
			_keyPressed = true;

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
				ParentForm.SelectNextControl(this, true, true, true, true);
			}
			else
			{
				this.RaiseKeyEvent(Keys.A, new KeyEventArgs(Keys.A));
			}
		}


		private void GeckoBox_Load(object sender, EventArgs e)
		{
			_browserIsReadyToNavigate = true;
			if (_pendingHtmlLoad != null)
			{
				_browser.LoadHtml(_pendingHtmlLoad);
				_pendingHtmlLoad = null;
			}
			else
			{
				SetText(""); //make an empty, editable box
			}
		}

		private void SetText(string s)
		{
			String justification = "left";
			if (_writingSystem != null && WritingSystem.RightToLeftScript)
			{
				justification = "right";
			}

			String editable = "true";
			if (ReadOnly)
			{
				editable = "false";
			}
			var html =
				string.Format(
					"<html><header><meta charset=\"UTF-8\"></head><body style='background:#FFFFFF'><div style='min-height:15px; font-family:{0}; font-size:{1}pt; text-align:{3}' id='main' name='textArea' contentEditable='{4}'>{2}</div></body></html>",
					WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), s, justification, editable);
			if (!_browserIsReadyToNavigate)
			{
				_pendingHtmlLoad = html;
			}
			else
			{
				if (!_keyPressed)
				{

					_browser.LoadHtml(html);
				}
				_keyPressed = false;
			}
		}

		public void SetHtml(string html)
		{
			String strHtmlColor = System.Drawing.ColorTranslator.ToHtml(BackColor);
			var finalHtml = string.Format(html, strHtmlColor);
			if (!_browserIsReadyToNavigate)
			{
				_pendingHtmlLoad = finalHtml;
			}
			else
			{
				_browser.LoadHtml(finalHtml);
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

		public bool ReadOnly { get; set; }

		public bool Multiline
		{
			get
			{
				//todo
				return false;
			}
			set
			{
				//todo
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
			{
				//todo
			}
		}

		// we do this in OnLayout instead of OnResize see
		// "Setting the Size/Location of child controls in the Resize event
		// http://blogs.msdn.com/jfoscoding/archive/2005/03/04/385625.aspx
/*		protected override void OnLayout(LayoutEventArgs levent)
		{
			Height = GetPreferredHeight(Width);
			base.OnLayout(levent);
		}

		// we still need the resize sometimes or ghost fields disappear
		protected override void OnSizeChanged(EventArgs e)
		{
			Height = GetPreferredHeight(Width);
			base.OnSizeChanged(e);
		}

		protected override void OnResize(EventArgs e)
		{
			Height = GetPreferredHeight(Width);
			base.OnResize(e);
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			Size size = base.GetPreferredSize(proposedSize);
			size.Height = GetPreferredHeight(size.Width);
			return size;
		}

		private int GetPreferredHeight(int width)
		{
//			using (Graphics g = CreateGraphics())
			{
			//	Graphics g = CreateGraphics();
/*				TextFormatFlags flags = TextFormatFlags.TextBoxControl | TextFormatFlags.Default |
										TextFormatFlags.NoClipping;
				if (Multiline && WordWrap)
				{
					flags |= TextFormatFlags.WordBreak;
				}
				if (_writingSystem != null && WritingSystem.RightToLeftScript)
				{
					flags |= TextFormatFlags.RightToLeft;
				}
				Size sz = TextRenderer.MeasureText(g,
												   Text == String.Empty ? " " : Text + "\n",
					// replace empty string with space, because mono returns zero height for empty string (windows returns one line height)
					// need extra new line to handle case where ends in new line (since last newline is ignored)
												   Font,
												   new Size(width, int.MaxValue),
												   flags);
				return sz.Height + 2; // add enough space for spell checking squiggle underneath
 * *
//				g.Dispose();
				return 32;
			}
		}
*/

		/// <summary>
		/// for automated tests
		/// </summary>
		public void PretendLostFocus()
		{
			OnLostFocus(new EventArgs());
		}

		/// <summary>
		/// for automated tests
		/// </summary>
		public void PretendSetFocus()
		{
			Debug.Assert(_browser != null, "_browser != null");
			_browser.Focus();
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
