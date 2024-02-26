using System;
using System.Runtime.Serialization;

[Serializable]
public class BackupFailedException : Exception
{
	public BackupFailedException()
	{
	}

	public BackupFailedException(string message) : base(message)
	{
	}

	public BackupFailedException(string message, Exception innerException) : base(message, innerException)
	{
	}

	protected BackupFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}