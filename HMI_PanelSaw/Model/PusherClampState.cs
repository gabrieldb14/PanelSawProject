namespace HMI_PanelSaw.Models
{
    public enum PusherClampState : short
    {
        Idle = 0,
        Positioning = 1,
        Clamping = 2,
        Clamped = 3,
        Unclamping = 4,
        Error = 5
    }
}