using Cysharp.Threading.Tasks;
using UnityEngine;



/*
EffectUtility.cs
用于创建和管理各种效果（Effect）的实用工具类。
包含对Unity内置效果、单位效果、高级效果和随机效果的支持。
*/


namespace GoveKits.Units
{
    #region Unity Effect Implementations
    public static class UnityEffect
    {
        public static IEffect Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return new InstantiateEffect(prefab, position, rotation);
        }
        public static IEffect Move(Transform transform, Vector3 targetPosition, float duration)
        {
            return new MoveEffect(transform, targetPosition, duration);
        }
        public static IEffect Rotate(Transform transform, Vector3 targetEulerAngles, float duration)
        {
            return new RotateEffect(transform, targetEulerAngles, duration);
        }
        public static IEffect Scale(Transform transform, Vector3 targetScale, float duration)
        {
            return new ScaleEffect(transform, targetScale, duration);
        }
        public static IEffect Color(Material material, Color targetColor, float duration)
        {
            return new ColorEffect(material, targetColor, duration);
        }
        public static IEffect SetActive(GameObject gameObject, bool active)
        {
            return new SetActiveEffect(gameObject, active);
        }
        public static IEffect PlayAudio(AudioSource audioSource, AudioClip audioClip = null)
        {
            return new PlayAudioEffect(audioSource, audioClip);
        }
        public static IEffect PlayParticle(ParticleSystem particleSystem)
        {
            return new PlayParticleEffect(particleSystem);
        }
        public static IEffect PlayAnimation(Animator animator, string stateName, float crossFadeTime = 0.1f)
        {
            return new PlayAnimationEffect(animator, stateName, crossFadeTime);
        }

    }

    internal class InstantiateEffect : IEffect
    {
        private readonly GameObject _prefab;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;

        public InstantiateEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            _prefab = prefab;
            _position = position;
            _rotation = rotation;
        }

        public UniTask Apply(EffectContext context)
        {
            Object.Instantiate(_prefab, _position, _rotation);
            return UniTask.CompletedTask;
        }
    }

    internal class MoveEffect : IEffect
    {
        private readonly Transform _transform;
        private readonly Vector3 _targetPosition;
        private readonly float _duration;
        private Vector3 _startPosition;

        public MoveEffect(Transform transform, Vector3 targetPosition, float duration)
        {
            _transform = transform;
            _targetPosition = targetPosition;
            _duration = duration;
        }

        public async UniTask Apply(EffectContext context)
        {
            _startPosition = _transform.position;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _duration);
                _transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
                await UniTask.Yield();
            }

            _transform.position = _targetPosition;
        }
    }

    internal class RotateEffect : IEffect
    {
        private readonly Transform _transform;
        private readonly Vector3 _targetEulerAngles;
        private readonly float _duration;
        private Vector3 _startEulerAngles;

        public RotateEffect(Transform transform, Vector3 targetEulerAngles, float duration)
        {
            _transform = transform;
            _targetEulerAngles = targetEulerAngles;
            _duration = duration;
        }

        public async UniTask Apply(EffectContext context)
        {
            _startEulerAngles = _transform.eulerAngles;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _duration);
                _transform.eulerAngles = Vector3.Lerp(_startEulerAngles, _targetEulerAngles, progress);
                await UniTask.Yield();
            }

            _transform.eulerAngles = _targetEulerAngles;
        }
    }

    internal class ScaleEffect : IEffect
    {
        private readonly Transform _transform;
        private readonly Vector3 _targetScale;
        private readonly float _duration;
        private Vector3 _startScale;

        public ScaleEffect(Transform transform, Vector3 targetScale, float duration)
        {
            _transform = transform;
            _targetScale = targetScale;
            _duration = duration;
        }

        public async UniTask Apply(EffectContext context)
        {
            _startScale = _transform.localScale;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _duration);
                _transform.localScale = Vector3.Lerp(_startScale, _targetScale, progress);
                await UniTask.Yield();
            }

            _transform.localScale = _targetScale;
        }
    }

    internal class ColorEffect : IEffect
    {
        private readonly Material _material;
        private readonly Color _targetColor;
        private readonly float _duration;
        private Color _startColor;

        public ColorEffect(Material material, Color targetColor, float duration)
        {
            _material = material;
            _targetColor = targetColor;
            _duration = duration;
        }

        public async UniTask Apply(EffectContext context)
        {
            _startColor = _material.color;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _duration);
                _material.color = Color.Lerp(_startColor, _targetColor, progress);
                await UniTask.Yield();
            }

            _material.color = _targetColor;
        }
    }

    internal class SetActiveEffect : IEffect
    {
        private readonly GameObject _gameObject;
        private readonly bool _active;

        public SetActiveEffect(GameObject gameObject, bool active)
        {
            _gameObject = gameObject;
            _active = active;
        }

        public UniTask Apply(EffectContext context)
        {
            _gameObject.SetActive(_active);
            return UniTask.CompletedTask;
        }
    }

    internal class PlayAudioEffect : IEffect
    {
        private readonly AudioSource _audioSource;
        private readonly AudioClip _audioClip;

        public PlayAudioEffect(AudioSource audioSource, AudioClip audioClip)
        {
            _audioSource = audioSource;
            _audioClip = audioClip;
        }

        public UniTask Apply(EffectContext context)
        {
            if (_audioClip != null)
                _audioSource.clip = _audioClip;

            _audioSource.Play();
            return UniTask.CompletedTask;
        }
    }

    internal class PlayParticleEffect : IEffect
    {
        private readonly ParticleSystem _particleSystem;

        public PlayParticleEffect(ParticleSystem particleSystem)
        {
            _particleSystem = particleSystem;
        }

        public UniTask Apply(EffectContext context)
        {
            _particleSystem.Play();
            return UniTask.CompletedTask;
        }
    }

    internal class PlayAnimationEffect : IEffect
    {
        private readonly Animator _animator;
        private readonly string _stateName;
        private readonly float _crossFadeTime;

        public PlayAnimationEffect(Animator animator, string stateName, float crossFadeTime)
        {
            _animator = animator;
            _stateName = stateName;
            _crossFadeTime = crossFadeTime;
        }

        public UniTask Apply(EffectContext context)
        {
            _animator.CrossFade(_stateName, _crossFadeTime);
            return UniTask.CompletedTask;
        }
    }
    #endregion



    #region IUnit Effect Implementations
    public static class UnitEffect
    {
        public static IEffect ModifyAttribute(AttributeContainer attributeContainer, string attributeKey, float modificationValue)
        {
            return new ModifyAttributeEffect(attributeContainer, attributeKey, modificationValue);
        }

        public static IEffect AddTag(GameplayTagContainer GameplayTagContainer, GameplayTag tag)
        {
            return new AddTagEffect(GameplayTagContainer, tag);
        }

        public static IEffect RemoveTag(GameplayTagContainer GameplayTagContainer, GameplayTag tag)
        {
            return new RemoveTagEffect(GameplayTagContainer, tag);
        }
    }

    internal class ModifyAttributeEffect : IEffect
    {
        private readonly AttributeContainer _attributeContainer;
        private readonly string _attributeKey;
        private readonly float _modificationValue;

        public ModifyAttributeEffect(AttributeContainer attributeContainer, string attributeKey, float modificationValue)
        {
            _attributeContainer = attributeContainer;
            _attributeKey = attributeKey;
            _modificationValue = modificationValue;
        }

        public UniTask Apply(EffectContext context)
        {
            var attribute = _attributeContainer.GetAttribute(_attributeKey);
            attribute.Value += _modificationValue;
            return UniTask.CompletedTask;
        }
    }

    internal class AddTagEffect : IEffect
    {
        private readonly GameplayTagContainer _GameplayTagContainer;
        private readonly GameplayTag _tag;

        public AddTagEffect(GameplayTagContainer GameplayTagContainer, GameplayTag tag)
        {
            _GameplayTagContainer = GameplayTagContainer;
            _tag = tag;
        }

        public UniTask Apply(EffectContext context)
        {
            _GameplayTagContainer.AddTag(_tag);
            return UniTask.CompletedTask;
        }
    }

    internal class RemoveTagEffect : IEffect
    {
        private readonly GameplayTagContainer _GameplayTagContainer;
        private readonly GameplayTag _tag;

        public RemoveTagEffect(GameplayTagContainer GameplayTagContainer, GameplayTag tag)
        {
            _GameplayTagContainer = GameplayTagContainer;
            _tag = tag;
        }

        public UniTask Apply(EffectContext context)
        {
            _GameplayTagContainer.RemoveTag(_tag);
            return UniTask.CompletedTask;
        }
    }
    #endregion



    #region Random Effect Implementations
    public static class RandomEffect
    {
        // 在这里添加随机效果实现工厂方法
    }
    #endregion
}