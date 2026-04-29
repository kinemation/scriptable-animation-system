// Designed by KINEMATION, 2024.

using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    public interface IAnimationLayerJob
    {
        /// <summary>
        /// Sets up a job. Called on the main thread when a new profile is linked.
        /// </summary>
        /// <param name="jobData">General job data.</param>
        /// <param name="settings">Procedural animation asset.</param>
        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings);

        /// <summary>
        /// Creates a new AnimationScriptPlayable based on the Animation Job.
        /// </summary>
        /// <param name="graph">Main Playable Graph.</param>
        /// <returns></returns>
        public AnimationScriptPlayable CreatePlayable(PlayableGraph graph);

        /// <summary>
        /// Returns active Animator Layer Settings.
        /// </summary>
        /// <returns></returns>
        public FPSAnimatorLayerSettings GetSettingAsset();

        /// <summary>
        /// Called when a layer is explicitly linked.
        /// </summary>
        /// <param name="newSettings">A layer to link.</param>
        public void OnLayerLinked(FPSAnimatorLayerSettings newSettings);

        /// <summary>
        /// Called when a new item or a weapon is equipped.
        /// </summary>
        /// <param name="newEntity">Weapon or item data component.</param>
        public void UpdateEntity(FPSAnimatorEntity newEntity);
        
        /// <summary>
        /// Early game thread update.
        /// </summary>
        public void OnPreGameThreadUpdate();

        /// <summary>
        /// Standard game thread update.
        /// </summary>
        /// <param name="playable">Playable to update.</param>
        /// <param name="weight">General feature weight.</param>
        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight);

        /// <summary>
        /// Called after the pose is finalized.
        /// </summary>
        public void LateUpdate();

        /// <summary>
        /// Destroys this job and disposes its data.
        /// </summary>
        public void Destroy();
    }
}