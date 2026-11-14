using UnityEngine;

namespace MolimoByLulucaGameDev
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteYSorter : MonoBehaviour
    {
        [SerializeField, Min(1)] private int precision = 50;
        [SerializeField] private int sortingOffset = 0;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            // Quanto menor o Y (mais perto da base do ecrã), maior o Order in Layer.
            // O offset permite colocar a silhueta "à frente" do objeto que a oclusa.
            _renderer.sortingOrder = (int)(transform.position.y * -precision) + sortingOffset;
        }
    }
}