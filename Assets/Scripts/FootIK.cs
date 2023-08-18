using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;

public class FootIK : MonoBehaviour
{
    // Playable动画
    public AnimationClip clip;
    private PlayableGraph m_Graph;

    [Header("迈步长度")]
    public float stepSize;

    [Header("预测路径分段数")] 
    public int deltaSteps ;

    [Header("脚位置")]
    // 用于碰撞检测点位
    public Transform LeftFoot;
    public Transform RightFoot;

    [Header("重心骨骼")]
    // 控制重心变化
    public Transform Bip;

    [Header("脚步IK控制器")]
    public Transform LeftFootController;
    public Transform RightFootController;

    [Header("脚部碰撞层")]
    public LayerMask ColliderMask;

    [Header("抬腿检测高度")]
    // 腿能抬的高度
    public float StepHeight = 0.4f;

    [Header("重心延迟时间")]
    public float damptime = 0.1f;

    // 检测脚步运动轨迹，用于调整脚
    public AnimationCurve RightFootCurve;
    public AnimationCurve LeftFootCurve;
    private float leftDist, rightDist;

    // 记录上一次起脚位置
    private Vector3 LastLeftPosition;
    private Vector3 LastRightPosition;
    private Vector3 m_PredictedLeftFootPos;
    private Vector3 m_PredictedRightFootPos;

    // 记录上一次重心高度
    private float LastBipHeight;

    private float vel;
    
    // Start is called before the first frame update
    void Start()
    {
        // 初始化上一次脚位置
        LastLeftPosition = LeftFoot.position;
        LastLeftPosition.y = this.transform.position.y;
        
        LastRightPosition = RightFoot.position;
        LastRightPosition.y = this.transform.position.y;
        
        // 初始化重心高度
        LastBipHeight = Bip.position.y;

        // 初始化脚步路径曲线
        LeftFootCurve = new AnimationCurve();
        LeftFootCurve.AddKey(0.0f, 0.0f);
        LeftFootCurve.AddKey(stepSize, 0.0f);
        
        RightFootCurve = new AnimationCurve();
        RightFootCurve.AddKey(0.0f, 0.0f);
        RightFootCurve.AddKey(stepSize, 0.0f);
        
        // 使用Playable播放动画
        m_Graph = PlayableGraph.Create();
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var graphOutput = AnimationPlayableOutput.Create(m_Graph, "Output", GetComponent<Animator>());
        var clipPlayable = AnimationClipPlayable.Create(m_Graph, clip);
        graphOutput.SetSourcePlayable(clipPlayable);
        m_Graph.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        // 假设认为：重心总是跟着高度更低的那只脚移动
        // 根据预测的脚的运动曲线，计算每次Update时，各脚应该有的高度
        
        // 预计左脚应有的高度
        // 根据当前脚与上次脚位置间的方向向量与Forward的点乘结果，得到此时对应的FootPathCurve上的时间坐标（处理右脚时相同）
        Vector3 LeftDir = LeftFoot.position - LastLeftPosition;
        leftDist = Vector3.Dot(LeftDir, transform.forward);
        leftDist = Mathf.Clamp(leftDist, 0.0f, stepSize);
        // 预计的左脚应有的高度
        float leftH = LeftFootCurve.Evaluate(leftDist) - LeftFootCurve[0].value + LastLeftPosition.y;

        // 预计右脚应有的高度
        Vector3 RightDir = RightFoot.position - LastRightPosition;
        rightDist = Vector3.Dot(RightDir, transform.forward);
        rightDist = Mathf.Clamp(rightDist, 0.0f, stepSize);
        float rightH = RightFootCurve.Evaluate(rightDist) - RightFootCurve[0].value + LastRightPosition.y;

        // 取预计左脚和预计右脚的最低高度值，用于调整重心
        float H = Mathf.Min(leftH, rightH);
        
        // 计算Hip应该预计位置
        Vector3 Hip = Bip.position;
        float HipY = Bip.position.y - transform.position.y; // Hip到transform中心的高度值 （该示例初始情况下，transform.y = 0.0f）
        Hip.y += H;
        Bip.position = Hip;
        // 重心的延迟
        Hip.y = Mathf.SmoothDamp(LastBipHeight, H + HipY, ref vel, damptime);
        LastBipHeight = Hip.y;
        Bip.position = Hip;
        
        // IK控制器位置设置，通过IK控制器调整脚部动画
        Vector3 L = LeftFoot.position;
        L.y = Mathf.Max(L.y + leftH - H, leftH); // 保证每次Update时左脚高度必须在FootPath路径高度之上，实际情况中，L.y + leftH - H 一直大于 leftH，下同
        LeftFootController.position = L;
        Vector3 R = RightFoot.position;
        R.y = Mathf.Max(R.y + rightH - H, rightH);
        RightFootController.position = R;
        
    }

    private void OnDisable()
    {
        m_Graph.Destroy();
    }

    private Vector3 PredictStep(Transform foot, ref AnimationCurve footCurve)
    {
        // 从脚向下发射线找到当前位置
        RaycastHit StartHit;
        Physics.Raycast(foot.position + Vector3.up * 0.1f, Vector3.down, out StartHit, 0.5f, ColliderMask);

        // 从预测脚下一步的位置上方向下发射射线，获得预计脚的落点
        RaycastHit EndHit;
        Physics.Raycast(StartHit.point + stepSize * transform.forward + new Vector3(0.0f, StepHeight + 0.1f ,0.0f)/*脚下一步的位置上方*/, Vector3.down, out EndHit, StepHeight * 4.0f, ColliderMask);
        // Physics.SphereCast(
        //     StartHit.point + stepSize * transform.forward + new Vector3(0.0f, StepHeight + 0.1f, 0.0f) /*脚下一步的位置上方*/,
        //     0.05f, Vector3.down, out EndHit, StepHeight * 4.0f, ColliderMask);
        
        // 画出当前脚与预测脚落点之间的连线
        Debug.DrawLine(StartHit.point + new Vector3(0.0f, 0.03f, 0.0f), EndHit.point + new Vector3(0.0f, 0.03f, 0.0f), Color.red, 1.0f);

        // 只计算forward方向上的移动，和竖直方向上的高度，构造运动轨迹曲线
        // 起始点的高度是上一个曲线的高度差
        float footCurveStartHeight = footCurve[footCurve.length - 1].value - footCurve[0].value;
        footCurve = AnimationCurve.EaseInOut(0.0f, footCurveStartHeight, stepSize,
            footCurveStartHeight + (EndHit.point - StartHit.point).y);
      
        var deltaStep = (EndHit.point - StartHit.point) / deltaSteps;
        for (int i = 1; i < deltaSteps; i++)
        {
            var curPoint = StartHit.point + i * deltaStep;
            RaycastHit curHit;
            bool hitFlag;
            hitFlag = Physics.Raycast(curPoint + new Vector3(0.0f, StepHeight + 0.1f, 0.0f), Vector3.down, out curHit, StepHeight * 4.0f, ColliderMask);
            if (hitFlag && curHit.transform.gameObject.name != "Plane")
            {
                float deltaT = Vector3.Dot(curHit.point - StartHit.point, Vector3.forward);
                Keyframe keyframe = new Keyframe();
                keyframe.time = deltaT;
                keyframe.value = (curHit.point.y - StartHit.point.y) + footCurve[0].value;
                footCurve.AddKey(keyframe);
            }
        }
        
        if (foot == LeftFoot)
            m_PredictedLeftFootPos = EndHit.point;
        if (foot == RightFoot)
            m_PredictedRightFootPos = EndHit.point;

        return StartHit.point;

    }
    
    // 用于相应AnimationClip上的事件，预测左脚抬起时，下次应当落下的左脚位置，并构造脚的运动曲线
    void PredictLeftFootPos()
    {
        LastLeftPosition = PredictStep(LeftFoot, ref LeftFootCurve);
    }

    // 用于相应AnimationClip上的事件，预测右脚抬起时，下次应当落下的右脚位置，并构造脚的运动曲线
    void PredictRightFootPos()
    {
        LastRightPosition = PredictStep(RightFoot, ref RightFootCurve);
    }

    void OnLeftFootContact()
    {
        
    }

    void OnRightFootContact()
    {
        
    }

    private void OnGUI()
    {
        GUI.TextField(new Rect(25, 25, 200, 30), LeftFootCurve.Evaluate(leftDist).ToString());
        GUI.TextField(new Rect(25, 65, 200, 30), LeftFoot.position.ToString());
        GUI.TextField(new Rect(25, 105, 200, 30), RightFootCurve.Evaluate(rightDist).ToString());
        GUI.TextField(new Rect(25, 145, 200, 30), RightFoot.position.ToString());
    }
}
