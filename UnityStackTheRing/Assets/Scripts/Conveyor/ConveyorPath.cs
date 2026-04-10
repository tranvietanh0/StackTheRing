namespace HyperCasualGame.Scripts.Conveyor
{
    using System.Collections.Generic;
    using Dreamteck.Splines;
    using UnityEngine;

    /// <summary>
    /// Wrapper for Dreamteck SplineComputer that provides sample points.
    /// Matches Cocos ConveyorPath interface.
    /// </summary>
    public class ConveyorPath
    {
        private readonly List<Vector3> points = new();
        private readonly SplineComputer spline;

        public ConveyorPath(SplineComputer splineComputer, int sampleCount = 100)
        {
            this.spline = splineComputer;
            this.BuildSamples(sampleCount);
        }

        public ConveyorPath(List<Vector3> pathPoints)
        {
            this.points = new List<Vector3>(pathPoints);
        }

        private void BuildSamples(int sampleCount)
        {
            this.points.Clear();

            if (this.spline == null)
            {
                return;
            }

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / (sampleCount - 1);
                var sample = new SplineSample();
                this.spline.Evaluate(t, ref sample);
                this.points.Add(sample.position);
            }
        }

        public int GetSampleCount()
        {
            return this.points.Count;
        }

        public Vector3 GetSample(int index)
        {
            if (index < 0)
            {
                return this.points[0];
            }

            if (index >= this.points.Count)
            {
                return this.points[^1];
            }

            return this.points[index];
        }

        /// <summary>
        /// Find nearest index on path based on world position
        /// </summary>
        public int GetNearestIndex(Vector3 worldPos)
        {
            var minDist = float.MaxValue;
            var nearestIndex = 0;

            for (var i = 0; i < this.points.Count; i++)
            {
                var d = Vector3.Distance(worldPos, this.points[i]);
                if (d < minDist)
                {
                    minDist = d;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
    }
}
