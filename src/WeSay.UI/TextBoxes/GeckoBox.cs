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
		private GeckoBodyElement _bodyElement;
		private EventHandler _loadHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyDownHandler;
		private EventHandler<GeckoDomKeyEventArgs> _domKeyUpHandler;
		private EventHandler<GeckoDomEventArgs> _domFocusHandler;
		private EventHandler<GeckoDomEventArgs> _domBlurHandler;
		private EventHandler<GeckoDomEventArgs> _domClickHandler;
		private EventHandler _domDocumentChangedHandler;
		private EventHandler _textChangedHandler;
		private readonly string _nameForLogging;
		private bool _inFocus;

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
			_inFocus = false;

			var designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
			if (designMode)
				return;

			Debug.WriteLine("New GeckoBox");
			_browser = new GeckoWebBrowser();
			_browser.Dock = DockStyle.Fill;
			//_browser.Parent = this;
			_loadHandler = new EventHandler(GeckoBox_Load);
			this.Load += _loadHandler;
			Controls.Add(_browser);

			_domKeyDownHandler = new EventHandler<GeckoDomKeyEventArgs>(OnDomKeyDown);
			_browser.DomKeyDown += _domKeyDownHandler;
			_domKeyUpHandler = new EventHandler<GeckoDomKeyEventArgs>(OnDomKeyUp);
			_browser.DomKeyUp += _domKeyUpHandler;
			_domFocusHandler = new EventHandler<GeckoDomEventArgs>(_browser_DomFocus);
			_browser.DomFocus += _domFocusHandler;
			_domBlurHandler = new EventHandler<GeckoDomEventArgs>(_browser_DomBlur);
			_browser.DomBlur += _domBlurHandler;
			_domDocumentChangedHandler = new EventHandler(_browser_DomDocumentChanged);
			_browser.DocumentCompleted += _domDocumentChangedHandler;
			_domClickHandler = new EventHandler<GeckoDomEventArgs>(_browser_DomClick);
			_browser.DomClick += _domClickHandler;

			_textChangedHandler = new EventHandler(OnTextChanged);
			this.TextChanged += _textChangedHandler;
			this.ResumeLayout(false);
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
			_browser.DomBlur -= _domBlurHandler;
			_browser.DocumentCompleted -= _domDocumentChangedHandler;
			_browser.DomClick -= _domClickHandler;
			this.TextChanged -= _textChangedHandler;
			_loadHandler = null;
			_domKeyDownHandler = null;
			_domKeyUpHandler = null;
			_domClickHandler = null;
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
			var content = _browser.Document.GetElementById("mainbody");
			if (content != null)
			{
				if (content is GeckoBodyElement)
				{
<<<<<<< HEAD
					_bodyElement = (GeckoBodyElement) content;
=======
					_bodyElement = (GeckoBodyElement)content;
>>>>>>> 24ced5369f191555b27d0860504b399daa22332c
					Height = _bodyElement.ScrollHeight;
				}
			}
		}

		private delegate void ChangeFocusDelegate(GeckoDivElement ctl);
		private void _browser_DomFocus(object sender, GeckoDomEventArgs e)
		{
#if DEBUG
			Debug.WriteLine("Got Focus: " + Text);
#endif
			var content = _browser.Document.GetElementById("main");
			if (content != null)
			{
				if ((content is GeckoDivElement) && (!_inFocus))
				{
					_inFocus = true;
#if DEBUG
					Debug.WriteLine("Got Focus2: " + Text);
#endif
					_divElement = (GeckoDivElement) content;
					this.BeginInvoke (new ChangeFocusDelegate(changeFocus), _divElement);
				}
			}
		}
		private void _browser_DomBlur(object sender, GeckoDomEventArgs e)
		{
			_inFocus = false;
#if DEBUG
			Debug.WriteLine("Got Blur: " + Text);
#endif
		}
		private void _browser_DomClick(object sender, GeckoDomEventArgs e)
		{
#if DEBUG
			Debug.WriteLine ("Got Dom Mouse Click " + Text);
#endif
			_browser.Focus ();
		}

		private void changeFocus(GeckoDivElement ctl)
		{
#if DEBUG
			Debug.WriteLine("Change Focus: " + Text);
#endif
			ctl.Focus();
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
			if (_inFocus)
			{
				if (!MultiParagraph && e.KeyCode == 13) // carriage return
				{
					e.Handled = true;
				}
				else if ((e.KeyCode == 9) && !e.CtrlKey && !e.AltKey)
				{
					int a = ParentForm.Controls.Count;
#if DEBUG
					Debug.WriteLine ("Got a Tab Key " + Text + " Count " + a.ToString() );
#endif
					if (e.ShiftKey)
					{
						if (!ParentForm.SelectNextControl(this, false, true, true, true))
						{
#if DEBUG
							Debug.WriteLine("Failed to advance");
#endif
						}
					}
					else
					{
						if (!ParentForm.SelectNextControl(this, true, true, true, true))
						{
#if DEBUG
							Debug.WriteLine("Failed to advance");
#endif
						}
					}
				}
				else
				{
					this.RaiseKeyEvent(Keys.A, new KeyEventArgs(Keys.A));
				}
			}
		}

		private void GeckoBox_Load(object sender, EventArgs e)
		{
			_browserIsReadyToNavigate = true;
			if (_pendingHtmlLoad != null)
			{
#if DEBUG
				Debug.WriteLine("Load: " + _pendingHtmlLoad);
#endif
				_browser.LoadHtml(_pendingHtmlLoad);
				_pendingHtmlLoad = null;
			}
			else
			{
#if DEBUG
				Debug.WriteLine ("Load: Empty Line");
#endif
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
					"<html><header><meta charset=\"UTF-8\"></head><body style='background:#FFFFFF' id='mainbody'><div style='min-height:15px; font-family:{0}; font-size:{1}pt; text-align:{3}' id='main' name='textArea' contentEditable='{4}'>{2}</div></body></html>",
					WritingSystem.DefaultFontName, WritingSystem.DefaultFontSize.ToString(), s, justification, editable);
			if (!_browserIsReadyToNavigate)
			{
				_pendingHtmlLoad = html;
			}
			else
			{
				if (!_keyPressed)
				{
#if DEBUG
					Debug.WriteLine ("SetText: " + html);
#endif
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
				Debug.WriteLine("SetHTML: " + finalHtml);
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
//			ClearKeyboard();
		 }
		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
//			AssignKeyboardFromWritingSystem();
		}

	}
}
