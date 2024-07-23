namespace MiniLcm;

public interface ILexboxApiProvider
{
    ILexboxApi GetProjectApi(string projectCode);
}
