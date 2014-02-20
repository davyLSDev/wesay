using System;
using NUnit.Framework;
using Palaso.WritingSystems;
using WeSay.LexicalModel.Foundation;
using WeSay.UI.TextBoxes;

namespace WeSay.UI.Tests
{
	[TestFixture]
	public class IWeSayTextBoxTests
	{
		[SetUp]
		public void Setup() {}

		[TearDown]
		public void TearDown() {}

		[Test]
		public void Create()
		{
			IWeSayTextBox textBox = new WeSayTextBox();
			Assert.IsNotNull(textBox);
		}

		[Test]
		public void CreateWithWritingSystem()
		{
			IWritingSystemDefinition ws = new WritingSystemDefinition();
			IWeSayTextBox textBox = new WeSayTextBox(ws, null);
			Assert.IsNotNull(textBox);
			Assert.AreSame(ws, textBox.WritingSystem);
		}

		[Test]
		public void SetWritingSystem_Null_Throws()
		{
			IWeSayTextBox textBox = new WeSayTextBox();
			Assert.Throws<ArgumentNullException>(() => textBox.WritingSystem = null);
		}

		[Test]
		public void WritingSystem_Unassigned_Get_Throws()
		{
			IWeSayTextBox textBox = new WeSayTextBox();
			IWritingSystemDefinition ws;
			Assert.Throws<InvalidOperationException>(() => ws= textBox.WritingSystem);
		}

		[Test]
		public void WritingSystem_Unassigned_Focused_Throws()
		{
			IWeSayTextBox textBox = new WeSayTextBox();
			Assert.Throws<InvalidOperationException>(() => textBox.AssignKeyboardFromWritingSystem());
		}

		[Test]
		public void WritingSystem_Unassigned_Unfocused_Throws()
		{
			IWeSayTextBox textBox = new WeSayTextBox();
			Assert.Throws<InvalidOperationException>(() => textBox.ClearKeyboard());
		}
	}
}