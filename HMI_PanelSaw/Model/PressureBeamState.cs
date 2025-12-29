namespace HMI_PanelSaw.Models
{
    public enum PressureBeamState : short
    {
        Idle = 0,
        Lowering = 1,
        Lowered = 2,
        Raising = 3,
        Error = 4
    }
}
