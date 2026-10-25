using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpriteAnimations
{
    public delegate void FramePlayedEvent(int index, Frame frame);
    /// <summary>
    /// The base class for all animation performers.
    /// </summary>
    public abstract class AnimationPerformer
    {
        #region Constants

        /// <summary>
        /// Small extra time added when skipping to the next frame, so that float
        /// rounding does not leave the performer just before the target frame.
        /// </summary>
        private const float FrameEpsilon = 0.0001f;

        #endregion

        #region Fields

        /// <summary>
        /// The SpriteAnimator related to that performer
        /// </summary>
        protected SpriteAnimator _animator;

        /// <summary>
        /// The current animation associated with the performer. Note that
        /// the performer may have a current animation set but may not be 
        /// playing it.
        /// </summary>
        protected SpriteAnimation _currentAnimation;

        /// <summary>
        /// If the performer is currently playing an animation
        /// </summary>
        protected bool _isPlaying = false;

        /// <summary>
        /// List of frames used by the current cycle  
        /// </summary>
        protected Cycle _currentCycle;

        /// <summary>
        /// Used when running an animation
        /// </summary>
        public float CurrentCycleElapsedTime => _currentCycleElapsedTime;
        protected float _currentCycleElapsedTime = 0.0f;

        /// <summary>
        /// Current frame of the current cycle
        /// </summary>
        protected Frame _currentFrame;

        protected Dictionary<int, UnityAction<Frame>> _frameIndexActions = new();
        protected Dictionary<string, UnityAction<Frame>> _frameIdActions = new();
        protected UnityAction _onEndAction;

        #endregion       

        #region Properties  

        public SpriteAnimator Animator { get => _animator; set => _animator = value; }
        protected bool HasCurrentAnimation => _currentAnimation != null;
        protected bool HasCurrentCycle => _currentCycle != null;

        #endregion

        #region Getters 

        /// <summary>
        /// Current cycle being played
        /// </summary>
        public Cycle CurrentCycle => _currentCycle;

        /// <summary>
        /// The current frame displayed in the animator's SpriteRenderer
        /// </summary>
        public Frame CurrentFrame => _currentFrame;

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when this performer changed the current frame displayed at the animator's SpriteRenderer
        /// </summary>
        public event FramePlayedEvent FramePlayed;

        protected void InvokeFramePlayed(int index, Frame frame)
        {
            FramePlayed?.Invoke(index, frame);
        }

        protected void ClearFramePlayedListeners()
        {
            FramePlayed = null;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// The method called every time the animator should evaluate if the frame must be changed.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last tick.</param>
        public abstract void Tick(float deltaTime);

        #endregion

        #region Logic       

        /// <summary>
        /// Starts the given sprite animation.
        /// </summary>
        /// <param name="animation">The sprite animation to start.</param>
        /// <param name="entryTime">Time to start the animation at (default 0)</param>
        public virtual void StartAnimation(SpriteAnimation animation, float entryTime = 0f)
        {

            // Set the flipX property of the sprite renderer to false
            _animator.SpriteRenderer.flipX = false;

            _currentCycleElapsedTime = entryTime;
        }

        /// <summary>
        /// Stops the animation and clears all the frame index actions, frame ID actions, and the on end action.
        /// </summary>
        public virtual void StopAnimation()
        {
            // Clear the frame index actions
            _frameIndexActions.Clear();

            // Clear the frame ID actions
            _frameIdActions.Clear();

            // Set the on end action to null
            _onEndAction = null;

            ClearFramePlayedListeners();
        }

        /// <summary>
        /// Immediately displays the next frame of the current cycle, without waiting
        /// for the animator's tick. That frame then lasts a full frame duration.
        /// </summary>
        public void PlayNextFrame()
        {
            float elapsedTime = _currentCycleElapsedTime;
            if (!HasCurrentCycle || !TryGetTimeUntilNextFrame(elapsedTime,
                _currentCycle.CalculateDuration(), _currentCycle.Size, out float remainingTime))
                return;

            Tick(remainingTime + FrameEpsilon);

            // A cycle end resets the elapsed time without displaying the first frame.
            if (_currentCycleElapsedTime < elapsedTime)
                Tick(FrameEpsilon);
        }

        /// <summary>
        /// Validates if the animation should be played.
        /// </summary>
        /// <returns> true if animation can be played </returns>
        protected bool ValidateAnimation(SpriteAnimation animation)
        {
            if (animation == null)
            {
                Logger.LogError($"Trying to set null as current animation.", _animator);
                return false;
            }

            int framesCount = animation.CalculateFramesCount();

            if (framesCount > 0) return true;

            Logger.LogError($"Unable play the animation {animation.AnimationName} due to the lack of frames. "
            + $"Did you set animation frames?", _animator);

            return false;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Sets an action to be performed when a specific frame index is played.
        /// </summary>
        /// <param name="frameIndex">The index of the frame.</param>
        /// <param name="action">The action to be performed.</param>
        /// <returns>The SpriteAnimationPerformer instance.</returns>
        public AnimationPerformer SetOnFrame(int frameIndex, UnityAction<Frame> action)
        {
            _frameIndexActions[frameIndex] = action;
            return this;
        }

        /// <summary>
        /// Sets an action to be invoked when a specific frame ID is played.
        /// </summary>
        /// <param name="id">The frame ID.</param>
        /// <param name="action">The UnityAction to be invoked.</param>
        /// <returns>The updated PerformerSingle instance.</returns>
        public AnimationPerformer SetOnFrame(string id, UnityAction<Frame> action)
        {
            _frameIdActions[id] = action;
            return this;
        }

        /// <summary>
        /// Sets the action to be invoked when the animation ends.
        /// This overrides previous actions defined for that animation end.
        /// </summary>
        /// <param name="action">The UnityAction to be invoked.</param>
        /// <returns>The updated SpriteAnimationPerformer instance.</returns>
        public AnimationPerformer SetOnEnd(UnityAction action)
        {
            _onEndAction = action;
            return this;
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Calculates the frame index based on the elapsed time, amount of frames, and duration.
        /// </summary>
        /// <param name="elapsedTime">The time that has elapsed since the animation started.</param>
        /// <param name="amountOfFrames">The total number of frames in the animation.</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        /// <returns>The index of the current frame to display.</returns>
        protected int CalculateFrameIndex(float elapsedTime, int amountOfFrames, float duration)
        {
            return Mathf.FloorToInt(elapsedTime * amountOfFrames / duration);
        }

        /// <summary>
        /// Calculates the time remaining before the next frame of a cycle starts.
        /// </summary>
        /// <param name="elapsedTime">The time elapsed in the cycle.</param>
        /// <param name="cycleDuration">The duration of the whole cycle.</param>
        /// <param name="amountOfFrames">The number of frames in the cycle.</param>
        /// <param name="remainingTime">The time before the next frame starts.</param>
        /// <returns>False if the cycle has no next frame (single frame, or 0 FPS).</returns>
        public static bool TryGetTimeUntilNextFrame(float elapsedTime, float cycleDuration,
            int amountOfFrames, out float remainingTime)
        {
            remainingTime = 0.0f;
            if (amountOfFrames <= 1 || !(cycleDuration > 0.0f) || float.IsInfinity(cycleDuration))
                return false;

            float frameDuration = cycleDuration / amountOfFrames;
            int frameIndex = Mathf.FloorToInt(elapsedTime / frameDuration);
            remainingTime = (frameIndex + 1) * frameDuration - elapsedTime;

            return true;
        }

        #endregion

    }
}