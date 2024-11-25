public class SuctionIrrigatorTeleoperationController : MTMTeleoperationControllerBase
{
    protected override void InitializePSM()
    {
        m_psmController.DriveJoints(new float[] { 0.5f, 0f, -1.8f, 0, 0, 0f});
    }
}
