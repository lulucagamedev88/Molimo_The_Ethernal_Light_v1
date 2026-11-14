using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class FriendAISorting : MonoBehaviour
    {
        public Transform player;
        private SpriteRenderer friendAISprite;
        private SpriteRenderer playerSprite;

        void Start()
        {
            friendAISprite = GetComponent<SpriteRenderer>();
            playerSprite = player.GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (transform.position.y > player.position.y)
            {
                friendAISprite.sortingOrder = playerSprite.sortingOrder - 1;
            }
            else
            {
                friendAISprite.sortingOrder = playerSprite.sortingOrder + 1;
            }
        }
    }
}
