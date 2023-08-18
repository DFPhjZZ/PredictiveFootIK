using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayAnimation : MonoBehaviour
{
    public AnimationClip clip;
    private PlayableGraph m_Graph;
    
    // Start is called before the first frame update
    void Start()
    {
        m_Graph = PlayableGraph.Create("Alt");
        m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var graphOutput = AnimationPlayableOutput.Create(m_Graph, "output", GetComponent<Animator>());

        var clipPlayable = AnimationClipPlayable.Create(m_Graph, clip);
        graphOutput.SetSourcePlayable(clipPlayable);
        m_Graph.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        m_Graph.Destroy();
    }

    void PredictRightFootPos()
    {
        
    }

    void PredictLeftFootPos()
    {
        
    }
}
