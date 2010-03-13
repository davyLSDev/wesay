using System;
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
		private const string _emptyEntryHtml =
			"<html><head><style type=\"text/css\">body { background-color: #cbffb9; }</style></head><body></body></html>";
		private string _lastEntryHtml = _emptyEntryHtml;

		/// <summary>
		/// designer only
		/// </summary>
		public EntryHeaderView()
		{
			InitializeComponent();
		}

		public EntryHeaderView(NotesBarView notesBarView)
		{
			InitializeComponent();
			_entryHeaderBrowser.DocumentCompleted += _browserDocumentLoaded;

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

		void _browserDocumentLoaded(object sender, WebBrowserDocumentCompletedEventArgs args)
		{
			// work around for text not being set when browser is first initialized
			if (!_lastEntryHtml.Equals(_entryHeaderBrowser.DocumentText))
				_entryHeaderBrowser.DocumentText = _lastEntryHtml;
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
#if MONO
				_entryHeaderBrowser.Navigate("javascript:{document.body.outerHTML='" + _emptyEntryHtml + "'}");
#else
				_entryHeaderBrowser.DocumentText = _emptyEntryHtml;
#endif
			}
			else
			{
				_entryPreview.Rtf = RtfRenderer.ToRtf(record,
													  currentItemInFocus,
													  lexEntryRepository);
				ViewTemplate viewTemplate = (ViewTemplate)
						WeSayWordsProject.Project.ServiceLocator.GetService(typeof(ViewTemplate));
				_lastEntryHtml = HtmlRenderer.ToHtml(record, currentItemInFocus, lexEntryRepository, viewTemplate);
#if MONO
				_entryHeaderBrowser.Navigate("javascript:{document.body.outerHTML='" +
					_lastEntryHtml.Replace("'","\'") + "'}");
#else
				_entryHeaderBrowser.DocumentText = _lastEntryHtml;

#endif
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
			_entryHeaderBrowser.Visible = (height > 20);
			_entryHeaderBrowser.Height = height;
#endif
		}
	}
}
