using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform m_target;
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        offset = m_target.position - this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = m_target.position - offset;
    }
}
