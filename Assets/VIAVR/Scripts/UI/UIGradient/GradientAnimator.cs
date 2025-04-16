using System;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts.UI.UIGradient
{
    public class GradientAnimator : MonoBehaviour
    {
        enum AnimationState
        {
            TO_RUN, RUNNING, TO_STOP, STOPPING, STOPPED
        }
    
        [Serializable]
        public struct GradientColors
        {
            [SerializeField] public Color _color1;
            [SerializeField] public Color _color2;

            public static GradientColors Lerp(GradientColors from, GradientColors to, float t)
            {
                t = Mathf.Clamp01(t);

                return new GradientColors()
                {
                    _color1 = Color.Lerp(from._color1, to._color1, t),
                    _color2 = Color.Lerp(from._color2, to._color2, t)
                };
            }
        }
    
        [SerializeField] private UIGradient _gradient;
        [SerializeField] private Graphic _graphic;

        [SerializeField] private bool _useChangeColorsAnimation;
        [SerializeField] private bool _useRotateGradientAnimation;

        [SerializeField] private GradientColors[] _animationColors;

        [SerializeField] private float _colorsChangeSpeed = 1f;
        [SerializeField] private float _stoppingAnimationSpeed = 1f;
        [SerializeField] private float _rotationSpeed = 360;

        [SerializeField] private AnimationState _animationState = AnimationState.STOPPED;

        private float _defaultGradientRotation;

        private int _currentState;
        private float _t;
    
        GradientColors _lastColors;
        GradientColors _newColors;

        public void StartAnimation()
        {
            if(_animationState != AnimationState.STOPPED && _animationState != AnimationState.STOPPING) return;
        
            _animationState = AnimationState.TO_RUN;
        }
    
        public void StopAnimation()
        {
            if(_animationState != AnimationState.RUNNING) return;

            _animationState = AnimationState.TO_STOP;
        }
    
        private void FixedUpdate()
        {
            if(!gameObject.activeSelf) return;

            switch (_animationState)
            {
                case AnimationState.STOPPED:
                    return;
            
                case AnimationState.TO_STOP:
                    _t = 0;
                    _animationState = AnimationState.STOPPING;
                    break;
            
                case AnimationState.TO_RUN:
                    _t = 0;
                    _lastColors = _newColors = _animationColors[0];
                    _defaultGradientRotation = _gradient.m_angle;
                    _animationState = AnimationState.RUNNING;
                    break;
            
                case AnimationState.RUNNING:
                    RunningAnimation();
                    break;
            
                case AnimationState.STOPPING:
                    StoppingAnimation();
                    if (_t >= 1)
                        _animationState = AnimationState.STOPPED;
                    break;

                default:
                    Debug.LogError($"{nameof(_animationState)} == {_animationState} is out of switch range!");
                    break;
            }
        }

        private void RunningAnimation()
        {
            _t += Time.deltaTime * _colorsChangeSpeed;

            if (_t >= 1)
            {
                _t -= 1;

                _lastColors = _animationColors[_currentState];
                    
                _currentState++;

                if (_currentState >= _animationColors.Length)
                    _currentState = 0;
                
                _newColors = _animationColors[_currentState];
            }

            if(_useRotateGradientAnimation)
                RotateGradient(_gradient, _graphic, Time.deltaTime * _rotationSpeed);
        
            if(_useChangeColorsAnimation)
                ApplyGradientState(_gradient, _graphic, GradientColors.Lerp(_lastColors, _newColors, _t));                                                                                                                                                                                                                                                                           
        }

        private void StoppingAnimation()
        {
            _t += Time.deltaTime * _stoppingAnimationSpeed;
        
            _lastColors = new GradientColors{ _color1 = _gradient.m_color1, _color2 = _gradient.m_color2 };
            _newColors = _animationColors[0];
        
            if(_useRotateGradientAnimation)
                RotateGradient(_gradient, _graphic, Mathf.Lerp(_gradient.m_angle, _defaultGradientRotation, _t), true);
        
            if(_useChangeColorsAnimation)
                ApplyGradientState(_gradient, _graphic, GradientColors.Lerp(_lastColors, _newColors, _t));
        }

        private void RotateGradient(UIGradient gradient, Graphic graphic, float value, bool absolute = false)
        {
            if(absolute)
                gradient.m_angle = value;
            else if(value == 0)
                return;
            else
                gradient.m_angle += value;

            if (gradient.m_angle > 180)
            {
                gradient.m_angle -= 360;
            }
            else if (gradient.m_angle < -180)
            {
                gradient.m_angle += 360;
            }
        
            graphic.SetVerticesDirty();
        }

        private void ApplyGradientState(UIGradient gradient, Graphic graphic, GradientColors gradientColors)
        {
            gradient.m_color1 = gradientColors._color1;
            gradient.m_color2 = gradientColors._color2;
            
            graphic.SetVerticesDirty();
        }
    }
}