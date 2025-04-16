using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.ViewControls
{
    public enum SubViewMode { DEFAULT, INPUT, AWAIT, SUCCESS, ERROR }

    public class ViewControlsGroup : ControlsGroupBase<ViewControls<SubViewMode>, SubViewMode>
    {
    }
}