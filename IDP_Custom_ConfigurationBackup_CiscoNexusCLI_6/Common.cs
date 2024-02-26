namespace IDP.Common
{
	public class BackupDataFileType
	{
		public string Tftp { get; set; }

		public int FileType { get; set; }

		public string FileName { get; set; }
	}

	public class BackupDataFolderPath
	{
		public string Tftp { get; set; }

		public string FolderPath { get; set; }

		public string FileName { get; set; }
	}

	public class BackupDataSourceIp
	{
		public string SourceIp { get; set; }

		public string FileName { get; set; }
	}
}