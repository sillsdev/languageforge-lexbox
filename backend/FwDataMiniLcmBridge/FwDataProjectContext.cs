﻿namespace FwDataMiniLcmBridge;

public class FwDataProjectContext
{
    private sealed class ProjectHolder
    {
        public FwDataProject? Project;
    }

    private static readonly AsyncLocal<ProjectHolder> _projectHolder = new();

    public virtual FwDataProject? Project
    {
        get => _projectHolder.Value?.Project;
        set
        {
            var holder = _projectHolder.Value;
            if (holder != null)
            {
                // Clear current Project trapped in the AsyncLocals, as its done.
                holder.Project = null;
            }

            if (value is not null)
            {
                // Use an object indirection to hold the Project in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared above.
                _projectHolder.Value = new ProjectHolder { Project = value };
            }
        }
    }
}
