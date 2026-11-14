using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Marca um objeto alto (pilar, árvore, edifício) como occluder.
    /// O GameObject precisa de um BoxCollider2D com IsTrigger = true cobrindo
    /// a área visual do sprite (topo até à base). O collider físico do
    /// objeto fica num filho separado, só na base.
    /// </summary>
    public class OccluderSilhouette : MonoBehaviour
    {
        [Tooltip("Transform cujo Y é o pivot de Y-sorting (base do objeto).")]
        [SerializeField] private Transform sortReference;

        public float SortY => sortReference != null
            ? sortReference.position.y
            : transform.position.y;
    }
}