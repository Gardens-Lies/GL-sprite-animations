using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace SpriteAnimations
{
    /// <summary>
    /// The Sprite Animator. Responsible for playing animations by changing the sprites
    /// of a SpriteRenderer based on the fps of the playing animation.
    /// </summary>
    [AddComponentMenu("No Slopes/Sprite Animations/Sprite Animator")]
    public class SpriteAnimator : MonoBehaviour
    {
        #region Inspector

        [Tooltip("The sprite renderer used for the animation.")]
        [SerializeField]
        protected SpriteRenderer _spriteRenderer;

        [Tooltip("The update mode of the animator.")]
        [SerializeField]
        protected UpdateMode _updateMode = UpdateMode.Update;

        [Tooltip("If the animator should start playing on start.")]
        [SerializeField]
        protected bool _playOnStart = true;

        [Tooltip("The animation that will be played when this animator behaviour starts.")]
        [SerializeField]
        protected SpriteAnimation _defaultAnimation;

        [Tooltip("The animations that can be played by this Sprite Animator. Use the Animations Manager to edit this.")]
        [SerializeField]
        protected List<SpriteAnimation> _spriteAnimations = new();

        [Tooltip("The event that will be invoked when the animation changes.")]
        [SerializeField]
        protected UnityEvent<SpriteAnimation> _animationChanged;

        [Tooltip("The event that will be invoked when the animator state changes.")]
        [SerializeField]
        protected UnityEvent<AnimatorState> _stateChanged;

        #endregion

        #region Fields

        protected bool _loaded;

        protected PerformerFactory _performersFactory;
        protected SpriteAnimation _currentAnimation;

        protected AnimationPerformer _currentPerformer;
        protected Dictionary<string, SpriteAnimation> _animations;

        #endregion

        #region Properties

        /// <summary>
        /// The default animation that will be played when this animator behaviour starts.
        /// </summary>
        public SpriteAnimation DefaultAnimation => _defaultAnimation;

        /// <summary>
        /// The list of animations registered to this animator. 
        /// 
        /// <br></br>
        /// Important: Changing this at runtime will have no effect on the capacity of the animator
        /// to play changed list of animations, unless you reset the animator.
        /// <see cref="ResetAnimator(bool)"/>
        /// </summary>
        public List<SpriteAnimation> AnimationsList => _spriteAnimations;

        /// <summary>
        /// Is the animator playing?
        /// </summary>
        public bool IsPlaying => _state == AnimatorState.Playing;

        /// <summary>
        /// Is The animator paused?
        /// </summary>
        public bool IsPaused => _state == AnimatorState.Paused;

        /// <summary>
        /// Is The animation stopped?
        /// </summary>
        public bool IsStopped => _state == AnimatorState.Stopped;

        protected AnimatorState _state = AnimatorState.Stopped;
        protected Dictionary<string, SpriteAnimation> Animations
        {
            get
            {
                if (!_loaded)
                {
                    LoadAnimator();
                }
                return _animations;
            }
        }

        #endregion

        #region Getters

        /// <summary>
        /// The current state of the animator.
        /// </summary>
        public AnimatorState State => _state;

        /// <summary>
        /// The sprite renderer used by the animator.
        /// </summary>
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        /// <summary>
        /// The current animation being played by the animator.
        /// </summary>
        public SpriteAnimation CurrentAnimation => _currentAnimation;

        /// <summary>
        /// The event that will be invoked when the animation changes.
        /// </summary>
        public UnityEvent<SpriteAnimation> AnimationChanged => _animationChanged;

        /// <summary>
        /// The event that will be invoked when the animator state changes.
        /// </summary>
        public UnityEvent<AnimatorState> StateChanged => _stateChanged;

        /// <summary>
        /// Gets the current performer cast to a specific type.
        /// <br></br>
        /// Useful when references might have changed after a Reset.
        /// <see cref="ResetAnimator(bool, bool)"/>
        /// </summary>
        public T GetCurrentPerformer<T>() where T : AnimationPerformer
            => _currentPerformer as T;
        
        #endregion

        #region Behaviour

        protected virtual void Awake()
        {
            if (!_loaded)
            {
                LoadAnimator();
            }
        }

        protected virtual void Start()
        {
            if (!_playOnStart) return;

            if (_defaultAnimation != null)
            {
                Play(DefaultAnimation);
                return;
            }

            if (_spriteAnimations.Count > 0)
            {
                Play(_spriteAnimations[0]);
                return;
            }
        }

        protected virtual void Update()
        {
            if (!IsPlaying || _updateMode != UpdateMode.Update) return;
            _currentPerformer?.Tick(Time.deltaTime);
        }

        protected virtual void LateUpdate()
        {
            if (!IsPlaying || _updateMode != UpdateMode.LateUpdate) return;
            _currentPerformer?.Tick(Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            if (!IsPlaying || _updateMode != UpdateMode.FixedUpdate) return;
            _currentPerformer?.Tick(Time.deltaTime);
        }

        #endregion

        #region Controlling Animator

        /// <summary>
        /// Plays the default animation. Or the first animation registered to the animator if 
        /// there is no default animation registered.
        /// </summary>
        public AnimationPerformer Play()
        {
            if (_currentAnimation == null)
            {
                return Play(DefaultAnimation);
            }

            if (_spriteAnimations.Count > 0)
            {
                return Play(_spriteAnimations[0]);
            }

            return Play(_currentAnimation);
        }

        /// <summary>
        /// Plays the specified animation by its name. Note that the animation must be registered to the 
        /// animator in order to be found.
        /// </summary>
        /// <typeparam name="TAnimator">The type of the animator.</typeparam>
        /// <param name="name">The name of the animation to play.</param>
        /// <param name="preserveFrame">Preserve the current frame played.</param>
        /// <returns>The AnimationPerformer instance for the played animation, or null if the animation is not found.</returns>
        public TAnimator Play<TAnimator>(string name,
            bool preserveFrame = false) where TAnimator : AnimationPerformer
        {
            // Try to get the animation by name
            if (!TryGetAnimationByName(name, out var animation))
            {
                // Log an error if the animation is not found
                Logger.LogError($"Animation '{name}' not found.", this);
                return null;
            }

            // Play the animation
            return Play<TAnimator>(animation, preserveFrame);
        }

        /// <summary>
        /// Plays the specified animation by its name. Note that the animation must be registered to the 
        /// animator in order to be found.
        /// </summary>
        /// <param name="name">The name of the animation.</param>
        /// <param name="preserveFrame">Preserve the current frame played.</param>
        /// <returns>The AnimationPerformer instance for the played animation, or null if the animation is not found.</returns>
        public AnimationPerformer Play(string name, bool preserveFrame = false)
        {
            // Try to get the animation by its name
            if (!TryGetAnimationByName(name, out var animation))
            {
                // Log an error if the animation is not found
                Logger.LogError($"Animation '{name}' not found.", this);
                return null;
            }

            // Play the animation
            return Play(animation, preserveFrame);
        }

        /// <summary>
        /// Plays the specified animation.
        /// </summary>
        /// <typeparam name="TAnimator">The type of the animation performer.</typeparam>
        /// <param name="animation">The animation to play.</param>
        /// <param name="preserveFrame">Preserve the current frame played.</param>
        /// <returns>The animation performer.</returns>
        public TAnimator Play<TAnimator>(SpriteAnimation animation,
            bool preserveFrame = false) where TAnimator : AnimationPerformer
        {
            // If the animation is already playing, return the existing animation performer.
            if (animation == _currentAnimation)
                return _performersFactory.Get<TAnimator>(animation);

            // If the animation is null, return null and log an error.
            if (animation == null)
            {
                Logger.LogError("Could not evaluate an animation to be played. Null given", this);
                return null;
            }

            // Change the current animation.
            ChangeAnimation(animation, CalculateStartTime(animation, preserveFrame));

            // Set the sprite animator state to playing.
            _state = AnimatorState.Playing;

            // Return the animation performer.
            return _performersFactory.Get<TAnimator>(animation);
        }

        /// <summary>
        /// Plays the given sprite animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        /// <param name="preserveFrame">Preserve the current frame played.</param>
        /// <returns>The animation performer.</returns>
        public AnimationPerformer Play(SpriteAnimation animation,
            bool preserveFrame = false)
        {

            // If the animation is already playing, return the existing animation performer
            if (animation == _currentAnimation)
                return _performersFactory.Get(animation);

            // If the animation is null, log an error and return null
            if (animation == null)
            {
                Logger.LogError("Trying to play a null animation", this);
                return null;
            }

            // Change the current animation
            ChangeAnimation(animation, CalculateStartTime(animation, preserveFrame));

            // Set the sprite animator state to playing.
            _state = AnimatorState.Playing;
            return _performersFactory.Get(animation);
        }

        /// <summary>
        /// Pauses the animator. Use Resume to continue.
        /// </summary>
        public AnimationPerformer Pause()
        {
            _state = AnimatorState.Paused;
            _stateChanged.Invoke(_state);
            return _currentPerformer;
        }

        /// <summary>
        /// Resumes a paused animator.
        /// </summary>
        public AnimationPerformer Resume()
        {
            _state = AnimatorState.Playing;
            _stateChanged.Invoke(_state);
            return _currentPerformer;
        }

        /// <summary>
        /// Stops animating
        /// </summary>
        public void Stop()
        {
            _currentPerformer?.StopAnimation(); // Stop current animation.
            _state = AnimatorState.Stopped;
            _stateChanged.Invoke(_state);
            _currentAnimation = null;
            _currentPerformer = null;
        }

        /// <summary>
        /// Resets the animator.
        /// Use with precautions !
        /// </summary>
        /// <param name="keepState">
        /// Keep the current animation state of performers.
        /// <br></br>
        /// If false, animation will not update automatically
        /// and errors may occurs as well.
        /// <br></br>
        /// It'll brock the reference with <see cref="_currentPerformer"/>
        /// so you better keep the performer with <see cref="GetCurrentPerformer{T}"/>
        /// </param>
        /// <param name="preserveTime">
        /// Preserve actual frame of animation.
        /// </param>
        public void ResetAnimator(bool keepState = false,
            bool preserveTime = false)
        {

            string previousAnimName = null;
            AnimatorState previousState = _state;

            // Saves additionnal informations for performers.
            bool isWindrose = false;
            WindroseDirection savedDirection = WindroseDirection.South;
            float timeline = 0.0f;
            if (keepState && _currentAnimation != null && _currentPerformer != null)
            {

                previousAnimName = _currentAnimation.AnimationName;

                // Here we keep Windrose's direction for later.
                if (_currentPerformer is WindroseAnimator windrosePerformer)
                {
                    isWindrose = true;
                    savedDirection = windrosePerformer.CurrentWindroseDirection;
                }

                if (preserveTime)
                    timeline = _currentAnimation.CalculateDuration();
            }

            // Cleanup

            _currentPerformer?.StopAnimation();
            _animations?.Clear();
            _performersFactory = null;
            _loaded = false;

            if (!keepState)
            {
                _currentAnimation = null;
                _currentPerformer = null;
                _state = AnimatorState.Stopped;
            }

            LoadAnimator();

            // Retoration of performers
            if (keepState && !string.IsNullOrEmpty(previousAnimName))
            {
                if (TryGetAnimationByName(previousAnimName, out SpriteAnimation newAnimation))
                {

                    ChangeAnimation(newAnimation, timeline);

                    // Restores Windrose Animator state.
                    if (isWindrose && _currentPerformer is WindroseAnimator newWindrosePerformer)
                        newWindrosePerformer.SetDirection(savedDirection);
                    
                    _state = previousState;
                    if (_state == AnimatorState.Paused)
                        _stateChanged.Invoke(_state);
                }

                else
                    Stop();
            }
        }

        /// <summary>
        /// This changes the current animation to be played by the animator. It will call 
        /// the current animation Stop() method and the new animation Start() method.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="startTime"></param>
        protected void ChangeAnimation(SpriteAnimation animation, float startTime = 0f)
        {
            _currentPerformer?.StopAnimation(); // Stop current animation.

            _currentPerformer = _performersFactory.Get(animation); // Sets the current handler to the given animation.
            _currentAnimation = animation; // current animation is now the given animation.
            _currentPerformer.StartAnimation(_currentAnimation, startTime); // Starts the given animation.

            _animationChanged.Invoke(_currentAnimation); // Fires the animation changed event.
        }

        protected void LoadAnimator()
        {
            if (_spriteRenderer == null)
            {
                if (!TryGetComponent(out _spriteRenderer))
                {
                    Logger.LogError($"Sprite Renderer not found.", this);
                    return;
                }
            }

            _performersFactory = new PerformerFactory(this);
            _animations = new();
            _spriteAnimations.ForEach(a => _animations.Add(a.AnimationName, a));
            _loaded = true;
        }

        /// <summary>
        /// Calculate start time of animation based on actual animation completion.
        /// <br></br>
        /// Frame preservation have no effect if animation duration is too small
        /// (at least 2 frames.)
        /// </summary>
        /// <param name="newAnimation"></param>
        /// <param name="preserveFrame">
        /// True if frame's animation is preserved,
        /// otherwise false.
        /// </param>
        protected float CalculateStartTime(SpriteAnimation newAnimation, bool preserveFrame)
        {

            // Si on ne veut pas préserver ou qu'il n'y a pas d'animation en cours, on part de 0
            if (!preserveFrame || _currentPerformer == null || _currentAnimation == null)
                return 0f;

            // Can't preserve frame if animation has a duration of 0.
            float currentDuration = _currentAnimation.CalculateDuration();
            float newDuration = newAnimation.CalculateDuration();
            if (currentDuration <= 0 || newDuration <= 0)
                return 0f;

            // Preserving frame require at least 2 frames.
            if (_currentAnimation.CalculateFramesCount() <= 1)
                return 0f;

            // Sync frames with modulo.
            float currentCycleTime = _currentPerformer.CurrentCycleElapsedTime % currentDuration;
            float normalizedTime = currentCycleTime / currentDuration;
            return normalizedTime * newDuration;
        }

        #endregion

        #region Handling Animation

        /// <summary>
        /// Gets a registered animation by its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns> The animation or null if not found </returns>
        public SpriteAnimation GetAnimationByName(string name)
        {
            if (!TryGetAnimationByName(name, out SpriteAnimation animation))
            {
                return null;
            }

            return animation;
        }

        /// <summary>
        /// Tries to get a registered animation by its name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="animation"></param>
        /// <returns> True if the animation is found, false otherwise </returns>
        public bool TryGetAnimationByName(string name, out SpriteAnimation animation)
        {
            return Animations.TryGetValue(name, out animation);
        }

        #endregion

        #region Evaluations

        /// <summary>
        /// Calculates the duration of the current animation.
        /// Depending on the type of animation this can have serious perfomance impacts
        /// as it has to evaluate all frames of all the animation cycles.
        /// </summary>
        /// <returns>The duration of the current animation.</returns>
        public float CalculateCurrentAnimationDuration()
        {
            if (_currentAnimation == null) return 0;

            return _currentAnimation.CalculateDuration();
        }

        #endregion
    }
}