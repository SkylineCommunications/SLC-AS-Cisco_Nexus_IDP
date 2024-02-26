using System;
using System.Runtime.Serialization;

[Serializable]
public class UpdateFailedException : Exception
{
	public UpdateFailedException()
	{
	}

	public UpdateFailedException(string message) : base(message)
	{
	}

	public UpdateFailedException(string message, Exception innerException) : base(message, innerException)
	{
	}

	protected UpdateFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}