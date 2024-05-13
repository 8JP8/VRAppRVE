using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class FlyingAnimLoopScript : StateMachineBehaviour
//{
// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
//override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//{
//    
//}

// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//{
//    
//}

// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
//override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//{
//    
//}

// OnStateMove is called right after Animator.OnAnimatorMove()
//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//{
//    // Implement code that processes and affects root motion
//}

// OnStateIK is called right after Animator.OnAnimatorIK()
//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
//{
//    // Implement code that sets up animation IK (inverse kinematics)
//}


public class LoopAnimationToTimestamp : StateMachineBehaviour
{
    // Names of the trigger floats for start and end time
    public string startTimeTriggerFloat = "StartTime";
    public string endTimeTriggerFloat = "EndTime";

    // Whether to loop the animation
    public bool loopAnimation = true;

    // Time to wait before looping the animation
    public float loopDelay = 0.1f;

    // Threshold for checking if the animation time has reached the end time
    private const float threshold = 0.01f;

    // Internal variables to keep track of start and end time
    private float startTime;
    private float endTime;

    // Internal variable to track if the animation has started looping
    private bool loopingStarted = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the start and end time from animator trigger floats
        startTime = animator.GetFloat(startTimeTriggerFloat);
        endTime = animator.GetFloat(endTimeTriggerFloat);

        // Reset looping flag
        loopingStarted = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Check if the animation time is greater than or equal to the end time
        Debug.Log(stateInfo.normalizedTime.ToString());
        Debug.Log(((stateInfo.normalizedTime - Mathf.Floor(stateInfo.normalizedTime)) * 10).ToString());
        if (((stateInfo.normalizedTime - Mathf.Floor(stateInfo.normalizedTime))*10) >= (endTime - threshold))
        {
            // If looping has not started yet, set the flag
            if (!loopingStarted)
            {
                loopingStarted = true;
            }

            // Check if looping has started and delay has passed
            if (loopingStarted)
            {
                LoopAnimation(animator);
            }
        }
    }

    // Method to loop the animation
    private void LoopAnimation(Animator animator)
    {
        // If loopAnimation is enabled, set the normalized time to the start time
        if (loopAnimation)
        {
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 3*startTime);
        }
    }
}
