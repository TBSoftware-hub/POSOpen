namespace POSOpen.Application.Exceptions;

public sealed class RoomConflictException : Exception
{
	public RoomConflictException(string message) : base(message) { }
}
