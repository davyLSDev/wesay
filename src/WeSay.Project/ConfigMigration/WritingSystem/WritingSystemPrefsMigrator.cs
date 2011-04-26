﻿using System;
using System.Collections.Generic;
using System.IO;
using Palaso.Migration;

namespace WeSay.Project.ConfigMigration.WritingSystem
{
	public class WritingSystemPrefsMigrator
	{
		private readonly string _sourceFilePath;
		private readonly WritingSystemPrefsToLdmlMigrationStrategy.OnMigrationFn _onWritingSystemTagChange;

		public WritingSystemPrefsMigrator(string sourceFilePath, WritingSystemPrefsToLdmlMigrationStrategy.OnMigrationFn onWritingSystemTagChange)
		{
			_onWritingSystemTagChange = onWritingSystemTagChange;
			_sourceFilePath = sourceFilePath;
		}

		public bool NeedsMigration()
		{
			return File.Exists(_sourceFilePath);
		}

		public string WritingSystemsFolder(string basePath)
		{
			return Path.Combine(basePath, "WritingSystems");
		}

		public void Migrate()
		{
			string backupFilePath = _sourceFilePath + ".bak";
			var filesToDelete = new List<string>();
			var directoriesToDelete = new List<string>();
			if (File.Exists(backupFilePath))
			{
				File.Delete(backupFilePath);
			}
			File.Copy(_sourceFilePath, backupFilePath);
			filesToDelete.Add(backupFilePath);

			string ldmlPath = WritingSystemsFolder(Path.GetDirectoryName(_sourceFilePath));
			var strategy = new WritingSystemPrefsToLdmlMigrationStrategy(_onWritingSystemTagChange);
			string sourceFilePath = _sourceFilePath;
			string destinationFilePath = String.Format("{0}.Migrate_{1}_{2}", _sourceFilePath, strategy.FromVersion,
												strategy.ToVersion);
			strategy.Migrate(sourceFilePath, destinationFilePath);
			File.Delete(_sourceFilePath);
			Directory.Move(destinationFilePath, ldmlPath);
			foreach (var filePath in filesToDelete)
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
			foreach (var filePath in directoriesToDelete)
			{
				if (Directory.Exists(filePath))
				{
					Directory.Delete(filePath);
				}
			}
		}
	}
}
