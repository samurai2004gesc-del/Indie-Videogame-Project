namespace Abismo
{
    /// <summary>
    /// Atajos para el "game feel": congelar el tiempo un instante al golpear (hit-stop)
    /// y hacer temblar la cámara. Son seguros aunque falte el GameManager o la cámara.
    /// </summary>
    public static class GameFeel
    {
        public static void HitStop(float seconds)
        {
            if (GameManager.Instance != null) GameManager.Instance.HitStop(seconds);
        }

        /// <param name="trauma">0..1. 0.2 = golpecito, 0.5 = golpe fuerte, 1 = terremoto.</param>
        public static void Shake(float trauma)
        {
            if (CameraFollow.Instance != null) CameraFollow.Instance.AddTrauma(trauma);
        }
    }
}
