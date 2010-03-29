namespace WeSay.LexicalTools
{
	partial class EntryHeaderView
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Palaso.Reporting.Logger.WriteEvent("EntryHeaderView.Dispose");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._entryPreview = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			//
			// _entryPreview
			//
			this._entryPreview.BackColor = System.Drawing.Color.LightSeaGreen;
			this._entryPreview.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._entryPreview.Dock = System.Windows.Forms.DockStyle.Top;
			this._entryPreview.Location = new System.Drawing.Point(0, 0);
			this._entryPreview.Name = "_entryPreview";
			this._entryPreview.ReadOnly = true;
			this._entryPreview.Size = new System.Drawing.Size(527, 85);
			this._entryPreview.TabIndex = 1;
			this._entryPreview.TabStop = false;
			this._entryPreview.Text = "";
			this._entryPreview.Visible = false;
			//
			// EntryHeaderView
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "EntryHeaderView";
			this.Size = new System.Drawing.Size(527, 129);
			this.Load += new System.EventHandler(this.EntryHeaderView_Load);
			this.BackColorChanged += new System.EventHandler(this.EntryHeaderView_BackColorChanged);
			this.SizeChanged += new System.EventHandler(this.EntryHeaderView_SizeChanged);
			this.ResumeLayout(false);

		}

		#endregion

		private void InitializeBrowser()
		{
			//
			// _entryHeaderBrowser
			//
#if MONO
			if (this._entryHeaderBrowser == null)
			{
					this._entryHeaderBrowser = sEntryHeaderBrowser;

			}
#endif
			if (this._entryHeaderBrowser == null)
			{
				this._entryHeaderBrowser = new DictWebBrowser();
				this._entryHeaderBrowser.AllowWebBrowserDrop = false;
				this._entryHeaderBrowser.Dock = System.Windows.Forms.DockStyle.Top;
				this._entryHeaderBrowser.Location = new System.Drawing.Point(0, 0);
				this._entryHeaderBrowser.MinimumSize = new System.Drawing.Size(20, 20);
				this._entryHeaderBrowser.Name = "_entryHeaderBrowser";
				this._entryHeaderBrowser.Size = new System.Drawing.Size(527, 85);
				this._entryHeaderBrowser.TabIndex = 2;
				this._entryHeaderBrowser.Visible = true;
				this._entryHeaderBrowser.AutoSize = true;
				this._entryHeaderBrowser.IsWebBrowserContextMenuEnabled = false;
				this._entryHeaderBrowser.ScrollBarsEnabled = true;
				System.Windows.Forms.WebBrowser.CheckForIllegalCrossThreadCalls = true;
#if MONO
				EntryHeaderView.sEntryHeaderBrowser = this._entryHeaderBrowser;
#endif
			}
			this.Controls.Add(this._entryHeaderBrowser);
		}

		private System.Windows.Forms.RichTextBox _entryPreview;
#if MONO
		private static System.Windows.Forms.WebBrowser sEntryHeaderBrowser = null;
#endif
		private System.Windows.Forms.WebBrowser _entryHeaderBrowser = null;
	}

	class DictWebBrowser : System.Windows.Forms.WebBrowser
	{
			protected override void Dispose(bool disposing)
			{
						System.Console.WriteLine("Dispose browser " +
									 new System.Diagnostics.StackTrace().ToString());

						base.Dispose(disposing);
			}
	}
}
