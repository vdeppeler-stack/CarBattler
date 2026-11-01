using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Static colliders only receive their own collision messages; pieces forward rams to the owner.</summary>
    public class DestructiblePiece : MonoBehaviour
    {
        public Destructible owner;

        void OnCollisionEnter(Collision collision)
        {
            if (owner != null) owner.OnPieceCollision(collision);
        }
    }
}
