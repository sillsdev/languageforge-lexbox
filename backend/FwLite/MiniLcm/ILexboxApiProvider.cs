namespace MiniLcm;

public interface ILexboxApiProvider
{
    IMiniLcmApi GetProjectApi(string projectCode);
}
