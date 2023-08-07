using Microsoft.Playwright;

namespace Testing.Browser.Component;

public abstract class BaseComponent<T> where T : BaseComponent<T>
{
    public IPage Page { get; private set; }
    public ILocator ComponentLocator { get; }

    public BaseComponent(IPage page, ILocator componentLocator)
    {
        Page = page;
        ComponentLocator = componentLocator;
    }

    public virtual async Task<T> WaitFor()
    {
        await ComponentLocator.WaitForAsync();
        return (T)this;
    }

    public ILocator Locator(string selector, LocatorLocatorOptions? options = null)
    {
        return ComponentLocator.Locator(selector, options);
    }

    public virtual async Task AssertGone()
    {
        await Assertions.Expect(ComponentLocator).Not.ToBeAttachedAsync();
    }
}
