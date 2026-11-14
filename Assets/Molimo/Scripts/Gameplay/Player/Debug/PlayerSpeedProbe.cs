using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>Diagnóstico: média de velocidade por direção.</summary>
    public class PlayerSpeedProbe : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField, Min(0.5f)] private float logInterval = 3f;

        private Vector2 lastPos;
        private readonly float[] distByDir = new float[8];
        private readonly float[] timeByDir = new float[8];
        private float timer;

        private static readonly string[] Names = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

        private void Start()
        {
            if (!rb) rb = GetComponent<Rigidbody2D>();
            lastPos = rb.position;
        }

        private void FixedUpdate()
        {
            Vector2 pos = rb.position;
            Vector2 delta = pos - lastPos;
            lastPos = pos;

            if (delta.sqrMagnitude > 0.000001f)
            {
                // Atribui ao eixo real do deslocamento (não à intenção).
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                int step = Mathf.RoundToInt(angle / 45f);
                if (step < 0) step += 8;
                int idx = step % 8;

                distByDir[idx] += delta.magnitude;
                timeByDir[idx] += Time.fixedDeltaTime;
            }

            timer += Time.fixedDeltaTime;
            if (timer >= logInterval)
            {
                LogReport();
                timer = 0f;
            }
        }

        private void LogReport()
        {
            var sb = new System.Text.StringBuilder("[SpeedProbe] Velocidade média por direção:\n");
            for (int i = 0; i < 8; i++)
            {
                float avg = timeByDir[i] > 0.05f ? distByDir[i] / timeByDir[i] : 0f;
                sb.AppendLine($"  {Names[i],-2} : {avg,7:F3} u/s   " +
                              $"(dist {distByDir[i],6:F2}, tempo {timeByDir[i],5:F2}s)");
            }
            Debug.Log(sb.ToString());
        }
    }
}