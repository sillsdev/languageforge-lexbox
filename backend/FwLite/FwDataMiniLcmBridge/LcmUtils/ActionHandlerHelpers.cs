using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.LcmUtils;

public static class ActionHandlerHelpers
{
    public static async ValueTask DoUsingNewOrCurrentUOW(
        this LcmCache cache,
        string description,
        string revertDescription,
        Func<ValueTask> action)
    {
        var actionHandler = cache.ServiceLocator.ActionHandler;
        if (actionHandler.CurrentDepth > 0)
        {
            await action();
            return;
        }

        using var undoHelper = new UndoableUnitOfWorkHelper(actionHandler, description, revertDescription);
        await action();
        undoHelper.RollBack = false; // task ran successfully, don't roll back.
    }
}
