using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace WeSay.UI
{
	partial class VBoxFlow
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
			//Debug.WriteLine("Disposing " + Name + "   Disposing=" + disposing);

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
			this.SuspendLayout();
			//
			// VBox
			//
			this.Name = "VBox";
			this.ResumeLayout(false);

		}

		#endregion


	}
}
