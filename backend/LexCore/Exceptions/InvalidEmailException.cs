namespace LexCore.Exceptions;

public class InvalidEmailException(string address) : Exception(address) { }
