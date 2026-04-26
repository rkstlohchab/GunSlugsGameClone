using UnityEngine;

namespace GunSlugsClone.Procedural
{
    public enum DoorDirection { North, East, South, West }

    public sealed class DoorSocket : MonoBehaviour
    {
        [SerializeField] private DoorDirection direction;
        [SerializeField] private bool startsOpen = false;
        public DoorDirection Direction => direction;
        public bool IsConnected { get; set; }

        public Vector2 LocalPosition => transform.localPosition;

        public DoorDirection OppositeDirection => direction switch
        {
            DoorDirection.North => DoorDirection.South,
            DoorDirection.South => DoorDirection.North,
            DoorDirection.East  => DoorDirection.West,
            _                   => DoorDirection.East,
        };

        private void OnDrawGizmos()
        {
            Gizmos.color = startsOpen ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Vector3 arrow = direction switch
            {
                DoorDirection.North => Vector3.up,
                DoorDirection.South => Vector3.down,
                DoorDirection.East  => Vector3.right,
                _                   => Vector3.left,
            };
            Gizmos.DrawLine(transform.position, transform.position + arrow * 0.8f);
        }
    }
}
