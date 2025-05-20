using Oculus.Interaction.Input;
using UnityEngine;

public class HandJointLogger : MonoBehaviour
{
    public Hand targetHand;

    void Update()
    {
        if (targetHand != null && targetHand.IsTrackedDataValid)
        {
            foreach (var joint in targetHand.HandSkeleton.joints)
            {
                Debug.Log($"Joint {joint.pose}");
            }
        }
    }
}
