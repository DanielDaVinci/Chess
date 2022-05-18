using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    // Generic evaluation callback called after all the clips have been processed
    internal interface ITimelineEvaluateCallback
    {
        void Evaluate();
    }


#if UNITY_EDITOR
    /// <summary>
    /// This Rebalancer class ensures that the interval tree structures stays balance regardless of whether the intervals inside change.
    /// </summary>
    class IntervalTreeRebalancer
    {
        private IntervalTree<RuntimeElement> m_Tree;
        public IntervalTreeRebalancer(IntervalTree<RuntimeElement> tree)
        {
            m_Tree = tree;
        }

        public bool Rebalance()
        {
            m_Tree.UpdateIntervals();
            return m_Tree.dirty;
        }
    }
#endif

    // The TimelinePlayable Playable
    // This is the actual runtime playable that gets evaluated as part of a playable graph.
    // It "compiles" a list of tracks into an IntervalTree of Runtime clips.
    // At each frame, it advances time, then fetches the "intersection: of various time interval
    // using the interval tree.
    // Finally, on each intersecting clip, it will calculate each clips' local time, as well as
    // blend weight and set them accordingly


    /// <summary>
    /// The root Playable generated by timeline.
    /// </summary>
    public class TimelinePlayable : PlayableBehaviour
    {
        private IntervalTree<RuntimeElement> m_IntervalTree = new IntervalTree<RuntimeElement>();
        private List<RuntimeElement> m_ActiveClips = new List<RuntimeElement>();
        private List<RuntimeElement> m_CurrentListOfActiveClips;
        private int m_ActiveBit = 0;

        private List<ITimelineEvaluateCallback> m_EvaluateCallbacks = new List<ITimelineEvaluateCallback>();

        private Dictionary<TrackAsset, Playable> m_PlayableCache = new Dictionary<TrackAsset, Playable>();

        internal static bool muteAudioScrubbing = true;

#if UNITY_EDITOR
        private IntervalTreeRebalancer m_Rebalancer;
#endif
        /// <summary>
        /// Creates an instance of a Timeline
        /// </summary>
        /// <param name="graph">The playable graph to inject the timeline.</param>
        /// <param name="tracks">The list of tracks to compile</param>
        /// <param name="go">The GameObject that initiated the compilation</param>
        /// <param name="autoRebalance">In the editor, whether the graph should account for the possibility of changing clip times</param>
        /// <param name="createOutputs">Whether to create PlayableOutputs in the graph</param>
        /// <returns>A subgraph with the playable containing a TimelinePlayable behaviour as the root</returns>
        public static ScriptPlayable<TimelinePlayable> Create(PlayableGraph graph, IEnumerable<TrackAsset> tracks, GameObject go, bool autoRebalance, bool createOutputs)
        {
            if (tracks == null)
                throw new ArgumentNullException("Tracks list is null", "tracks");

            if (go == null)
                throw new ArgumentNullException("GameObject parameter is null", "go");

            var playable = ScriptPlayable<TimelinePlayable>.Create(graph);
            playable.SetTraversalMode(PlayableTraversalMode.Passthrough);
            var sequence = playable.GetBehaviour();
            sequence.Compile(graph, playable, tracks, go, autoRebalance, createOutputs);
            return playable;
        }

        /// <summary>
        /// Compiles the subgraph of this timeline
        /// </summary>
        /// <param name="graph">The playable graph to inject the timeline.</param>
        /// <param name="timelinePlayable"></param>
        /// <param name="tracks">The list of tracks to compile</param>
        /// <param name="go">The GameObject that initiated the compilation</param>
        /// <param name="autoRebalance">In the editor, whether the graph should account for the possibility of changing clip times</param>
        /// <param name="createOutputs">Whether to create PlayableOutputs in the graph</param>
        public void Compile(PlayableGraph graph, Playable timelinePlayable, IEnumerable<TrackAsset> tracks, GameObject go, bool autoRebalance, bool createOutputs)
        {
            if (tracks == null)
                throw new ArgumentNullException("Tracks list is null", "tracks");

            if (go == null)
                throw new ArgumentNullException("GameObject parameter is null", "go");

            var outputTrackList = new List<TrackAsset>(tracks);
            var maximumNumberOfIntersections = outputTrackList.Count * 2 + outputTrackList.Count; // worse case: 2 overlapping clips per track + each track
            m_CurrentListOfActiveClips = new List<RuntimeElement>(maximumNumberOfIntersections);
            m_ActiveClips = new List<RuntimeElement>(maximumNumberOfIntersections);

            m_EvaluateCallbacks.Clear();
            m_PlayableCache.Clear();

            CompileTrackList(graph, timelinePlayable, outputTrackList, go, createOutputs);

#if UNITY_EDITOR
            if (autoRebalance)
            {
                m_Rebalancer = new IntervalTreeRebalancer(m_IntervalTree);
            }
#endif
        }

        private void CompileTrackList(PlayableGraph graph, Playable timelinePlayable, IEnumerable<TrackAsset> tracks, GameObject go, bool createOutputs)
        {
            foreach (var track in tracks)
            {
                if (!track.IsCompilable())
                    continue;

                if (!m_PlayableCache.ContainsKey(track))
                {
                    track.SortClips();
                    CreateTrackPlayable(graph, timelinePlayable, track, go, createOutputs);
                }
            }
        }

        void CreateTrackOutput(PlayableGraph graph, TrackAsset track, GameObject go, Playable playable, int port)
        {
            if (track.isSubTrack)
                return;

            var bindings = track.outputs;
            foreach (var binding in bindings)
            {
                var playableOutput = binding.CreateOutput(graph);
                playableOutput.SetReferenceObject(binding.sourceObject);
                playableOutput.SetSourcePlayable(playable, port);
                playableOutput.SetWeight(1.0f);

                // only apply this on our animation track
                if (track as AnimationTrack != null)
                {
                    EvaluateWeightsForAnimationPlayableOutput(track, (AnimationPlayableOutput)playableOutput);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        EvaluateAnimationPreviewUpdateCallback(track, (AnimationPlayableOutput)playableOutput);
#endif
                }
                if (playableOutput.IsPlayableOutputOfType<AudioPlayableOutput>())
                    ((AudioPlayableOutput)playableOutput).SetEvaluateOnSeek(!muteAudioScrubbing);

                // If the track is the timeline marker track, assume binding is the PlayableDirector
                if (track.timelineAsset.markerTrack == track)
                {
                    var director = go.GetComponent<PlayableDirector>();
                    playableOutput.SetUserData(director);
                    foreach (var c in go.GetComponents<INotificationReceiver>())
                    {
                        playableOutput.AddNotificationReceiver(c);
                    }
                }
            }
        }

        void EvaluateWeightsForAnimationPlayableOutput(TrackAsset track, AnimationPlayableOutput animOutput)
        {
            m_EvaluateCallbacks.Add(new AnimationOutputWeightProcessor(animOutput));
        }

        void EvaluateAnimationPreviewUpdateCallback(TrackAsset track, AnimationPlayableOutput animOutput)
        {
            m_EvaluateCallbacks.Add(new AnimationPreviewUpdateCallback(animOutput));
        }

        private static Playable CreatePlayableGraph(PlayableGraph graph, TrackAsset asset, GameObject go, IntervalTree<RuntimeElement> tree, Playable timelinePlayable)
        {
            return asset.CreatePlayableGraph(graph, go, tree, timelinePlayable);
        }

        private Playable CreateTrackPlayable(PlayableGraph graph, Playable timelinePlayable, TrackAsset track, GameObject go, bool createOutputs)
        {
            if (!track.IsCompilable()) // where parents are not compilable (group tracks)
                return timelinePlayable;

            Playable playable;
            if (m_PlayableCache.TryGetValue(track, out playable))
                return playable;

            if (track.name == "root")
                return timelinePlayable;

            TrackAsset parentActor = track.parent as TrackAsset;
            var parentPlayable = parentActor != null ? CreateTrackPlayable(graph, timelinePlayable, parentActor, go, createOutputs) : timelinePlayable;
            var actorPlayable = CreatePlayableGraph(graph, track, go, m_IntervalTree, timelinePlayable);
            bool connected = false;

            if (!actorPlayable.IsValid())
            {
                // if a track says it's compilable, but returns Playable.Null, that can screw up the whole graph.
                throw new InvalidOperationException(track.name + "(" + track.GetType() + ") did not produce a valid playable. Use the compilable property to indicate whether the track is valid for processing");
            }


            // Special case for animation tracks
            if (parentPlayable.IsValid() && actorPlayable.IsValid())
            {
                int port = parentPlayable.GetInputCount();
                parentPlayable.SetInputCount(port + 1);
                connected = graph.Connect(actorPlayable, 0, parentPlayable, port);
                parentPlayable.SetInputWeight(port, 1.0f);
            }

            if (createOutputs && connected)
            {
                CreateTrackOutput(graph, track, go, parentPlayable, parentPlayable.GetInputCount() - 1);
            }

            CacheTrack(track, actorPlayable, connected ? (parentPlayable.GetInputCount() - 1) : -1, parentPlayable);
            return actorPlayable;
        }

        /// <summary>
        /// Overridden to handle synchronizing time on the timeline instance.
        /// </summary>
        /// <param name="playable">The Playable that owns the current PlayableBehaviour.</param>
        /// <param name="info">A FrameData structure that contains information about the current frame context.</param>
        public override void PrepareFrame(Playable playable, FrameData info)
        {
#if UNITY_EDITOR
            if (m_Rebalancer != null)
                m_Rebalancer.Rebalance();
#endif

            // force seek if we are being evaluated
            //  or if our time has jumped. This is used to
            //  resynchronize
            Evaluate(playable, info);
        }

        private void Evaluate(Playable playable, FrameData frameData)
        {
            if (m_IntervalTree == null)
                return;

            double localTime = playable.GetTime();
            m_ActiveBit = m_ActiveBit == 0 ? 1 : 0;

            m_CurrentListOfActiveClips.Clear();
            m_IntervalTree.IntersectsWith(DiscreteTime.GetNearestTick(localTime), m_CurrentListOfActiveClips);

            foreach (var c in m_CurrentListOfActiveClips)
            {
                c.intervalBit = m_ActiveBit;
                if (frameData.timeLooped)
                    c.Reset();
            }

            // all previously active clips having a different intervalBit flag are not
            // in the current intersection, therefore are considered becoming disabled at this frame
            var timelineEnd = playable.GetDuration();
            foreach (var c in m_ActiveClips)
            {
                if (c.intervalBit != m_ActiveBit)
                {
                    var clipEnd = (double)DiscreteTime.FromTicks(c.intervalEnd);
                    var time = frameData.timeLooped ? Math.Min(clipEnd, timelineEnd) : Math.Min(localTime, clipEnd);
                    c.EvaluateAt(time, frameData);
                    c.enable = false;
                }
            }

            m_ActiveClips.Clear();
            // case 998642 - don't use m_ActiveClips.AddRange, as in 4.6 .Net scripting it causes GC allocs
            for (var a = 0; a < m_CurrentListOfActiveClips.Count; a++)
            {
                m_CurrentListOfActiveClips[a].EvaluateAt(localTime, frameData);
                m_ActiveClips.Add(m_CurrentListOfActiveClips[a]);
            }

            int count = m_EvaluateCallbacks.Count;
            for (int i = 0; i < count; i++)
            {
                m_EvaluateCallbacks[i].Evaluate();
            }
        }

        private void CacheTrack(TrackAsset track, Playable playable, int port, Playable parent)
        {
            m_PlayableCache[track] = playable;
        }

        //necessary to build on AOT platforms
        static void ForAOTCompilationOnly()
        {
            new List<IntervalTree<RuntimeElement>.Entry>();
        }
    }
}
