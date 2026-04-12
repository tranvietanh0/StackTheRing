namespace HyperCasualGame.Scripts.CollectArea
{
    using System.Collections.Generic;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Manages CollectArea slots. Matches Cocos CollectAreaManager.ts
    /// </summary>
    public class CollectAreaManager : MonoBehaviour
    {
        [SerializeField] private CollectArea collectAreaPrefab;
        [SerializeField] private Transform areaContainer;
        [SerializeField] private float areaSpacing = GameConstants.CollectAreaConfig.Spacing;

        private readonly List<CollectArea> collectAreas = new();

        public IReadOnlyList<CollectArea> CollectAreas => this.collectAreas;

        private void Awake()
        {
            if (this.areaContainer == null)
            {
                this.areaContainer = this.transform;
            }
        }

        /// <summary>
        /// Spawn N collect areas in a row.
        /// </summary>
        public void SpawnAreas(int count)
        {
            this.ClearContainer();

            if (this.collectAreaPrefab == null)
            {
                Debug.LogError("CollectAreaManager: collectAreaPrefab is null!");
                return;
            }

            var totalWidth = (count - 1) * this.areaSpacing;
            var startX = -totalWidth / 2f;

            for (var i = 0; i < count; i++)
            {
                var area = Instantiate(this.collectAreaPrefab, this.areaContainer);
                area.transform.localPosition = new Vector3(startX + i * this.areaSpacing, 0f, 0f);
                area.SetIndex(i);
                area.name = $"CollectArea_{i}";
                this.collectAreas.Add(area);
            }
        }

        /// <summary>
        /// Get list of all CollectAreas.
        /// </summary>
        public List<CollectArea> GetListCollectArea()
        {
            return new List<CollectArea>(this.collectAreas);
        }

        /// <summary>
        /// Get the first empty (unoccupied) CollectArea.
        /// </summary>
        public CollectArea GetFirstEmptyArea()
        {
            foreach (var area in this.collectAreas)
            {
                if (area != null && !area.IsOccupied)
                {
                    return area;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if all collect areas are occupied.
        /// </summary>
        public bool AreAllCollectAreasOccupied()
        {
            if (this.collectAreas.Count == 0) return false;

            foreach (var area in this.collectAreas)
            {
                if (area != null && !area.IsOccupied)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Clear all areas from the container.
        /// </summary>
        public void ClearContainer()
        {
            foreach (var area in this.collectAreas)
            {
                if (area != null)
                {
                    Destroy(area.gameObject);
                }
            }
            this.collectAreas.Clear();
        }

        /// <summary>
        /// Cleanup all resources.
        /// </summary>
        public void Cleanup()
        {
            this.ClearContainer();
        }
    }
}
