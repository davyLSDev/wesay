using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Chorus;
using Chorus.UI.Notes;
using Palaso.DictionaryServices.Model;
using WeSay.LexicalModel;
using WeSay.Project;
using WeSay.UI;

namespace WeSay.LexicalTools
{
	public partial class EntryHeaderView : UserControl
	{
		private const int kNotesBarHeight = 30;//we want 16pixel icons
		//autofac generates a factory which comes up with all the other needed parameters from its container
		public delegate EntryHeaderView Factory();

		private NotesBarView _notesBar;
		private LexEntry _currentRecord=null;
		private string _htmlPath = null;
		private string _browserUrl = "about:blank";
		private const string _emptyEntryHtml =
			"<html><head><style type=\"text/css\">body { background-color: #cbffb9; }</style></head><body></body></html>";
		private string _lastEntryHtml = _emptyEntryHtml;

		/// <summary>
		/// designer only
		/// </summary>
		public EntryHeaderView()
		{
			SetupEntryHtmlPath();
			InitializeComponent();
		}

		public EntryHeaderView(NotesBarView notesBarView)
		{
			SetupEntryHtmlPath();
			InitializeComponent();
			_notesBar = notesBarView;// notesSystem.CreateNotesBarView(id => WeSayWordsProject.GetUrlFromLexEntry(_currentRecord));
			_notesBar.BorderStyle = System.Windows.Forms.BorderStyle.None;
			_notesBar.Dock = System.Windows.Forms.DockStyle.Top;
			_notesBar.Location = new System.Drawing.Point(0, 0);
			_notesBar.Name = "notesBar";
			_notesBar.BackColor = this.BackColor;
			//notesBar.Size = new System.Drawing.Size(474, 85);
			_notesBar.TabIndex = 1;
			_notesBar.TabStop = false;
			//_notesBar.Visible = false;//wait until we have a record to show
			_notesBar.Height = kNotesBarHeight;
			this.Controls.Add(_notesBar);
			Controls.SetChildIndex(_notesBar, 0);
			_notesBar.SizeChanged += new EventHandler(_notesBar_SizeChanged);
			DoLayout();
		}

		private void SetupEntryHtmlPath()
		{
			// A real file URL is used for Mono/Mozilla because in Mozila's security model
			// images on the local filesystem will only load if the baseUrl of the document
			// is a parent or at the same level as the image. If you just use javascript
			// to set the document content, it won't load the image, even if the URL is correct.
			_htmlPath = Path.Combine(WeSayWordsProject.Project.ProjectDirectoryPath,
															 "LexicalEntry.html");
			_browserUrl = "file://" + _htmlPath.Replace('\\', '/');
		}

		protected override void OnHandleCreated (System.EventArgs e)
		{
			base.OnHandleCreated (e);
			Palaso.Reporting.Logger.WriteEvent("EntryHeaderView Handle Created");
			InitializeBrowser();
			_entryHeaderBrowser.DocumentCompleted += _browserDocumentLoaded;
			_entryHeaderBrowser.Navigating += _browserNavigating;
#if MONO
			System.IO.File.WriteAllText(_htmlPath, _lastEntryHtml);
			_entryHeaderBrowser.Navigate(_browserUrl);
			//_entryHeaderBrowser.Navigate("javascript:{" + _lastEntryHtml.Replace("'","\'") + "}");
#else
			_entryHeaderBrowser.DocumentText = _lastEntryHtml;
#endif
			DoLayout();
		}

		public void PrepareToDispose()
		{
			// The Mono WebBrowser seems to be really fragile and will crash if you try to
			// recreate it after it has already been disposed once.
			// This method takes care of only disposing of it when the application
			// is really closing, rather than just the user switching to another tab.
			if (_entryHeaderBrowser != null && !_entryHeaderBrowser.IsDisposed)
			{
				Palaso.Reporting.Logger.WriteEvent("EntryHeaderView PrepareToDispose");
				_entryHeaderBrowser.DocumentCompleted -= _browserDocumentLoaded;
				_entryHeaderBrowser.Navigating -= _browserNavigating;

				// Removing the control before the EntryHeaderView is disposed helps to
				// avoid X locking up on Mono.
				Controls.Remove(_entryHeaderBrowser);
				if (TopLevelControl == null || TopLevelControl.Disposing)
				{
					Palaso.Reporting.Logger.WriteEvent("EntryHeaderView will really dispose browser");
					try
					{
						_entryHeaderBrowser.Dispose();
					}
					catch (Exception e)
					{
						Palaso.Reporting.Logger.WriteEvent(e.StackTrace);
					}
					_entryHeaderBrowser = null;
				}
			}
		}

		protected override void OnHandleDestroyed (System.EventArgs e)
		{
			Palaso.Reporting.Logger.WriteEvent("EntryHeaderView Handle Destroyed");
			base.OnHandleDestroyed (e);
		}


		void _browserDocumentLoaded(object sender, WebBrowserDocumentCompletedEventArgs args)
		{
			Palaso.Reporting.Logger.WriteEvent("WebBrowserDocumentCompletedEvent");
			// work around for text not being set when browser is first initialized
#if !MONO
			if (!_lastEntryHtml.Equals(_entryHeaderBrowser.DocumentText))
				_entryHeaderBrowser.DocumentText = _lastEntryHtml;
#endif
		}

		void _browserNavigating(object sender, WebBrowserNavigatingEventArgs browseArgs)
		{
			// TODO add support for links to other entries
			Palaso.Reporting.Logger.WriteMinorEvent("BrowserNavigating {0}" ,
												   new Object[] {browseArgs.Url });
		}

		void _notesBar_SizeChanged(object sender, EventArgs e)
		{
			_notesBar.Height = kNotesBarHeight;
		}

		public string RtfForTests
		{
			get { return this._entryPreview.Rtf; }
		}

		public string TextForTests
		{
			get { return this._entryPreview.Text; }
		}

		private void EntryHeaderView_Load(object sender, EventArgs e)
		{

		}

		public void UpdateContents(LexEntry record, CurrentItemEventArgs currentItemInFocus, LexEntryRepository lexEntryRepository)
		{
			if (record == null)
			{
				_entryPreview.Rtf = string.Empty;
				if (_entryHeaderBrowser != null)
				{
#if MONO
				//_entryHeaderBrowser.Navigate("javascript:{document.body.outerHTML='" + _emptyEntryHtml + "'}");
				System.IO.File.WriteAllText(_htmlPath, _emptyEntryHtml);
				_entryHeaderBrowser.Navigate(_browserUrl);
#else
				_entryHeaderBrowser.DocumentText = _emptyEntryHtml;
#endif
				}
			}
			else
			{
				_entryPreview.Rtf = RtfRenderer.ToRtf(record,
													  currentItemInFocus,
													  lexEntryRepository);
				ViewTemplate viewTemplate = (ViewTemplate)
						WeSayWordsProject.Project.ServiceLocator.GetService(typeof(ViewTemplate));
				_lastEntryHtml = HtmlRenderer.ToHtml(record, currentItemInFocus, lexEntryRepository, viewTemplate);
				if (_entryHeaderBrowser != null)
				{
#if MONO
					//_entryHeaderBrowser.Navigate("javascript:{document.body.outerHTML='" +
					//_lastEntryHtml.Replace("'","\'") + "'}");
					System.IO.File.WriteAllText(_htmlPath, _lastEntryHtml);
					_entryHeaderBrowser.Navigate(_browserUrl);
#else
				 _entryHeaderBrowser.DocumentText = _lastEntryHtml;

#endif
				}
			}

			if (record != _currentRecord)
			{
				_currentRecord = record;
				if (_notesBar == null)
					return;
				if (record == null)
				{
					_notesBar.SetTargetObject(null);
				}
				else
				{
					_notesBar.SetTargetObject(record);
				}
			}

			//_notesBar.Visible = true;
		}

		private void EntryHeaderView_BackColorChanged(object sender, EventArgs e)
		{
			_entryPreview.BackColor = BackColor;
			if (_notesBar == null)
				return;
			_notesBar.BackColor = BackColor;
		}

		private void EntryHeaderView_SizeChanged(object sender, EventArgs e)
		{
			if (_notesBar == null)
				return;
			DoLayout();
		}

		private void DoLayout()
		{
			if (_notesBar == null)
				return;
			_notesBar.Location=new Point(0, Height-_notesBar.Height);
			int height = Height - _notesBar.Height;
#if USERTF
			_entryPreview.Visible = (height > 20);
			_entryPreview.Height = height;
#else
			if (_entryHeaderBrowser != null)
			{
				_entryHeaderBrowser.Visible = (height > 20);
				_entryHeaderBrowser.Height = height;
			}
#endif
		}
	}
}
