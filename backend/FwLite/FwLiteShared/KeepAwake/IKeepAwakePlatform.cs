namespace FwLiteShared.KeepAwake;

public interface IKeepAwakePlatform
{
    void Acquire(KeepAwakeWork work);
    void Release();
}

internal class NoOpKeepAwakePlatform : IKeepAwakePlatform
{
    public void Acquire(KeepAwakeWork work)
    {
    }

    public void Release()
    {
    }
}
