﻿using ADV.Commands.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Utility scripts for the post processing stack
// https://github.com/keijiro/PostProcessingUtilities
namespace AIGraphics
{
    public class FocusPuller : MonoBehaviour
    {   
        AIGraphics _parent;
        float _velocity;
        Vector3 lastDoFPoint;
        bool initialFrame;
        private GameObject _ftgo;
        private Transform _target;
        public LayerMask hitLayer;
        public float maxDistance;        

        public Transform FocusTarget 
        { 
            get
            {
                if(!_target)
                {
                    _ftgo = new GameObject("FocusTarget");
                    _target = _ftgo.transform;
                }
                return _target;
            }
            set => _target = value;
        }

        [SerializeField] float _speed = 6f;
        public float speed
        {
            get { return _speed; }
            set { _speed = Mathf.Max(0.01f, value); }
        }

        public void init(AIGraphics parent)
        {
            _parent = parent;
            hitLayer = _parent.CameraSettings.MainCamera.cullingMask;
            maxDistance = _parent.CameraSettings.MainCamera.farClipPlane;
        }

        void Awake()
        {
            Physics.queriesHitBackfaces = true;            

        }
        void OnEnable()
        {
            initialFrame = true;
        }
        void OnValidate()
        {
            speed = _speed;
        }

        void LateUpdate()
        {
            if (initialFrame)
            {
                initialFrame = false;
                Focus(new Vector3(Screen.width / 2, Screen.height / 2));
            }
            else
                Focus(Input.mousePosition);

            // Retrieve the current value.
            var d1 = _parent.PostProcessingSettings.FocalDistance;//_controller.depthOfField.focusDistance;

            //Debug.DrawLine(transform.position, _target.position, Color.green);

            // Calculate the depth of the focus point.
            var d2 = Vector3.Dot((FocusTarget?.position ?? new Vector3(Screen.width / 2, Screen.height / 2)) - transform.position, transform.forward);
            if (d2 < 0.1f)
                d2 = 0.1f;

            // Damped-spring interpolation.
            var dt = Time.deltaTime;
            var n1 = _velocity - (d1 - d2) * speed * speed * dt;
            var n2 = 1 + speed * dt;
            _velocity = n1 / (n2 * n2);
            var d = d1 + _velocity * dt;

            // Apply the result.
            _parent.PostProcessingSettings.FocalDistance = d;
        }

        void Focus(Vector3 PointOnScreen)
        {
            // our ray
            //var ray = transform.GetComponent<Camera>().ScreenPointToRay(PointOnScreen);
            var ray = _parent.CameraSettings.MainCamera.ScreenPointToRay(PointOnScreen);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayer))
            {
                //Debug.DrawLine(ray.origin, hit.point);
                // do we have a new point?					
                if (lastDoFPoint == hit.point)
                    return;
                FocusTarget.position = hit.point;
                // asign the last hit
                lastDoFPoint = hit.point;
            }
        }
    }
}
