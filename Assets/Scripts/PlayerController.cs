using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    CharacterController cc;
    public float m_Speed;
    public float m_Gravity = -9.81f;
    private Vector3 moveDir = Vector3.forward;
    private Vector3 moveDist;

    // Start is called before the first frame update
    void Start()
    {
        cc = this.GetComponent<CharacterController>(); 
        // Debug.Log("Left Foot Pos: " + GameObject.Find("PBRCharacter/Hips/UpperLeg_Left/LowerLeg_Left/Foot_Left").GetComponent<Transform>().position);
        // Debug.Log("Right Foot Pos: " + GameObject.Find("PBRCharacter/Hips/UpperLeg_Right/LowerLeg_Right/Foot_Right").GetComponent<Transform>().position);
        // (-0.25, 0.11, -0.06)
        // (0.25, 0.11, -0.06)
    }

    // Update is called once per frame
    void Update()
    {
        moveDir.y += m_Gravity;
        moveDist = moveDir * Time.deltaTime * m_Speed;
        cc.Move(moveDist);
    }
}
