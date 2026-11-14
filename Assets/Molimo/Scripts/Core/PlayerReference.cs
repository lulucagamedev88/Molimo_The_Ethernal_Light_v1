using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class PlayerReference : MonoBehaviour
    {
        public static PlayerReference Instance { get; private set; }
        public Transform Root => transform;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }
}