namespace HyperCasualGame.Scripts.CollectArea
{
    using UnityEngine;

    /// <summary>
    /// A slot where a bucket can land. Matches Cocos CollectArea.ts
    /// </summary>
    public class CollectArea : MonoBehaviour
    {
        [SerializeField] private int areaIndex;

        private bool isOccupied;
        private Transform occupyingBucket;

        public int AreaIndex => this.areaIndex;
        public bool IsOccupied => this.isOccupied;
        public Transform OccupyingBucket => this.occupyingBucket;

        public void SetIndex(int index)
        {
            this.areaIndex = index;
        }

        /// <summary>
        /// Mark this CollectArea as occupied by a bucket.
        /// </summary>
        public void Occupy(Transform bucketTransform)
        {
            if (this.isOccupied)
            {
                Debug.LogWarning($"CollectArea {this.name} is already occupied!");
                return;
            }

            this.isOccupied = true;
            this.occupyingBucket = bucketTransform;
        }

        /// <summary>
        /// Release this CollectArea, making it available for another bucket.
        /// </summary>
        public void Release()
        {
            if (!this.isOccupied)
            {
                Debug.LogWarning($"CollectArea {this.name} is already empty!");
                return;
            }

            this.isOccupied = false;
            this.occupyingBucket = null;
        }

        /// <summary>
        /// Reset the CollectArea to initial state.
        /// </summary>
        public void Reset()
        {
            this.isOccupied = false;
            this.occupyingBucket = null;
        }
    }
}
