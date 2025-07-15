namespace Message.Domain.Exceptions;

public class MessageNotFoundException(Guid id) : Exception($"Message with ID {id} was not found.");