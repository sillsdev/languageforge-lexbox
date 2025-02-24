namespace MiniLcm.Exceptions;

public class MultiStringReadonlyException(string path, string ws) : Exception($"value {path}:{ws} is readonly");
